using GraphifyCode.Markdown;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GraphifyCode.Data.Experiment;

[MarkdownSerializable]
public partial class Service : ILoadable<Service>
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }

    [MarkdownIgnore]
    public Endpoints? Endpoints { get; set; }

    [MarkdownIgnore]
    public required List<UseCase> UseCases { get; set; }

    public static async IAsyncEnumerable<Service> Load(string pathContext, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var serviceDir in Directory.GetDirectories(pathContext))
        {
            if (Path.GetDirectoryName(serviceDir) is { } dirName && Guid.TryParse(dirName, out var serviceId))
            {
                var filePath = Path.Combine(serviceDir, "service.md");
                var md = await File.ReadAllTextAsync(filePath, cancellationToken);
                var service = FromMarkdown(md);
                if (service is not null)
                {
                    service.Id = serviceId;
                    service.Endpoints = await Endpoints.Load(serviceDir, service, cancellationToken).FirstOrDefaultAsync(cancellationToken);
                    service.UseCases = await UseCase.Load(serviceDir, service, cancellationToken).ToListAsync(cancellationToken);
                    yield return service;
                }
            }
        }
    }
}

[MarkdownSerializable]
public partial class Endpoints : ILoadable<Endpoints, Service>
{
    public required Endpoint[] EndpointList { get; set; }

    [MarkdownIgnore]
    public required Service Parent { get; set; }

    public static async IAsyncEnumerable<Endpoints> Load(string pathContext, Service parent, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(pathContext, "endpoints.md");
        var md = await File.ReadAllTextAsync(filePath, cancellationToken);
        var endpoints = FromMarkdown(md);
        if (endpoints is not null)
        {
            endpoints.Parent = parent;
            yield return endpoints;
        }
    }
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

[MarkdownSerializable]
public partial class UseCase : ILoadable<UseCase, Service>
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public Guid InitiatingEndpointId { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public required UseCaseStep[] Steps { get; set; }

    [MarkdownIgnore]
    public required Service Parent { get; set; }

    public static async IAsyncEnumerable<UseCase> Load(string pathContext, Service parent, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var useCaseFilePath in Directory.GetFiles(Path.Combine(pathContext, "usecases"), "*.md"))
        {
            if (Path.GetFileNameWithoutExtension(useCaseFilePath) is { } useCaseIdStr && Guid.TryParse(useCaseIdStr, out var useCaseId))
            {
                var md = await File.ReadAllTextAsync(useCaseFilePath, cancellationToken);
                var useCase = FromMarkdown(md);
                if (useCase is not null)
                {
                    useCase.Id = useCaseId;
                    useCase.Parent = parent;
                    yield return useCase;
                }
            }
        }
    }
}

[MarkdownSerializable]
public partial class UseCaseStep
{
    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public Guid? ServiceId { get; set; }

    public Guid? EndpointId { get; set; }

    public string? RelativeCodePath { get; set; }
}

internal interface ILoadable<T>
{
    static abstract IAsyncEnumerable<T> Load(string pathContext, CancellationToken cancellationToken);
}

internal interface ILoadable<T, TParent>
{
    static abstract IAsyncEnumerable<T> Load(string pathContext, TParent parent, CancellationToken cancellationToken);
}
