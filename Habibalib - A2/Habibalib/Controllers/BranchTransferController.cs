using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Habibalib.Data;
using Habibalib.Exceptions;
using Habibalib.Models;
using Habibalib.ViewModels;

namespace Habibalib.Controllers
{
    [Authorize(Roles = "Reception")]
    [ServiceFilter(typeof(BranchTransferExceptionFilter))]
    public class BranchTransferController : Controller
    {
        private const string ActiveBranchSessionKey = "ActiveReceptionBranchId";
        private readonly AppDbContext db;
        private readonly IConfiguration configuration;

        public BranchTransferController(AppDbContext context, IConfiguration configuration)
        {
            db = context;
            this.configuration = configuration;
        }

        public IActionResult ReceptionDesk(int? branchId)
        {
            if (branchId.HasValue)
            {
                if (!db.Branches.Any(branch => branch.Id == branchId.Value))
                {
                    return TransferError("The selected branch does not exist.", StatusCodes.Status404NotFound);
                }

                HttpContext.Session.SetInt32(ActiveBranchSessionKey, branchId.Value);
            }

            var activeBranchId = GetActiveBranchId();
            var activeBranch = db.Branches.Find(activeBranchId);
            if (activeBranch == null)
            {
                return TransferError(
                    "The configured reception branch does not exist. Set BranchSettings:LocationBranchId to an existing branch.",
                    StatusCodes.Status500InternalServerError);
            }

            var model = new BranchReceptionDeskViewModel
            {
                ActiveBranch = activeBranch,
                Branches = db.Branches.OrderBy(branch => branch.Name).ToList(),
                LocalBooks = db.Books
                    .Include(book => book.HomeBranch)
                    .Where(book => book.CurrentBranchId == activeBranchId)
                    .OrderBy(book => book.Name)
                    .ToList(),
                IncomingTransfers = db.BranchTransfers
                    .Include(transfer => transfer.Book)
                    .Include(transfer => transfer.FromBranch)
                    .Where(transfer => transfer.ToBranchId == activeBranchId
                        && transfer.Status == TransferStatus.InTransit)
                    .OrderByDescending(transfer => transfer.RequestedAt)
                    .ToList(),
                OutgoingTransfers = db.BranchTransfers
                    .Include(transfer => transfer.Book)
                    .Include(transfer => transfer.ToBranch)
                    .Where(transfer => transfer.FromBranchId == activeBranchId
                        && (transfer.Status == TransferStatus.Requested || transfer.Status == TransferStatus.InTransit))
                    .OrderByDescending(transfer => transfer.RequestedAt)
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestTransfer(int bookId, int toBranchId, string notes)
        {
            var currentBranchId = GetActiveBranchId();
            var book = db.Books.FirstOrDefault(item =>
                item.Id == bookId && item.CurrentBranchId == currentBranchId);

            if (book == null
                || book.IsInTransit
                || book.Status != "available"
                || toBranchId == currentBranchId
                || !db.Branches.Any(branch => branch.Id == toBranchId))
            {
                return TransferError(
                    "That book cannot be transferred. It may not be at this branch, may already have a transfer, may not be available, or the destination may be invalid.",
                    StatusCodes.Status400BadRequest);
            }

            db.BranchTransfers.Add(new BranchTransfer
            {
                BookId = book.Id,
                FromBranchId = currentBranchId,
                ToBranchId = toBranchId,
                RequestedAt = DateTime.UtcNow,
                Status = TransferStatus.Requested,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                RequestedByStaffId = User.Identity.Name
            });

            book.IsInTransit = true;
            db.SaveChanges();

            TempData["SuccessMessage"] = $"Transfer requested for '{book.Name}'. Dispatch it when it leaves this branch.";
            return RedirectToAction(nameof(ReceptionDesk));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DispatchTransfer(int transferId)
        {
            var transfer = db.BranchTransfers.FirstOrDefault(item =>
                item.Id == transferId && item.FromBranchId == GetActiveBranchId());

            if (transfer == null || transfer.Status != TransferStatus.Requested)
            {
                return TransferError(
                    "This transfer was not found at the active sending branch or has already been dispatched.",
                    StatusCodes.Status400BadRequest);
            }

            transfer.Status = TransferStatus.InTransit;
            transfer.DispatchedAt = DateTime.UtcNow;
            db.SaveChanges();

            TempData["SuccessMessage"] = "The transfer has been dispatched.";
            return RedirectToAction(nameof(ReceptionDesk));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReceiveTransfer(int transferId)
        {
            var activeBranchId = GetActiveBranchId();
            var transfer = db.BranchTransfers
                .Include(item => item.Book)
                .FirstOrDefault(item =>
                    item.Id == transferId && item.ToBranchId == activeBranchId);

            if (transfer == null || transfer.Status != TransferStatus.InTransit)
            {
                return TransferError(
                    "This transfer is not in transit to the active branch, or it has already been received.",
                    StatusCodes.Status400BadRequest);
            }

            transfer.Status = TransferStatus.Received;
            transfer.CompletedAt = DateTime.UtcNow;
            transfer.Book.CurrentBranchId = activeBranchId;
            transfer.Book.IsInTransit = false;
            db.SaveChanges();

            TempData["SuccessMessage"] = $"'{transfer.Book.Name}' has been added to this branch's inventory.";
            return RedirectToAction(nameof(ReceptionDesk));
        }

        private int GetActiveBranchId()
        {
            return HttpContext.Session.GetInt32(ActiveBranchSessionKey)
                ?? configuration.GetValue<int>("BranchSettings:LocationBranchId", 1);
        }

        private IActionResult TransferError(string message, int statusCode)
        {
            Response.StatusCode = statusCode;
            return View("Error", new BranchTransferErrorViewModel
            {
                StatusCode = statusCode,
                Message = message
            });
        }
    }
}
