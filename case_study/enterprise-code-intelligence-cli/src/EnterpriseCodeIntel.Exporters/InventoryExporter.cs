using System.Net;
using System.Text;
using System.Text.Json;
using EnterpriseCodeIntel.Core;

namespace EnterpriseCodeIntel.Exporters;

public sealed class InventoryExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public void ExportAll(CodeInventory inventory, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        File.WriteAllText(Path.Combine(outputDirectory, "api-inventory.json"), ToJson(inventory), Encoding.UTF8);
        File.WriteAllText(Path.Combine(outputDirectory, "api-docs.md"), ToMarkdown(inventory), Encoding.UTF8);
        File.WriteAllText(Path.Combine(outputDirectory, "api-inventory.csv"), ToCsv(inventory), Encoding.UTF8);
        File.WriteAllText(Path.Combine(outputDirectory, "api-report.html"), ToHtml(inventory), Encoding.UTF8);
    }

    public string ToJson(CodeInventory inventory) => JsonSerializer.Serialize(inventory, JsonOptions);

    public string ToMarkdown(CodeInventory inventory)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Code Inventory Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: `{inventory.GeneratedAtUtc:O}`");
        builder.AppendLine($"Source root: `{inventory.SourceRoot}`");
        builder.AppendLine();

        builder.AppendLine("## API Endpoints");
        builder.AppendLine("| Verb | Route | Controller | Method | Return Type |");
        builder.AppendLine("|---|---|---|---|---|");

        foreach (var endpoint in inventory.Endpoints)
        {
            builder.AppendLine($"| {endpoint.HttpVerb} | `{endpoint.Route}` | {endpoint.Controller} | {endpoint.MethodName} | `{endpoint.ReturnType}` |");
        }

        builder.AppendLine();
        builder.AppendLine("## Azure Functions");
        builder.AppendLine("| Function | Trigger | Method | Source |");
        builder.AppendLine("|---|---|---|---|");

        foreach (var function in inventory.Functions)
        {
            builder.AppendLine($"| {function.FunctionName} | {function.Trigger} | {function.MethodName} | `{function.SourceFile}` |");
        }

        builder.AppendLine();
        builder.AppendLine("## Services");
        builder.AppendLine("| Service | Public Methods | Source |");
        builder.AppendLine("|---|---|---|");

        foreach (var service in inventory.Services)
        {
            builder.AppendLine($"| {service.ClassName} | {string.Join(", ", service.PublicMethods)} | `{service.SourceFile}` |");
        }

        return builder.ToString();
    }

    public string ToCsv(CodeInventory inventory)
    {
        var builder = new StringBuilder();
        builder.AppendLine("kind,name,verb_or_trigger,route_or_methods,method_name,source_file");

        foreach (var endpoint in inventory.Endpoints)
        {
            builder.AppendLine(
                $"endpoint,{Escape(endpoint.MethodName)},{endpoint.HttpVerb},{Escape(endpoint.Route)},{Escape(endpoint.MethodName)},{Escape(endpoint.SourceFile)}");
        }

        foreach (var function in inventory.Functions)
        {
            builder.AppendLine(
                $"function,{Escape(function.FunctionName)},{function.Trigger},,{Escape(function.MethodName)},{Escape(function.SourceFile)}");
        }

        foreach (var service in inventory.Services)
        {
            builder.AppendLine(
                $"service,{Escape(service.ClassName)},,{Escape(string.Join(";", service.PublicMethods))},,{Escape(service.SourceFile)}");
        }

        return builder.ToString();
    }

    public string ToHtml(CodeInventory inventory)
    {
        var rows = string.Join(Environment.NewLine, inventory.Endpoints.Select(endpoint =>
            $"<tr>" +
            $"<td>{WebUtility.HtmlEncode(endpoint.HttpVerb)}</td>" +
            $"<td><code>{WebUtility.HtmlEncode(endpoint.Route)}</code></td>" +
            $"<td>{WebUtility.HtmlEncode(endpoint.Controller)}</td>" +
            $"<td>{WebUtility.HtmlEncode(endpoint.MethodName)}</td>" +
            $"<td><code>{WebUtility.HtmlEncode(endpoint.ReturnType)}</code></td>" +
            $"</tr>"));

        return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>Enterprise Code Intelligence Report</title>" +
               "<style>body{font-family:system-ui,sans-serif;background:#f7f7f5;color:#171717;margin:0}main{max-width:960px;margin:48px auto;padding:40px;background:white;border:1px solid #d8d8d2}h1{font-size:40px;letter-spacing:-.04em}table{width:100%;border-collapse:collapse}th,td{border:1px solid #d8d8d2;padding:10px;text-align:left}th{background:#eeeeeb;text-transform:uppercase;font-size:12px;letter-spacing:.08em}code{background:#eeeeeb;padding:2px 5px}.muted{color:#5f5f5f}</style></head>" +
               $"<body><main><p class=\"muted\">Enterprise Code Intelligence</p><h1>Code Inventory Report</h1><p>Total discovered items: <strong>{inventory.TotalItems}</strong></p><h2>Endpoints</h2><table><thead><tr><th>Verb</th><th>Route</th><th>Controller</th><th>Method</th><th>Return Type</th></tr></thead><tbody>{rows}</tbody></table></main></body></html>";
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}
