using EnterpriseCodeIntel.Core;
using EnterpriseCodeIntel.Exporters;
using Xunit;

namespace EnterpriseCodeIntel.Tests;

// ── shared fixture ────────────────────────────────────────────────────────────

file static class TestInventory
{
    public static CodeInventory WithEndpoint(string controller = "OrdersController",
        string method = "GetById", string verb = "GET", string route = "/api/orders/{id}",
        string returnType = "ActionResult<OrderDto>") =>
        new(
            SourceRoot: "./sample",
            GeneratedAtUtc: DateTimeOffset.Parse("2026-04-29T00:00:00Z"),
            Endpoints:
            [
                new DiscoveredEndpoint(controller, method, verb, route, returnType,
                    ["Guid id"], "Controllers/OrdersController.cs")
            ],
            Functions: [],
            Services: []);

    public static CodeInventory WithFunction(string name = "DocumentRetentionSweep",
        string trigger = "timer", string methodName = "Run") =>
        new(
            SourceRoot: "./sample",
            GeneratedAtUtc: DateTimeOffset.Parse("2026-04-29T00:00:00Z"),
            Endpoints: [],
            Functions: [new DiscoveredFunction(name, trigger, methodName, "Functions/DocRetention.cs")],
            Services: []);

    public static CodeInventory WithService(string className = "LoanApplicationService",
        string[] methods = null!) =>
        new(
            SourceRoot: "./sample",
            GeneratedAtUtc: DateTimeOffset.Parse("2026-04-29T00:00:00Z"),
            Endpoints: [],
            Functions: [],
            Services: [new DiscoveredService(className, methods ?? ["ApproveAsync", "RejectAsync", "SubmitAsync"],
                "LoanApplicationService.cs")]);

    public static CodeInventory Empty() =>
        new("./sample", DateTimeOffset.Parse("2026-04-29T00:00:00Z"), [], [], []);
}

// ── markdown exporter ─────────────────────────────────────────────────────────

public sealed class MarkdownExporterTests
{
    [Fact]
    public void IncludesEndpointInventory()
    {
        var markdown = new InventoryExporter().ToMarkdown(TestInventory.WithEndpoint());

        Assert.Contains("OrdersController", markdown);
        Assert.Contains("/api/orders/{id}", markdown);
        Assert.Contains("ActionResult<OrderDto>", markdown);
        Assert.Contains("GET", markdown);
    }

    [Fact]
    public void IncludesFunctionWithMethodName()
    {
        var markdown = new InventoryExporter().ToMarkdown(TestInventory.WithFunction());

        Assert.Contains("DocumentRetentionSweep", markdown);
        Assert.Contains("timer", markdown);
        Assert.Contains("Run", markdown);
    }

    [Fact]
    public void IncludesServicePublicMethods()
    {
        var markdown = new InventoryExporter().ToMarkdown(TestInventory.WithService());

        Assert.Contains("LoanApplicationService", markdown);
        Assert.Contains("ApproveAsync", markdown);
        Assert.Contains("SubmitAsync", markdown);
    }

    [Fact]
    public void EmptyInventoryProducesValidMarkdown()
    {
        var markdown = new InventoryExporter().ToMarkdown(TestInventory.Empty());

        Assert.Contains("# Code Inventory Report", markdown);
        Assert.Contains("## API Endpoints", markdown);
    }
}

// ── json exporter ─────────────────────────────────────────────────────────────

public sealed class JsonExporterTests
{
    [Fact]
    public void ProducesValidJson()
    {
        var json = new InventoryExporter().ToJson(TestInventory.WithEndpoint());

        Assert.Contains("OrdersController", json);
        Assert.Contains("/api/orders/{id}", json);
    }

    [Fact]
    public void SourceRootIsCallerProvidedNotAbsolutePath()
    {
        var json = new InventoryExporter().ToJson(TestInventory.WithEndpoint());

        // Should not contain drive letters or expanded paths — just the
        // caller-supplied value to avoid embedding machine paths in artifacts.
        Assert.Contains("\"./sample\"", json);
    }

    [Fact]
    public void EmptyInventorySerializesToValidJson()
    {
        var json = new InventoryExporter().ToJson(TestInventory.Empty());

        Assert.Contains("\"Endpoints\"", json);
        Assert.Contains("\"Functions\"", json);
        Assert.Contains("\"Services\"", json);
    }
}

// ── csv exporter ──────────────────────────────────────────────────────────────

public sealed class CsvExporterTests
{
    [Fact]
    public void EndpointRowHasCorrectNumberOfColumns()
    {
        var csv = new InventoryExporter().ToCsv(TestInventory.WithEndpoint());
        var dataLine = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1) // header
            .First(l => l.StartsWith("endpoint"));

        // Header has 6 columns; endpoint rows must match.
        var headerLine = csv.Split('\n').First();
        var headerCount = headerLine.Split(',').Length;
        var rowCount = dataLine.Split(',').Length;

