using GraphifyCode.Core.Services;
using GraphifyCode.Core.Settings;
using Microsoft.Extensions.Options;

namespace GraphifyCode.Data;

public class DataService(IOptions<GraphifyCodeSettings> options) : IDataService
{
    private readonly GraphifyCodeSettings _settings = options.Value;

    // TODO
}
