using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data.Experiment;

internal static class EntityDriver
{
    /// <summary>
    /// Services driver
    /// </summary>
    private static readonly Driver<Service> ServicesDriver = new(
        PathEnumerator: (pathContext) => Directory.Exists(pathContext)
            ? Directory.GetDirectories(pathContext)
            : [],
        FilePathEnumerator: (path) => [Path.Combine(path, "service.md")],
        Reader: async (path, filePath, ct) =>
        {
            if (filePath is { } fp
                && Path.GetDirectoryName(fp) is { } dirPath
                && Path.GetFileName(dirPath) is { } dn
                && Guid.TryParse(dn, out var entityId)
                && File.Exists(fp)
                && await File.ReadAllTextAsync(fp, ct) is { } md
                && Service.FromMarkdown(md) is { } entity)
            {
                entity.Id = entityId;
                await foreach (var endpoints in Load<Endpoints>(path, ct))
                {
                    if (endpoints is not null)
                    {
                        endpoints.Parent = entity;
                        entity.Endpoints = endpoints;
                    }
                }

                entity.UseCases = [];
                await foreach (var useCase in Load<UseCase>(path, ct))
                {
                    if (useCase is not null)
                    {
                        useCase.Parent = entity;
                        entity.UseCases.Add(useCase);
                    }
                }

                return entity;
            }
            else
            {
                return null;
            }
        },
        FilePathResolver: (pathContext, entity) => Path.Combine(pathContext, entity.Id.ToString(), "service.md"),
        Writer: async (filePath, entity, ct) =>
        {
            var md = entity.ToMarkdown();
            await File.WriteAllTextAsync(filePath, md, ct);
        },
        Remover: (filePath, entity, ct) =>
        {
            if (filePath is { } fp
                && Path.GetDirectoryName(fp) is { } dirPath
                && Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath, recursive: true);
            }

            return Task.CompletedTask;
        },
        OwnedCollectionNames: []
    );

    /// <summary>
    /// Endpoints driver
    /// </summary>
    private static readonly Driver<Endpoints> EndpointsDriver = new(
        PathEnumerator: (pathContext) => [pathContext],
        FilePathEnumerator: (path) => [Path.Combine(path, "endpoints.md")],
        Reader: async (path, filePath, ct) => filePath is { } fp
                && File.Exists(fp)
                && await File.ReadAllTextAsync(fp, ct) is { } md
                && Endpoints.FromMarkdown(md) is { } entity
            ? entity
            : null,
        FilePathResolver: (pathContext, entity) => Path.Combine(pathContext, entity.Parent.Id.ToString(), "endpoints.md"),
        Writer: async (filePath, entity, ct) =>
        {
            var md = entity.ToMarkdown();
            await File.WriteAllTextAsync(filePath, md, ct);
        },
        Remover: (filePath, entity, ct) =>
        {
            if (filePath is { } fp && File.Exists(fp))
            {
                File.Delete(filePath);
            }

            return Task.CompletedTask;
        },
        OwnedCollectionNames: [nameof(Endpoints.EndpointList)]
    );

    /// <summary>
    /// UseCases driver
    /// </summary>
    private static readonly Driver<UseCase> UseCasesDriver = new(
        PathEnumerator: (pathContext) => [Path.Combine(pathContext, "usecases")],
        FilePathEnumerator: (path) => Directory.Exists(path)
            ? Directory.GetFiles(path, "*.md")
            : [],
        Reader: async (path, filePath, ct) =>
        {
            if (filePath is { } fp
                && Path.GetFileNameWithoutExtension(fp) is { } fn
                && Guid.TryParse(fn, out var id)
                && File.Exists(fp)
                && await File.ReadAllTextAsync(fp, ct) is { } md
                && UseCase.FromMarkdown(md) is { } entity)
            {
                entity.Id = id;
                return entity;
            }
            else
            {
                return null;
            }
        },
        FilePathResolver: (pathContext, entity) => Path.Combine(pathContext, entity.Parent.Id.ToString(), "usecases", $"{entity.Id}.md"),
        Writer: async (filePath, entity, ct) =>
        {
            var md = entity.ToMarkdown();
            await File.WriteAllTextAsync(filePath, md, ct);
        },
        Remover: (filePath, entity, ct) =>
        {
            if (filePath is { } fp && File.Exists(fp))
            {
                File.Delete(filePath);
            }

            return Task.CompletedTask;
        },
        OwnedCollectionNames: [nameof(UseCase.Steps)]
    );

    public static IAsyncEnumerable<TEntity> Load<TEntity>(
        string pathContext,
        CancellationToken cancellationToken
    ) where TEntity : class
    {
        var loader = typeof(TEntity) switch
        {
            var type when type == typeof(Service) && ServicesDriver is Driver<TEntity> driver => driver,
            var type when type == typeof(Endpoints) && EndpointsDriver is Driver<TEntity> driver => driver,
            var type when type == typeof(UseCase) && UseCasesDriver is Driver<TEntity> driver => driver,
            _ => null
        };

        return loader?.Load(pathContext, cancellationToken) ?? AsyncEnumerable.Empty<TEntity>();
    }

    public static Task Write(string pathContext, object entity, CancellationToken cancellationToken)
    {
        return entity switch
        {
            Service s => ServicesDriver.Write(pathContext, s, cancellationToken),
            Endpoints e => EndpointsDriver.Write(pathContext, e, cancellationToken),
            UseCase u => UseCasesDriver.Write(pathContext, u, cancellationToken),
            _ => Task.CompletedTask
        };
    }

    public static Task Remove(string pathContext, object entity, CancellationToken cancellationToken)
    {
        return entity switch
        {
            Service s => ServicesDriver.Remove(pathContext, s, cancellationToken),
            Endpoints e => EndpointsDriver.Remove(pathContext, e, cancellationToken),
            UseCase u => UseCasesDriver.Remove(pathContext, u, cancellationToken),
            _ => Task.CompletedTask
        };
    }

    public static IEnumerable<string> GetOwnedCollectionNames(object entity)
    {
        return entity switch
        {
            Service => ServicesDriver.OwnedCollectionNames,
            Endpoints => EndpointsDriver.OwnedCollectionNames,
            UseCase => UseCasesDriver.OwnedCollectionNames,
            _ => []
        };
    }
}

internal record Driver<TEntity>(
    Func<string, IEnumerable<string>> PathEnumerator,
    Func<string, IEnumerable<string>> FilePathEnumerator,
    Func<string, string, CancellationToken, Task<TEntity?>> Reader,
    Func<string, TEntity, string> FilePathResolver,
    Func<string, TEntity, CancellationToken, Task> Writer,
    Func<string, TEntity, CancellationToken, Task> Remover,
    IEnumerable<string> OwnedCollectionNames
) where TEntity : class
{
    public async IAsyncEnumerable<TEntity> Load(string pathContext, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var path in PathEnumerator(pathContext))
        {
            foreach (var filePath in FilePathEnumerator(path))
            {
                if (filePath is { } fp && await Reader(path, fp, cancellationToken) is { } entity)
                {
                    yield return entity;
                }
            }
        }
    }

    public Task Write(string pathContext, TEntity entity, CancellationToken cancellationToken)
    {
        var filePath = FilePathResolver(pathContext, entity);
        return Writer(filePath, entity, cancellationToken);
    }

    public Task Remove(string pathContext, TEntity entity, CancellationToken cancellationToken)
    {
        var filePath = FilePathResolver(pathContext, entity);
        return Remover(filePath, entity, cancellationToken);
    }
}
