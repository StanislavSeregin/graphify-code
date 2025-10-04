using GraphifyCode.Markdown;
using System;

namespace GraphifyCode.Data.Entities;

[MarkdownSerializable]
public partial class Endpoints
{
    public required Endpoint[] EndpointList { get; set; }
}

[MarkdownSerializable]
public partial class Endpoint
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Type { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }
}

public static class EndpointTypes
{
    public const string Http = "http";

    public const string Queue = "queue";

    public const string Job = "job";

    public static bool IsValidEndpointType(string type)
    {
        return type == Http || type == Queue || type == Job;
    }
}
