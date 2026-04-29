namespace EnterpriseCodeIntel.Core;

public sealed record CodeInventory(
    string SourceRoot,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<DiscoveredEndpoint> Endpoints,
    IReadOnlyList<DiscoveredFunction> Functions,
    IReadOnlyList<DiscoveredService> Services)
{
    public int TotalItems => Endpoints.Count + Functions.Count + Services.Count;
}

public sealed record DiscoveredEndpoint(
    string Controller,
    string MethodName,
    string HttpVerb,
    string Route,
    string ReturnType,
    IReadOnlyList<string> Parameters,
    string SourceFile);

public sealed record DiscoveredFunction(
    string FunctionName,
    string Trigger,
    string MethodName,
    string SourceFile);

public sealed record DiscoveredService(
    string ClassName,
    IReadOnlyList<string> PublicMethods,
    string SourceFile);