        Assert.Equal(headerCount, rowCount);
    }

    [Fact]
    public void FunctionRowHasCorrectNumberOfColumns()
    {
        var csv = new InventoryExporter().ToCsv(TestInventory.WithFunction());
        var headerLine = csv.Split('\n').First();
        var dataLine = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .First(l => l.StartsWith("function"));

        // Empty fields for route_or_methods produce empty column, not missing column.
        var headerCount = headerLine.Split(',').Length;
        var rowCount = dataLine.Split(',').Length;

        Assert.Equal(headerCount, rowCount);
    }

    [Fact]
    public void ValuesWithCommasAreQuoted()
    {
        var inventory = TestInventory.WithService(methods: ["Method,One", "MethodTwo"]);
        var csv = new InventoryExporter().ToCsv(inventory);

        Assert.Contains("\"Method,One;MethodTwo\"", csv);
    }
}

// ── html exporter ─────────────────────────────────────────────────────────────

public sealed class HtmlExporterTests
{
    [Fact]
    public void ProducesValidHtmlShell()
    {
        var html = new InventoryExporter().ToHtml(TestInventory.WithEndpoint());

        Assert.Contains("<!doctype html>", html);
        Assert.Contains("<title>", html);
        Assert.Contains("</html>", html);
    }

    [Fact]
    public void HtmlEncodesSpecialCharactersInRoute()
    {
        // Route containing `<`, `>`, `&` must not produce malformed HTML.
        var inventory = TestInventory.WithEndpoint(route: "/api/items?filter=a&b=c<d>");
        var html = new InventoryExporter().ToHtml(inventory);

        Assert.DoesNotContain("filter=a&b", html);  // raw ampersand must be encoded
        Assert.Contains("&amp;", html);
        Assert.DoesNotContain("<d>", html);           // raw angle brackets must be encoded
        Assert.Contains("&lt;d&gt;", html);
    }

    [Fact]
    public void HtmlEncodesSpecialCharactersInReturnType()
    {
        var inventory = TestInventory.WithEndpoint(returnType: "ActionResult<OrderDto>");
        var html = new InventoryExporter().ToHtml(inventory);

        Assert.DoesNotContain("<OrderDto>", html);
        Assert.Contains("&lt;OrderDto&gt;", html);
    }

    [Fact]
    public void EmptyInventoryProducesHtmlWithZeroItems()
    {
        var html = new InventoryExporter().ToHtml(TestInventory.Empty());

        Assert.Contains("Total discovered items: <strong>0</strong>", html);
    }
}

// ── source scanner ────────────────────────────────────────────────────────────

public sealed class SourceScannerTests
{
    private static readonly string SampleRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "SampleEnterpriseApp"));

    [Fact]
    public void ScanDiscoversLoanApplicationsControllerEndpoints()
    {
        var scanner = new SourceScanner();
        var inventory = scanner.Scan(SampleRoot);

        Assert.NotEmpty(inventory.Endpoints);
        Assert.Contains(inventory.Endpoints, e =>
            e.Controller == "LoanApplicationsController" && e.HttpVerb == "GET");
        Assert.Contains(inventory.Endpoints, e =>
            e.Controller == "LoanApplicationsController" && e.HttpVerb == "POST");
    }

    [Fact]
    public void ScanDiscoversDocumentRetentionFunction()
    {
        var scanner = new SourceScanner();
        var inventory = scanner.Scan(SampleRoot);

        Assert.NotEmpty(inventory.Functions);
        Assert.Contains(inventory.Functions, f => f.FunctionName == "DocumentRetentionSweep");
        Assert.Contains(inventory.Functions, f => f.Trigger == "timer");
    }

    [Fact]
    public void ScanDiscoversLoanApplicationService()
    {
        var scanner = new SourceScanner();
        var inventory = scanner.Scan(SampleRoot);

        Assert.NotEmpty(inventory.Services);
        var svc = Assert.Single(inventory.Services, s => s.ClassName == "LoanApplicationService");
        Assert.Contains("SubmitAsync", svc.PublicMethods);
        Assert.Contains("ApproveAsync", svc.PublicMethods);
        Assert.Contains("RejectAsync", svc.PublicMethods);
    }

    [Fact]
    public void ScanPreservesCallerSuppliedSourceRoot()
    {
        var scanner = new SourceScanner();
        var inventory = scanner.Scan(SampleRoot);

        // SourceRoot should be exactly what was passed in, not an expanded absolute path.
        Assert.Equal(SampleRoot, inventory.SourceRoot);
    }

    [Fact]
    public void ScanThrowsArgumentExceptionForBlankSourceRoot()
    {
        var scanner = new SourceScanner();
        Assert.Throws<ArgumentException>(() => scanner.Scan("   "));
    }

    [Fact]
    public void ScanThrowsDirectoryNotFoundForMissingRoot()
    {
        var scanner = new SourceScanner();
        Assert.Throws<DirectoryNotFoundException>(() => scanner.Scan("./does-not-exist-xyz"));
    }

    [Fact]
    public async Task ScanAsyncProducesSameResultsAsScan()
    {
        var scanner = new SourceScanner();
        var sync = scanner.Scan(SampleRoot);
        var async_ = await scanner.ScanAsync(SampleRoot);

        Assert.Equal(sync.Endpoints.Count, async_.Endpoints.Count);
        Assert.Equal(sync.Functions.Count, async_.Functions.Count);
        Assert.Equal(sync.Services.Count, async_.Services.Count);
    }
}

