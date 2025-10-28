using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data.Experiment;

internal static class EntityLoader
{
    private static readonly Dictionary<Type, object> Loaders = new()
    {
        /* Services */
        [typeof(Service)] = new Loader<Service>(
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
                    await foreach(var useCase in Load<UseCase>(path, ct))
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
                File.Delete(filePath);
                return Task.CompletedTask;
            }
        ),
        /* Endpoints */
        [typeof(Endpoints)] = new Loader<Endpoints>(
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
                File.Delete(filePath);
                return Task.CompletedTask;
            }
        ),
        /* UseCases */
        [typeof(UseCase)] = new Loader<UseCase>(
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
                File.Delete(filePath);
                return Task.CompletedTask;
            }
        )
    };

    public static IAsyncEnumerable<TEntity> Load<TEntity>(
        string pathContext,
        CancellationToken cancellationToken
    ) where TEntity : class
    {
        return Loaders.TryGetValue(typeof(TEntity), out var loaderObj) && loaderObj is Loader<TEntity> loader
            ? loader.Load(pathContext, cancellationToken)
            : throw new InvalidOperationException($"No loader registered for type {typeof(TEntity).Name}");
    }

    public static Task Write<TEntity>(
        string pathContext,
        TEntity entity,
        CancellationToken cancellationToken
    ) where TEntity : class
    {
        return Loaders.TryGetValue(typeof(TEntity), out var loaderObj) && loaderObj is Loader<TEntity> loader
            ? loader.Write(pathContext, entity, cancellationToken)
            : throw new InvalidOperationException($"No loader registered for type {typeof(TEntity).Name}");
    }

    public static Task Remove<TEntity>(
        string pathContext,
        TEntity entity,
        CancellationToken cancellationToken
    ) where TEntity : class
    {
        return Loaders.TryGetValue(typeof(TEntity), out var loaderObj) && loaderObj is Loader<TEntity> loader
            ? loader.Remove(pathContext, entity, cancellationToken)
            : throw new InvalidOperationException($"No loader registered for type {typeof(TEntity).Name}");
    }
}

internal record Loader<TEntity>(
    Func<string, IEnumerable<string>> PathEnumerator,
    Func<string, IEnumerable<string>> FilePathEnumerator,
    Func<string, string, CancellationToken, Task<TEntity?>> Reader,
    Func<string, TEntity, string> FilePathResolver,
    Func<string, TEntity, CancellationToken, Task> Writer,
    Func<string, TEntity, CancellationToken, Task> Remover
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
