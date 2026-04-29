namespace SampleEnterpriseApp;

public sealed class LoanApplicationService
{
    public Task SubmitAsync(Guid applicationId) => Task.CompletedTask;

    public Task ApproveAsync(Guid applicationId, string reviewerId) => Task.CompletedTask;

    public Task RejectAsync(Guid applicationId, string reviewerId, string reason) => Task.CompletedTask;
}
