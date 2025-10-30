using GraphifyCode.Data.Entities;
using GraphifyCode.Markdown;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphifyCode.Data.Models;

[MarkdownSerializable("Services overview")]
public partial class ServicesOverview
{
    public required ServiceItem[] ServiceList { get; set; }

    [MarkdownSerializable]
    public partial class ServiceItem
    {
        public Guid Id { get; set; }

        [MarkdownHeader]
        public required string Name { get; set; }

        public required string Description { get; set; }

        public DateTime LastAnalyzedAt { get; set; }

        public string? RelativeCodePath { get; set; }
    }

    public static ServicesOverview FromEntities(IEnumerable<Service> services)
    {
        return new ServicesOverview()
        {
            ServiceList = [.. services.Select(srv => new ServiceItem()
            {
                Id = srv.Id,
                Name = srv.Name,
                Description = srv.Description,
                LastAnalyzedAt = srv.LastAnalyzedAt,
                RelativeCodePath = srv.RelativeCodePath
            })]
        };
    }
}


