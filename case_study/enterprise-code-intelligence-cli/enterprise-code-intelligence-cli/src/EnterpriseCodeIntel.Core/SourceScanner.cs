using System.Text.RegularExpressions;

namespace EnterpriseCodeIntel.Core;

public sealed class SourceScanner
{
    private static readonly Regex ClassRegex = new(@"class\s+(?<name>[A-Za-z0-9_]+)", RegexOptions.Compiled);
    private static readonly Regex MethodRegex = new(@"public\s+(?:async\s+)?(?<return>[A-Za-z0-9_<>,\s\?]+)\s+(?<name>[A-Za-z0-9_]+)\s*\((?<params>[^)]*)\)", RegexOptions.Compiled);
    private static readonly Regex RouteRegex = new(@"\[Route\(""(?<route>[^""]+)""\)\]", RegexOptions.Compiled);
    private static readonly Regex HttpVerbRegex = new(@"\[(?<verb>HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)(?:\(""(?<route>[^""]*)""\))?\]", RegexOptions.Compiled);

    // Azure Functions v1/v2/v3 (in-process): [FunctionName("Name")]
    private static readonly Regex FunctionNameAttrRegex = new(@"\[FunctionName\(""(?<name>[^""]+)""\)\]", RegexOptions.Compiled);
    // Azure Functions v4 (isolated worker): [Function("Name")]
    private static readonly Regex FunctionAttrRegex = new(@"\[Function\(""(?<name>[^""]+)""\)\]", RegexOptions.Compiled);

    /// <summary>
    /// Scans all .cs files under <paramref name="sourceRoot"/> and returns a
    /// <see cref="CodeInventory"/> of discovered endpoints, Azure Functions,
    /// and services.
    /// </summary>
    /// <remarks>
    /// Known limitation: the scanner uses regex over text rather than Roslyn
    /// semantic analysis. This means interfaces, abstract base classes, and
    /// non-conventional naming patterns are not discovered. The upgrade path
    /// is to replace the per-file regex walk with a <c>SyntaxWalker</c> from
    /// the Microsoft.CodeAnalysis.CSharp package.
    /// </remarks>
    public CodeInventory Scan(string sourceRoot)
    {
        if (string.IsNullOrWhiteSpace(sourceRoot))
        {
            throw new ArgumentException("A source root is required.", nameof(sourceRoot));
        }

        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Source root not found: {sourceRoot}");
        }

        var files = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var endpoints = new List<DiscoveredEndpoint>();
        var functions = new List<DiscoveredFunction>();
        var services = new List<DiscoveredService>();

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var className = ClassRegex.Match(text).Groups["name"].Value;

            if (string.IsNullOrWhiteSpace(className))
            {
                continue;
            }

            var relativeFile = Path.GetRelativePath(sourceRoot, file);

            if (className.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                endpoints.AddRange(DiscoverEndpoints(text, className, relativeFile));
            }
            else if (IsFunctionFile(text))
            {
                functions.AddRange(DiscoverFunctions(text, relativeFile));
            }
            else if (className.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
            {
                var publicMethods = MethodRegex.Matches(text)
                    .Select(match => match.Groups["name"].Value)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                services.Add(new DiscoveredService(className, publicMethods, relativeFile));
            }
        }

