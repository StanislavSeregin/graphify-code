using GraphifyCode.Markdown;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data.Experiment;

public class FullGraph : ILoadable<FullGraph>
{
    public required List<Service> Services { get; set; }

    public static async Task<FullGraph> Load(string pathContext, CancellationToken cancellationToken)
    {
        var self = new FullGraph() { Services = [] };
        foreach (var serviceDir in Directory.GetDirectories(pathContext))
        {
            var service = await Service.Load(serviceDir, self, cancellationToken);
            if (service is not null)
            {
                self.Services.Add(service);
            }
        }

        return self;
    }
}

[MarkdownSerializable]
public partial class Service : ILoadable<Service, FullGraph>
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

    [MarkdownIgnore]
    public required FullGraph Parent { get; set; }

    public static async Task<Service?> Load(string pathContext, FullGraph parent, CancellationToken cancellationToken)
    {
        var serviceId = Guid.Parse(Path.GetDirectoryName(pathContext) ?? throw new InvalidOperationException("Service id should be exists"));
        var filePath = Path.Combine(pathContext, "service.md");
        var md = await File.ReadAllTextAsync(filePath, cancellationToken);
        var self = FromMarkdown(md);
        if (self is not null)
        {
            self.Id = serviceId;
            self.Parent = parent;
            self.Endpoints = await Endpoints.Load(pathContext, self, cancellationToken);
            self.UseCases = [];
            foreach (var useCaseFilePath in Directory.GetFiles(Path.Combine(pathContext, "usecases"), "*.md"))
            {
                var useCase = await UseCase.Load(useCaseFilePath, self, cancellationToken);
                if (useCase is not null)
                {
                    self.UseCases.Add(useCase);
                }
            }
        }

        return self;
    }
}

[MarkdownSerializable]
public partial class Endpoints : ILoadable<Endpoints, Service>
{
    public required Endpoint[] EndpointList { get; set; }

    [MarkdownIgnore]
    public required Service Parent { get; set; }

    public static async Task<Endpoints?> Load(string pathContext, Service parent, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(pathContext, "endpoints.md");
        var md = await File.ReadAllTextAsync(filePath, cancellationToken);
        var self = FromMarkdown(md);
        if (self is not null)
        {
            self.Parent = parent;
        }

        return self;
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

    public static async Task<UseCase?> Load(string pathContext, Service parent, CancellationToken cancellationToken)
    {
        var useCaseId = Guid.Parse(Path.GetFileNameWithoutExtension(pathContext) ?? throw new InvalidOperationException("Usecase id should be exists"));
        var md = await File.ReadAllTextAsync(pathContext, cancellationToken);
        var self = FromMarkdown(md);
        if (self is not null)
        {
            self.Id = useCaseId;
            self.Parent = parent;
        }

        return self;
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
    static abstract Task<T> Load(string pathContext, CancellationToken cancellationToken);
}

internal interface ILoadable<T, TParent>
{
    static abstract Task<T?> Load(string pathContext, TParent parent, CancellationToken cancellationToken);
}
