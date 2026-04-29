using EnterpriseCodeIntel.Core;
using EnterpriseCodeIntel.Exporters;

// Usage:
//   scan <source-root> --out <output-directory> [--format json|markdown|csv|html|all]
//
// --format defaults to "all". Multiple formats can be comma-separated: --format json,markdown

if (args.Length < 1 || !string.Equals(args[0], "scan", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine(
        "Usage: EnterpriseCodeIntel.Cli scan <source-root> --out <output-directory> [--format json|markdown|csv|html|all]");
    return 1;
}

var sourceRoot = args.ElementAtOrDefault(1);
if (string.IsNullOrWhiteSpace(sourceRoot))
{
    Console.Error.WriteLine("Missing required <source-root> argument.");
    return 1;
}

var outIndex = Array.FindIndex(args, arg => string.Equals(arg, "--out", StringComparison.OrdinalIgnoreCase));
if (outIndex < 0 || outIndex + 1 >= args.Length)
{
    Console.Error.WriteLine("Missing required --out <output-directory> argument.");
    return 1;
}

var outputDirectory = args[outIndex + 1];

var formatIndex = Array.FindIndex(args, arg => string.Equals(arg, "--format", StringComparison.OrdinalIgnoreCase));
var formatValue = formatIndex >= 0 && formatIndex + 1 < args.Length ? args[formatIndex + 1] : "all";
var formats = formatValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(f => f.ToLowerInvariant())
    .ToHashSet();

if (!formats.All(f => f is "json" or "markdown" or "csv" or "html" or "all"))
{
    Console.Error.WriteLine("Invalid --format value. Supported: json, markdown, csv, html, all.");
    return 1;
}

try
{
    var scanner = new SourceScanner();
    var exporter = new InventoryExporter();

    // sourceRoot is kept as the caller-provided value to avoid embedding machine-specific
    // absolute paths (e.g., build agent paths) into generated report artifacts.
    var inventory = scanner.Scan(sourceRoot);

    Directory.CreateDirectory(outputDirectory);

    var emitAll = formats.Contains("all");

    if (emitAll || formats.Contains("json"))
        File.WriteAllText(Path.Combine(outputDirectory, "api-inventory.json"), exporter.ToJson(inventory));

    if (emitAll || formats.Contains("markdown"))
        File.WriteAllText(Path.Combine(outputDirectory, "api-docs.md"), exporter.ToMarkdown(inventory));

    if (emitAll || formats.Contains("csv"))
        File.WriteAllText(Path.Combine(outputDirectory, "api-inventory.csv"), exporter.ToCsv(inventory));

    if (emitAll || formats.Contains("html"))
        File.WriteAllText(Path.Combine(outputDirectory, "api-report.html"), exporter.ToHtml(inventory));

    Console.WriteLine($"Discovered {inventory.TotalItems} items.");
    Console.WriteLine($"Output written to: {Path.GetFullPath(outputDirectory)}");
    return 0;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Invalid argument: {ex.Message}");
    return 1;
}
catch (DirectoryNotFoundException ex)
{
    Console.Error.WriteLine($"Directory not found: {ex.Message}");
    return 1;
}
catch (UnauthorizedAccessException ex)
{
    Console.Error.WriteLine($"Access denied: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex}");
    return 2;
}
