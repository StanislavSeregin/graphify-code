using GraphifyCode.Data.Entities;
using GraphifyCode.Markdown;
using System.Collections.Generic;
using System.Linq;

namespace GraphifyCode.Data.Models;

[MarkdownSerializable]
public partial class UseCasesDetails
{
    public required DetailedUseCase[] UseCases { get; set; }

    public static UseCasesDetails FromEntities(IEnumerable<UseCase> useCases)
    {
        return new UseCasesDetails()
        {
            UseCases = [.. useCases.Select(u => new DetailedUseCase()
            {
                Name = u.Name,
                Steps = [.. u.Steps.Select(s => new DetailedUseCaseStep() {
                    Name = s.Name,
                    Description = s.Description,
                    ServiceName = s.ServiceName,
                    EndpointName = s.EndpointName,
                    RelativeCodePath = s.RelativeCodePath
                })]
            })]
        };
    }
}

[MarkdownSerializable]
public partial class DetailedUseCase
{
    [MarkdownHeader]
    public required string Name { get; set; }

    public required DetailedUseCaseStep[] Steps { get; set; }
}

[MarkdownSerializable]
public partial class DetailedUseCaseStep
{
    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public string? ServiceName { get; set; }

    public string? EndpointName { get; set; }

    public string? RelativeCodePath { get; set; }
}
