# Code Inventory Report

Generated: `2026-04-29T00:00:00+00:00`

## API Endpoints

| Verb | Route | Controller | Method | Return Type |
|---|---|---|---|---|
| GET | `/api/loan-applications/{id}` | LoanApplicationsController | GetById | `ActionResult<LoanApplicationDto>` |
| POST | `/api/loan-applications` | LoanApplicationsController | Create | `ActionResult<LoanApplicationDto>` |

## Azure Functions

| Function | Trigger | Source |
|---|---|---|
| DocumentRetentionSweep | timer | `Functions/DocumentRetentionFunction.cs` |

## Services

| Service | Public Methods | Source |
|---|---|---|
| LoanApplicationService | ApproveAsync, RejectAsync, SubmitAsync | `LoanApplicationService.cs` |
