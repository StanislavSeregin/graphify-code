using System;

namespace GraphifyCode.Markdown;

/// <summary>
/// Marks a type for Markdown serialization code generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class MarkdownSerializableAttribute : Attribute { /* NOTHING */ }
