using System;

namespace GraphifyCode.Markdown;

/// <summary>
/// Provides methods for serializing and deserializing objects to/from Markdown format.
/// </summary>
public static partial class MarkdownSerializer
{
    /// <summary>
    /// Serializes an object to Markdown format.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize. Must be marked with [MarkdownSerializable].</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>Markdown representation of the object.</returns>
    public static string Serialize<T>(T obj) where T : class, IMarkdownSerializable<T>
    {
        return obj.ToMarkdown();
    }

    /// <summary>
    /// Deserializes an object from Markdown format.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize. Must be marked with [MarkdownSerializable].</typeparam>
    /// <param name="markdown">The Markdown string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    public static T Deserialize<T>(string markdown) where T : class, IMarkdownSerializable<T>
    {
        return T.FromMarkdown(markdown);
    }
}
