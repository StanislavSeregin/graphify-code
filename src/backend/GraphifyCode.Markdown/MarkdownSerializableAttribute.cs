using System;

namespace GraphifyCode.Markdown;

/// <summary>
/// Marks a type for Markdown serialization code generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class MarkdownSerializableAttribute : Attribute { /* NOTHING */ }

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
