using System;
using System.Text;

#nullable enable

namespace GraphifyCode.Markdown.SourceGen;

/// <summary>
/// Fluent API builder for generating C# code with automatic indentation management.
/// </summary>
internal class CodeBuilder
{
    private readonly StringBuilder _sb = new();
    private int _indentLevel = 0;
    private const string IndentString = "    ";

    /// <summary>
    /// Appends a line of code with proper indentation.
    /// If the input contains multiple lines, each line will be appended with proper indentation.
    /// </summary>
    /// <param name="line">The line(s) to append. If empty or null, appends a blank line.</param>
    public CodeBuilder Line(string line = "")
    {
        if (string.IsNullOrEmpty(line))
        {
            _sb.AppendLine();
        }
        else
        {
            var lines = line.Split(['\r', '\n'], StringSplitOptions.None);
            foreach (var singleLine in lines)
            {
                if (string.IsNullOrEmpty(singleLine))
                {
                    _sb.AppendLine();
                }
                else
                {
                    AppendIndent();
                    _sb.AppendLine(singleLine);
                }
            }
        }
        return this;
    }

    /// <summary>
    /// Appends text without a newline, with proper indentation.
    /// </summary>
    /// <param name="text">The text to append.</param>
    public CodeBuilder Text(string text)
    {
        AppendIndent();
        _sb.Append(text);
        return this;
    }

    /// <summary>
    /// Creates a nested block with curly braces and automatic indentation.
    /// </summary>
    /// <param name="header">Optional header line (e.g., "if (condition)", "class MyClass").</param>
    /// <param name="buildContent">Action to build the content inside the block.</param>
    public CodeBuilder Nest(string? header, Action<CodeBuilder> buildContent)
    {
        if (!string.IsNullOrEmpty(header))
        {
            Line(header!);
        }

        Line("{");
        _indentLevel++;

        buildContent(this);

        _indentLevel--;
        Line("}");

        return this;
    }

    /// <summary>
    /// Returns the generated code as a string.
    /// </summary>
    public string Code() => _sb.ToString();

    private void AppendIndent()
    {
        for (int i = 0; i < _indentLevel; i++)
        {
            _sb.Append(IndentString);
        }
    }
}
