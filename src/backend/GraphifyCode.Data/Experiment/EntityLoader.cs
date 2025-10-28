using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            PathEnumerator: Directory.GetDirectories,
            FilePathEnumerator: (path) => [Path.Combine(path, "service.md")],
            Reader: async (path, filePath, ct) =>
            {
                if (filePath is { } fp
                    && Path.GetDirectoryName(fp) is { } dn
                    && Guid.TryParse(dn, out var entityId)
                    && await File.ReadAllTextAsync(fp, ct) is { } md
                    && Service.FromMarkdown(md) is { } entity)
                {
                    entity.Id = entityId;
                    entity.Endpoints = await Load<Endpoints>(path, ct).FirstOrDefaultAsync(ct);
                    entity.UseCases = await Load<UseCase>(path, ct).ToListAsync(ct);
                    return entity;
                }
                else
                {
                    return null;
                }
            }
        ),
        /* Endpoints */
        [typeof(Endpoints)] = new Loader<Endpoints>(
            PathEnumerator: (pathContext) => [pathContext],
            FilePathEnumerator: (path) => [Path.Combine(path, "endpoints.md")],
            Reader: async (path, filePath, ct) => filePath is { } fp
                    && await File.ReadAllTextAsync(fp, ct) is { } md
                    && Endpoints.FromMarkdown(md) is { } entity
                ? entity
                : null
        ),
        /* UseCases */
        [typeof(UseCase)] = new Loader<UseCase>(
            PathEnumerator: (pathContext) => [Path.Combine(pathContext, "usecases")],
            FilePathEnumerator: (path) => Directory.GetFiles(path, "*.md"),
            Reader: async (path, filePath, ct) =>
            {
                if (filePath is { } fp
                    && Path.GetFileNameWithoutExtension(fp) is { } fn
                    && Guid.TryParse(fn, out var id)
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
}

internal record Loader<TEntity>(
    Func<string, IEnumerable<string>> PathEnumerator,
    Func<string, IEnumerable<string>> FilePathEnumerator,
    Func<string, string, CancellationToken, Task<TEntity?>> Reader
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
}
