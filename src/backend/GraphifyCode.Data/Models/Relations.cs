using GraphifyCode.Markdown;
using System;

namespace GraphifyCode.Data.Models;

[MarkdownSerializable]
public partial class Relations
{
    public required Guid[] TargetEndpointIds { get; set; }
}
