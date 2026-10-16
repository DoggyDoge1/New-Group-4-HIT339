"""End-to-end checks using a disposable database, never the working libdata.db.

Run from any directory: python scripts/verify_member3.py
Requires Python 3 and the .NET 10 SDK. Set DOTNET_EXE to override `dotnet`.
"""
import csv
import html
import http.cookiejar
import io
import json
import os
from pathlib import Path
import re
import shutil
import socket
import sqlite3
import subprocess
import tempfile
import time
import unittest
import urllib.error
import urllib.parse
import urllib.request

PROJECT = Path(__file__).resolve().parents[1]
DOTNET = os.environ.get("DOTNET_EXE", "dotnet")
KEY = "integration-test-key-used-only-in-disposable-process"


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, file, code, message, headers, new_url):
        return None


def client():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), NoRedirect())


class IntegrationChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        subprocess.run([DOTNET, "build", str(PROJECT / "Habibalib.csproj"), "--nologo"], check=True)
        cls.temp = tempfile.TemporaryDirectory(prefix="habibalib-member3-")
        cls.root = Path(cls.temp.name)
        shutil.copytree(PROJECT / "Samples", cls.root / "Samples")
        shutil.copy(PROJECT / "appsettings.json", cls.root / "appsettings.json")
        with socket.socket() as sock:
            sock.bind(("127.0.0.1", 0))
            port = sock.getsockname()[1]
        cls.base = f"http://127.0.0.1:{port}"
        cls.log = (cls.root / "server.log").open("w+")
        env = dict(os.environ, PublicApi__ApiKey=KEY, ASPNETCORE_ENVIRONMENT="Development",
                   Logging__LogLevel__Default="Warning", Logging__LogLevel__Microsoft="Warning")
        cls.process = subprocess.Popen([DOTNET, str(PROJECT / "bin/Debug/net10.0/Habibalib.dll"),
                                        "--urls", cls.base], cwd=cls.root, env=env, stdout=cls.log, stderr=cls.log)
        cls.anonymous = client()
        for _ in range(150):
            try:
                if cls.request("/")[0] == 200:
                    break
            except OSError:
                pass
            if cls.process.poll() is not None:
                cls.log.seek(0)
                raise RuntimeError(cls.log.read())
            time.sleep(0.1)
        else:
            cls.process.terminate()
            raise RuntimeError("Application did not start")
        cls.admin = cls.login("admin@gmail.com", "pwdadmin@999")
        cls.manager = cls.login("manager@gmail.com", "pwd@999")
        cls.reception = cls.login("recept@gmail.com", "pwdrecept@999")
        with cls.database() as db:
            # Independent values exercise inclusive dates, revenue state and exported totals.
            db.execute("UPDATE Books SET IsInTransit=1 WHERE LibraryCode='B003'")
            db.executemany('''INSERT INTO BorrowTransactions
                (ItemType,ItemId,ItemName,BorrowerId,BorrowDate,DueDate,ReturnDate,Fine,Paid)
                VALUES (?,?,?,?,?,?,?,?,?)''', [
                ("Book", 1, 'Café, "quoted" report', 1001, "2025-01-02 10:00:00", "2025-01-16 10:00:00", "2025-01-20 10:00:00", "40.00", 1),
                ("Music", 1, "=SUM(1,2)", 1001, "2025-01-02 23:59:59", "2025-01-16 10:00:00", "2025-01-18 10:00:00", "20.00", 0),
                ("Toy", 1, "Outside range", 1001, "2025-01-03 00:00:00", "2025-01-17 10:00:00", None, "0", 0)])

    @classmethod
    def tearDownClass(cls):
        cls.process.terminate()
        try:
            cls.process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            cls.process.kill()
            cls.process.wait()
        cls.log.close()
        cls.temp.cleanup()

    @classmethod
    def database(cls):
        return sqlite3.connect(cls.root / "libdata.db")

    @classmethod
    def request(cls, path, opener=None, method="GET", data=None, headers=None):
        request = urllib.request.Request(cls.base + path, data=data, headers=headers or {}, method=method)
        try:
            response = (opener or cls.anonymous).open(request, timeout=30)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            return response.status, response.read(), response.headers

    @classmethod
    def login(cls, email, password):
        opener = client()
        status, _, _ = cls.request("/Account/Login", opener, "POST",
                                  urllib.parse.urlencode({"email": email, "password": password}).encode(),
                                  {"Content-Type": "application/x-www-form-urlencoded"})
        if status != 302:
            raise RuntimeError(f"Demo login failed for {email}: {status}")
        return opener

    def api(self, path, **kwargs):
        return self.request(path, headers={"X-Api-Key": KEY}, **kwargs)

    def upload(self, data, token=True, filename="test.csv"):
        _, page, _ = self.request("/Import", self.admin)
        fields = []
        if token:
            match = re.search(rb'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
            self.assertIsNotNone(match, "Import form must include an anti-forgery token")
            fields.append(("__RequestVerificationToken", html.unescape(match.group(1).decode())))
        boundary = "----habibalibtestboundary"
        body = bytearray()
        for name, value in fields:
            body.extend(f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n'.encode())
        body.extend(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{filename}"\r\nContent-Type: text/csv\r\n\r\n'.encode())
        body.extend(data)
        body.extend(f'\r\n--{boundary}--\r\n'.encode())
        return self.request("/Import/Index", self.admin, "POST", bytes(body),
                            {"Content-Type": f"multipart/form-data; boundary={boundary}"})

    def count_items(self):
        with self.database() as db:
            return sum(db.execute(f'SELECT COUNT(*) FROM "{table}"').fetchone()[0] for table in ["Books", "Music", "Toys"])

    def test_api_authentication(self):
        self.assertEqual(401, self.request("/api/v1/items")[0])
        self.assertEqual(401, self.request("/api/v1/items", headers={"X-Api-Key": "wrong"})[0])
        self.assertEqual(401, self.request("/api/v1/items", self.admin)[0], "A staff cookie does not replace an API key")
        self.assertEqual(200, self.api("/api/v1/items")[0])

    def test_api_availability_and_no_personal_data(self):
        status, body, _ = self.api("/api/v1/items?pageSize=100")
        self.assertEqual(200, status)
        items = json.loads(body)["items"]
        self.assertTrue(items)
        self.assertTrue(all(i["status"] == "available" and not i["isInTransit"] for i in items))
        self.assertNotIn("B003", [i["libraryCode"] for i in items])
        self.assertFalse(any(key in i for i in items for key in ["password", "email", "borrowerId"]))

    def test_api_filters_paging_validation(self):
        status, body, _ = self.api("/api/v1/items?type=Book&search=anne&branchId=1&pageSize=1")
        self.assertEqual(200, status)
        self.assertEqual("B001", json.loads(body)["items"][0]["libraryCode"])
        for query in ["page=0", "pageSize=101", "type=Unknown", "branchId=-1"]:
            with self.subTest(query=query):
                self.assertEqual(400, self.api("/api/v1/items?" + query)[0])
        self.assertEqual([], json.loads(self.api("/api/v1/items?page=2147483647")[1])["items"])

    def test_api_categories_status_and_read_only(self):
        self.assertTrue(json.loads(self.api("/api/v1/categories")[1]))
        status = json.loads(self.api("/api/v1/status")[1])
        self.assertTrue(status["readOnly"])
        self.assertEqual(3, len(status["branches"]))
        for method in ["POST", "PUT", "DELETE", "PATCH"]:
            self.assertEqual(405, self.api("/api/v1/items", method=method)[0])

    def test_swagger(self):
        status, body, _ = self.request("/swagger/v1/swagger.json")
        self.assertEqual(200, status)
        schema = json.loads(body)
        self.assertEqual({"/api/v1/items", "/api/v1/categories", "/api/v1/status"}, set(schema["paths"]))
        self.assertEqual("X-Api-Key", schema["components"]["securitySchemes"]["ApiKey"]["name"])
        self.assertTrue(all(set(path) == {"get"} for path in schema["paths"].values()))
        self.assertEqual(200, self.request("/swagger/index.html")[0])

    def test_role_restrictions_and_csrf(self):
        for path in ["/Import", "/Import/Sample", "/Manager/Reports", "/Manager/Export?format=pdf"]:
            self.assertIn(self.request(path)[0], [302, 401])
        for opener, path in [(self.manager, "/Import"), (self.admin, "/Manager/Reports"),
                             (self.reception, "/Import"), (self.reception, "/Manager/Export")]:
            self.assertIn(self.request(path, opener)[0], [302, 403])
        self.assertEqual(400, self.upload((PROJECT / "Samples/catalogue-import.csv").read_bytes(), token=False)[0])

    def test_import_valid_and_duplicate(self):
        before = self.count_items()
        sample = (PROJECT / "Samples/catalogue-import.csv").read_bytes()
        status, body, _ = self.upload(sample)
        self.assertEqual(200, status)
        self.assertIn(b"3 items imported successfully", body)
        self.assertEqual(before + 3, self.count_items())
        with self.database() as db:
            self.assertEqual((2, 2, "available"), db.execute("SELECT CurrentBranchId,HomeBranchId,Status FROM Books WHERE LibraryCode='IMP-B001'").fetchone())
        self.assertIn(b"already exists", self.upload(sample)[1])
        self.assertEqual(before + 3, self.count_items())

    def test_import_atomic_validation(self):
        before = self.count_items()
        text = (PROJECT / "Samples/catalogue-import.csv").read_text().replace("IMP-", "BAD-").replace("NORTH", "MISSING")
        self.assertIn(b"valid BranchCode", self.upload(text.encode())[1])
        self.assertEqual(before, self.count_items())
        self.assertIn(b"header", self.upload(b"bad,header\n")[1])
        text = (PROJECT / "Samples/catalogue-import.csv").read_text().replace("IMP-", "BAD2-").replace("2025", "not-a-year")
        self.assertIn(b"valid Year", self.upload(text.encode())[1])
        self.assertEqual(before, self.count_items())

    def test_import_quoted_unicode_and_duplicate_in_file(self):
        sample = list(csv.reader(io.StringIO((PROJECT / "Samples/catalogue-import.csv").read_text())))
        sample[1][1:4] = ["UNI-B001", 'Café, "Distributed"', "A description\nwith a new line"]
        content = io.StringIO(newline="")
        csv.writer(content).writerows(sample[:2])
        self.assertIn(b"1 items imported successfully", self.upload(content.getvalue().encode())[1])
        with self.database() as db:
            self.assertEqual('Café, "Distributed"', db.execute("SELECT Name FROM Books WHERE LibraryCode='UNI-B001'").fetchone()[0])
        sample[1][1] = "REPEATED-CODE"
        content = io.StringIO(newline="")
        csv.writer(content).writerows([sample[0], sample[1], sample[1]])
        before = self.count_items()
        self.assertIn(b"repeated", self.upload(content.getvalue().encode())[1])
        self.assertEqual(before, self.count_items())

    def test_import_size_and_malformed_input(self):
        before = self.count_items()
        sample = (PROJECT / "Samples/catalogue-import.csv").read_bytes()
        for content, filename in [(b"", "empty.csv"), (sample, "bad.txt"), (b"x" * (1024 * 1024 + 1), "big.csv")]:
            self.assertIn(b"no larger than 1 MB", self.upload(content, filename=filename)[1])
        header = sample.splitlines()[0] + b"\n"
        self.assertIn(b"Invalid CSV quoting", self.upload(header + b'Book,"unfinished\n')[1])
        self.assertIn(b"no items", self.upload(header)[1])
        row = b'Book,ROW-LIMIT,Example,Description,Author,Genre,ISBN,,,,,CENTRAL\n'
        self.assertIn(b"at most 1000", self.upload(header + row * 1001)[1])
        self.assertEqual(before, self.count_items())

    def test_reports_validation_and_totals(self):
        query = "Kind=fines&From=2025-01-02&To=2025-01-02"
        status, body, _ = self.request("/Manager/Reports?" + query, self.manager)
        self.assertEqual(200, status)
        self.assertIn(b"60.00", body)
        self.assertIn(b"40.00", body)
        self.assertIn(b"20.00", body)
        self.assertNotIn(b"Outside range", body)
        status, body, _ = self.request("/Manager/Reports?From=2025-02-01&To=2025-01-01", self.manager)
        self.assertEqual(200, status)
        self.assertIn(b"From date must", body)
        for query in ["Kind=unknown", "From=invalid", "From=2025-02-01&To=2025-01-01", "Kind=inventory&From=2025-01-01", "format=exe"]:
            self.assertEqual(400, self.request("/Manager/Export?" + query, self.manager)[0])

    def test_pdf_pagination(self):
        with self.database() as db:
            db.executemany("INSERT INTO Toys (Name,Description,LibraryCode,Status,ToyType,Age) VALUES (?,?,?,?,?,?)",
                           [(f"Pagination fixture {i:03}", "Temporary fixture", f"PAGE-{i}", "available", "Test", 3) for i in range(90)])
        try:
            status, body, _ = self.request("/Manager/Export?Kind=inventory&format=pdf", self.manager)
            self.assertEqual(200, status)
            self.assertTrue(body.startswith(b"%PDF"))
            output = os.environ.get("MEMBER3_TEST_OUTPUT")
            if output:
                (Path(output) / "inventory-multipage.pdf").write_bytes(body)
        finally:
            with self.database() as db:
                db.execute("DELETE FROM Toys WHERE LibraryCode LIKE 'PAGE-%'")

    def test_all_exports_and_formula_protection(self):
        output = os.environ.get("MEMBER3_TEST_OUTPUT")
        if output:
            Path(output).mkdir(parents=True, exist_ok=True)
        for kind in ["borrowing", "fines", "inventory"]:
            query = f"Kind={kind}" + ("&From=2025-01-02&To=2025-01-02" if kind != "inventory" else "")
            for format in ["csv", "pdf"]:
                with self.subTest(kind=kind, format=format):
                    status, body, headers = self.request(f"/Manager/Export?{query}&format={format}", self.manager)
                    self.assertEqual(200, status, body[:500])
                    self.assertIn("attachment", headers["Content-Disposition"])
                    if format == "pdf":
                        self.assertTrue(body.startswith(b"%PDF"), body[:200])
                    else:
                        self.assertIn("text/csv", headers["Content-Type"])
                        rows = list(csv.reader(io.StringIO(body.decode("utf-8-sig"))))
                        self.assertTrue(rows)
                        if kind == "fines":
                            self.assertIn(["Fines charged (AUD)", "60.00"], rows)
                            self.assertIn(["Fines paid (AUD)", "40.00"], rows)
                            self.assertTrue(any("'=SUM(1,2)" in row for row in rows))
                            self.assertTrue(any('Café, "quoted" report' in row for row in rows))
                    if output:
                        (Path(output) / f"{kind}.{format}").write_bytes(body)

    def test_existing_pages_still_render(self):
        for path, opener in [("/", None), ("/Kiosk", None), ("/Item", self.admin),
                             ("/Manager", self.manager), ("/BranchTransfer/ReceptionDesk", self.reception)]:
            with self.subTest(path=path):
                self.assertEqual(200, self.request(path, opener)[0])

    def test_team_reservation_return_notifications_integration(self):
        status, _, _ = self.request("/Borrow/BorrowItem", self.reception, "POST",
            urllib.parse.urlencode({"libraryCode": "B002", "borrowerId": 1001}).encode(),
            {"Content-Type": "application/x-www-form-urlencoded"})
        self.assertEqual(302, status)
        with self.database() as db:
            book_id = db.execute("SELECT Id FROM Books WHERE LibraryCode='B002'").fetchone()[0]
            loan_id = db.execute("SELECT MAX(Id) FROM BorrowTransactions WHERE ItemType='Book' AND ItemId=?", (book_id,)).fetchone()[0]
            self.assertTrue(db.execute("SELECT COUNT(*) FROM Notifications WHERE NotifyType='Borrowed' AND BorrowerId=1001").fetchone()[0])
        items = json.loads(self.api("/api/v1/items?search=Matilda")[1])["items"]
        self.assertEqual([], items)
        for name, email in [("John Smith", "john@example.com"), ("Queue test", "queue-test@example.com")]:
            status, body, _ = self.request("/Reservation/Reserve", method="POST",
                data=urllib.parse.urlencode({"type": "Book", "id": book_id, "name": name, "email": email}).encode(),
                headers={"Content-Type": "application/x-www-form-urlencoded"})
            self.assertEqual(200, status)
            self.assertIn(b"in line", body)
        status, _, _ = self.request("/Borrow/ReturnItem", self.reception, "POST",
            urllib.parse.urlencode({"id": loan_id}).encode(), {"Content-Type": "application/x-www-form-urlencoded"})
        self.assertEqual(302, status)
        with self.database() as db:
            queue = db.execute("SELECT Status FROM Reservations WHERE ItemType='Book' AND ItemId=? ORDER BY ReservedDate,Id", (book_id,)).fetchall()
            self.assertEqual([("notified",), ("waiting",)], queue)
            self.assertTrue(db.execute("SELECT COUNT(*) FROM Notifications WHERE Message LIKE '%now available%'").fetchone()[0])
        items = json.loads(self.api("/api/v1/items?search=Matilda")[1])["items"]
        self.assertEqual(1, len(items))
        for path, opener in [("/Reservation", self.reception), ("/Notification", self.reception), ("/Notification", self.manager)]:
            self.assertEqual(200, self.request(path, opener)[0])

    def test_z_rate_limit(self):
        # Run last: exhaust the real per-IP window without slowing the remaining checks.
        statuses = [self.api("/api/v1/status")[0] for _ in range(125)]
        self.assertIn(429, statuses)


if __name__ == "__main__":
    unittest.main(verbosity=2)
