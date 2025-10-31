using System;

namespace GraphifyCode.Markdown;

/// <summary>
/// Marks a type for Markdown serialization code generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class MarkdownSerializableAttribute : Attribute
{
    /// <summary>
    /// Gets the header name to use instead of the type name.
    /// </summary>
    public string? HeaderName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownSerializableAttribute"/> class.
    /// </summary>
    public MarkdownSerializableAttribute() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownSerializableAttribute"/> class with a custom header name.
    /// </summary>
    /// <param name="headerName">The header name to use instead of the type name.</param>
    public MarkdownSerializableAttribute(string headerName)
    {
        HeaderName = headerName;
    }
}

/// <summary>
/// Marks a property whose value should be used as the markdown header when serializing objects in arrays.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class MarkdownHeaderAttribute : Attribute { /* NOTHING */ }

/// <summary>
/// Marks a property that should be excluded from Markdown serialization and deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class MarkdownIgnoreAttribute : Attribute { /* NOTHING */ }

/// <summary>
/// Marks a collection property that should be rendered with an intermediate subheader.
/// This increases the nesting level of collection elements by adding a header before them.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class MarkdownSubHeaderAttribute : Attribute
{
    /// <summary>
    /// Gets the subheader text to display before the collection elements.
    /// </summary>
    public string SubHeaderText { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownSubHeaderAttribute"/> class.
    /// </summary>
    /// <param name="subHeaderText">The subheader text to display before the collection elements.</param>
    public MarkdownSubHeaderAttribute(string subHeaderText)
    {
        SubHeaderText = subHeaderText ?? throw new ArgumentNullException(nameof(subHeaderText));
    }
}
