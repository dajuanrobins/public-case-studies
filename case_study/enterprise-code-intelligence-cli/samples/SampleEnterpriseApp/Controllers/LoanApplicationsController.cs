using Microsoft.AspNetCore.Mvc;

namespace SampleEnterpriseApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LoanApplicationsController : ControllerBase
{
    [HttpGet("{id}")]
    public ActionResult<LoanApplicationDto> GetById(Guid id)
    {
        return Ok(new LoanApplicationDto(id, "UnderReview"));
    }

    [HttpPost]
    public ActionResult<LoanApplicationDto> Create(CreateLoanApplicationRequest request)
    {
        return Created($"/api/loan-applications/{Guid.NewGuid()}", new LoanApplicationDto(Guid.NewGuid(), "Created"));
    }
}

public sealed record LoanApplicationDto(Guid Id, string Status);
public sealed record CreateLoanApplicationRequest(string ApplicantName, decimal RequestedAmount);
