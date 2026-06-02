using System;

namespace GraphifyCode.MCP.Contracts;

public sealed class McpResult<TData>
{
    public required bool Ok { get; init; }

    public TData? Data { get; init; }

    public McpError? Error { get; init; }

    public string[] Warnings { get; init; } = [];

    public static McpResult<TData> Success(TData data, params string[] warnings)
    {
        return new McpResult<TData>
        {
            Ok = true,
            Data = data,
            Warnings = warnings ?? []
        };
    }

    public static McpResult<TData> Failure(string code, string message, object? details = null, bool retriable = false)
    {
        return new McpResult<TData>
        {
            Ok = false,
            Error = new McpError
            {
                Code = code,
                Message = message,
                Details = details,
                Retriable = retriable
            }
        };
    }
}

public sealed class McpError
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public object? Details { get; init; }

    public bool Retriable { get; init; }
}

public enum GraphEntityType
{
    Service = 1,
    Endpoint = 2,
    UseCase = 3
}

public sealed class ListServicesData
{
    public required GraphifyCode.Data.Models.ServicesOverview Services { get; init; }

    public required string Markdown { get; init; }
}

public sealed class GetServiceData
{
    public required GraphifyCode.Data.Models.ServicesDetails Service { get; init; }

    public required string Markdown { get; init; }
}

public sealed class GetUseCaseData
{
    public required GraphifyCode.Data.Models.UseCasesDetails UseCase { get; init; }

    public required string Markdown { get; init; }
}

public sealed class SearchGraphData
{
    public required SearchMatch[] Matches { get; init; }
}

public sealed class SearchMatch
{
    public required GraphEntityType EntityType { get; init; }

    public required Guid EntityId { get; init; }

    public required string Name { get; init; }

    public required Guid ServiceId { get; init; }

    public string? RelativeCodePath { get; init; }
}

public sealed class UpsertServiceRequest
{
    public Guid? ServiceId { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public string? RelativeCodePath { get; init; }
}

public sealed class UpsertEndpointRequest
{
    public Guid ServiceId { get; init; }

    public Guid? EndpointId { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string Type { get; init; }

    public string? RelativeCodePath { get; init; }
}

public sealed class UpsertUseCaseRequest
{
    public Guid ServiceId { get; init; }

    public Guid? UseCaseId { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public Guid InitiatingEndpointId { get; init; }
}

public sealed class UpsertRelationRequest
{
    public Guid UseCaseId { get; init; }

    public required string StepName { get; init; }

    public required string StepDescription { get; init; }

    public Guid? ServiceId { get; init; }

    public Guid? EndpointId { get; init; }

    public string? RelativeCodePath { get; init; }
}

public sealed class MutationResultData
{
    public required GraphEntityType EntityType { get; init; }

    public required Guid EntityId { get; init; }

    public required string Action { get; init; }

    public string? Message { get; init; }
}