        return new CodeInventory(
            // Preserve the caller-supplied path. Resolving to an absolute path
            // would embed machine-specific or CI agent paths into all outputs.
            SourceRoot: sourceRoot,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Endpoints: endpoints,
            Functions: functions,
            Services: services);
    }

    /// <summary>
    /// Async variant for callers that process large solution trees and want to
    /// avoid blocking the calling thread on I/O. Each file is read
    /// asynchronously; results are merged in source order.
    /// </summary>
    public async Task<CodeInventory> ScanAsync(string sourceRoot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceRoot))
        {
            throw new ArgumentException("A source root is required.", nameof(sourceRoot));
        }

        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Source root not found: {sourceRoot}");
        }

        var files = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var readTasks = files.Select(f => File.ReadAllTextAsync(f, cancellationToken));
        var texts = await Task.WhenAll(readTasks).ConfigureAwait(false);

        var endpoints = new List<DiscoveredEndpoint>();
        var functions = new List<DiscoveredFunction>();
        var services = new List<DiscoveredService>();

        for (var i = 0; i < files.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = files[i];
            var text = texts[i];
            var className = ClassRegex.Match(text).Groups["name"].Value;

            if (string.IsNullOrWhiteSpace(className))
            {
                continue;
            }

            var relativeFile = Path.GetRelativePath(sourceRoot, file);

            if (className.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                endpoints.AddRange(DiscoverEndpoints(text, className, relativeFile));
            }
            else if (IsFunctionFile(text))
            {
                functions.AddRange(DiscoverFunctions(text, relativeFile));
            }
            else if (className.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
            {
                var publicMethods = MethodRegex.Matches(text)
                    .Select(match => match.Groups["name"].Value)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                services.Add(new DiscoveredService(className, publicMethods, relativeFile));
            }
        }

        return new CodeInventory(
            SourceRoot: sourceRoot,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Endpoints: endpoints,
            Functions: functions,
            Services: services);
    }

    private static bool IsFunctionFile(string text) =>
        FunctionNameAttrRegex.IsMatch(text) || FunctionAttrRegex.IsMatch(text);

    private static IEnumerable<DiscoveredEndpoint> DiscoverEndpoints(string text, string controllerName, string sourceFile)
    {
        var controllerRoute = RouteRegex.Match(text).Groups["route"].Value;

        foreach (Match method in MethodRegex.Matches(text))
        {
            var methodStart = Math.Max(0, method.Index - 300);
            var methodPrefix = text.Substring(methodStart, method.Index - methodStart);
            var verbMatch = HttpVerbRegex.Matches(methodPrefix).Cast<Match>().LastOrDefault();

            if (verbMatch is null)
            {
                continue;
            }

            var verb = verbMatch.Groups["verb"].Value.Replace("Http", string.Empty).ToUpperInvariant();
            var methodRoute = verbMatch.Groups["route"].Value;
            var route = CombineRoute(controllerRoute, methodRoute);

            yield return new DiscoveredEndpoint(
                Controller: controllerName,
                MethodName: method.Groups["name"].Value,
                HttpVerb: verb,
                Route: route,
                ReturnType: NormalizeWhitespace(method.Groups["return"].Value),
                Parameters: SplitParameters(method.Groups["params"].Value),
                SourceFile: sourceFile);
        }
    }

    private static IEnumerable<DiscoveredFunction> DiscoverFunctions(string text, string sourceFile)
    {
        // Support both v1/v2/v3 [FunctionName("…")] and v4 isolated-worker [Function("…")].
        var allMatches = FunctionNameAttrRegex.Matches(text).Cast<Match>()
            .Concat(FunctionAttrRegex.Matches(text).Cast<Match>())
            .OrderBy(m => m.Index);

        foreach (var function in allMatches)
        {
            // Trigger attributes ([TimerTrigger], [QueueTrigger], etc.) appear in
            // the method signature, which follows the [FunctionName] attribute.
            // Search the text AFTER the attribute index for the nearest trigger.
            var suffix = text[function.Index..];
            var trigger = suffix.Contains("TimerTrigger", StringComparison.OrdinalIgnoreCase)
                ? "timer"
                : suffix.Contains("QueueTrigger", StringComparison.OrdinalIgnoreCase)
                    ? "queue"
                    : suffix.Contains("HttpTrigger", StringComparison.OrdinalIgnoreCase)
                        ? "http"
                        : suffix.Contains("ServiceBusTrigger", StringComparison.OrdinalIgnoreCase)
                            ? "service-bus"
                            : suffix.Contains("EventHubTrigger", StringComparison.OrdinalIgnoreCase)
                                ? "event-hub"
                                : "unknown";

            var methodMatch = MethodRegex.Matches(text.Substring(function.Index))
                .Cast<Match>()
                .FirstOrDefault();

            yield return new DiscoveredFunction(
                FunctionName: function.Groups["name"].Value,
                Trigger: trigger,
                MethodName: methodMatch?.Groups["name"].Value ?? string.Empty,
                SourceFile: sourceFile);
        }
    }

    private static string CombineRoute(string controllerRoute, string methodRoute)
    {
        var route = string.Join("/", new[] { controllerRoute, methodRoute }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim('/')));

        return "/" + route.Replace("[controller]", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("//", "/", StringComparison.Ordinal)
            .Trim('/');
    }

    private static IReadOnlyList<string> SplitParameters(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeWhitespace)
            .ToArray();
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }
}
