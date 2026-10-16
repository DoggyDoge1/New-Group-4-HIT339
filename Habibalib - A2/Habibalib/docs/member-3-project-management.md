# Member 3 project management working draft

Status: proposed planning material, not a record of completed meetings, approved commitments or actual hours. Review with all members and replace the pending fields before submission.

## Board setup

Create a GitHub Project for `DoggyDoge1/New-Group-4-HIT339` with columns **Backlog**, **Ready**, **In progress**, **Review/testing** and **Done**. Create one issue per manageable task and assign it to the agreed owner. Record acceptance criteria, estimated hours, actual hours, dependencies and a link to the relevant pull request or document. Use the board as the project-plan overview and keep dates/statuses current.

Board URL: **pending team setup**.

Marker access: **pending verification**. Confirm read access using an account that is not a project owner, or supply the required read credentials through the assessment's approved method. If using a trial service, the brief requires it to remain active for two weeks after submission.

## Proposed work breakdown

Hours below are starting estimates for team review. Actual hours are deliberately blank and must be recorded truthfully. Team-wide estimates should also be reviewed by the relevant owners.

| ID | Task | Owner | Estimate (hours) | Actual hours | Depends on | Acceptance evidence |
|---|---|---|---:|---|---|---|
| M3-01 | Inspect baseline and agree API/import/report scope | Member 3 | 2 | Pending | Chosen baseline | Agreed requirements and notes |
| M3-02 | Upgrade project target and check baseline compatibility | Member 3 + team review | 2 | Pending | M3-01 | .NET 10 build and regression checks |
| M3-03 | Implement API-key access and API routes | Member 3 | 4 | Pending | M3-01 | Success, 401 and validation responses |
| M3-04 | Configure Swagger and API demonstration | Member 3 | 1 | Pending | M3-03 | Swagger showing all three routes |
| M3-05 | Implement CSV validation and transactional import | Member 3 | 4 | Pending | Shared branch/item fields | Valid import, rejected rows, duplicate checks |
| M3-06 | Build manager borrowing, fine and inventory reports | Member 3 | 4 | Pending | Loan/item models | Reconciled totals and filters |
| M3-07 | Add CSV and PDF exports | Member 3 | 3 | Pending | M3-06 | All three reports in both formats |
| M3-08 | Run integrated checks and resolve defects | Member 3 + team | 3 | Pending | M3-03 to M3-07 | Recorded test results |
| M3-09 | Set up project board and maintain task hours | Member 3 | 2 | Pending | Team agreement | Accessible board with assigned tasks |
| M3-10 | Gather evidence for strengths and weaknesses | Member 3 + team | 1 | Pending | Team reflections | Specific examples and mitigations |
| M3-11 | Assemble and review final portfolio | Member 3 | 3 | Pending | All members' documents | Complete reviewed PDF |
| M1-01 | Complete kiosk and branch integration evidence | Member 1 | Team to estimate | Pending | Baseline modules | Agreed feature tests |
| M1-02 | Installation, location branches, VM test and patron guide | Member 1 | Team to estimate | Pending | Integrated application | VM evidence and guide/video |
| M2-01 | Reservations and notification integration | Member 2 | Team to estimate | Pending | Borrow/return/status workflows | Waitlist order and notification evidence |
| M2-02 | Contract, requirements, methodology and minutes | Member 2 | Team to estimate | Pending | Team agreement/real meetings | Completed team documents |

Proposed Member 3 estimate: **29 hours**, excluding additional group regression work and changes following review. This is a planning estimate, not billable time already incurred. No rate or cost is assumed because none was supplied.

## Dependencies to discuss

- Member 1: confirm branch IDs/codes, available/in-transit rules and any planned changes to music/toy locations.
- Member 2: make imported items usable in reservations and notification workflows, and ensure report totals reflect the integrated borrowing records.
- Whole team: agree the .NET 10 toolchain and branch/merge workflow; review any changes to shared models and `Program.cs` before merging.
- Whole team: provide real dates, names, working hours, board access and evidence for portfolio completion.

## Strengths and weaknesses: evidence prompts

The following are observations about the current implementation, not claims about individual team members. Turn them into a team reflection only after everyone reviews them.

| Observation | Possible strength or limitation | Evidence/action needed |
|---|---|---|
| Members have separate module ownership | Scope is divided into concrete responsibilities | Cite actual tasks, handoffs and reviews |
| Existing MVC controllers and shared SQLite data are reused | New features can build on the previous assessment | Cite working integration examples |
| Book branches and in-transit status already exist | Import/API can follow existing availability rules | Demonstrate a transferred book being excluded |
| Music/toys lack branch fields | Multi-branch reporting is incomplete for those types | Agree scope with Member 1 and document the decision |
| Loans have payment state but no payment date | Cash receipts cannot be grouped accurately by receipt date | Keep report wording accurate or agree a model extension |
| Baseline authentication stores plain-text demo passwords | Existing authentication is limited to assignment/demo use | Team to decide whether to improve it and record remaining scope |

For each final weakness, explain its actual effect and the action the team took. Do not invent missed meetings, disputes, strengths, hours or outcomes.
