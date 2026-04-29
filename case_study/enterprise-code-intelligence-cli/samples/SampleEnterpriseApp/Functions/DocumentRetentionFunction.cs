using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace SampleEnterpriseApp.Functions;

public sealed class DocumentRetentionFunction
{
    [FunctionName("DocumentRetentionSweep")]
    public void Run([TimerTrigger("0 0 2 * * *")] TimerInfo timer, ILogger log)
    {
        log.LogInformation("Document retention sweep started.");
    }
}
