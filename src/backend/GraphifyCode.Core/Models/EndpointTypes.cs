namespace GraphifyCode.Core.Models;

public static class EndpointTypes
{
    public const string Http = "http";

    public const string Queue = "queue";

    public const string Job = "job";

    public static bool IsValidEndpointType(string type)
    {
        return type == EndpointTypes.Http || type == EndpointTypes.Queue || type == EndpointTypes.Job;
    }
}
