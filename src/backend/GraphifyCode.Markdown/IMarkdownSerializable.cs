namespace GraphifyCode.Markdown;

/// <summary>
/// Interface for types that can be serialized to and deserialized from Markdown.
/// This is automatically implemented by the source generator for types marked with [MarkdownSerializable].
/// </summary>
public interface IMarkdownSerializable<TSelf> where TSelf : IMarkdownSerializable<TSelf>
{
    /// <summary>
    /// Converts this object to Markdown format.
    /// </summary>
    string ToMarkdown();

    /// <summary>
    /// Creates an object from Markdown format.
    /// </summary>
    static abstract TSelf FromMarkdown(string markdown);
}
