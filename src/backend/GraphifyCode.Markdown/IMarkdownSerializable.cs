namespace GraphifyCode.Markdown;

/// <summary>
/// Interface for types that can be serialized to Markdown.
/// This is automatically implemented by the source generator for types marked with [MarkdownSerializable].
/// </summary>
public interface IMarkdownSerializable
{
    /// <summary>
    /// Converts this object to Markdown format.
    /// </summary>
    string ToMarkdown();
}
