using GraphifyCode.Data.Entities;

namespace GraphifyCode.Data.Models;

public class FullGraph
{
    public required ServiceData[] Services { get; set; }

    public class ServiceData
    {
        public required Entities.Service Service { get; set; }

        public required Endpoint[] Endpoint { get; set; }

        public required Relations Relations { get; set; }

        public required UseCase[] UseCases { get; set; }
    }
}
