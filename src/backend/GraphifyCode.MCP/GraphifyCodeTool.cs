using GraphifyCode.Data.Context;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP;

[McpServerToolType]
public class GraphifyCodeTool(GraphifyContext context)
{
    [McpServerTool, Description("""
    ???
    """)]
    public async Task<string> GetServices(CancellationToken cancellationToken = default)
    {
        return "";
    }
}
