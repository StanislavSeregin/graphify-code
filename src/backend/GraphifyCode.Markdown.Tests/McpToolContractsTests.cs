using FluentAssertions;
using GraphifyCode.Data;
using GraphifyCode.MCP;
using GraphifyCode.MCP.Contracts;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Markdown.Tests;

[TestFixture]
public class McpToolContractsTests
{
    private ServiceProvider _provider = null!;
    private GraphifyCodeTool _tool = null!;
    private string _dataPath = null!;

    [SetUp]
    public void SetUp()
    {
        _dataPath = Path.Combine(Path.GetTempPath(), "graphify-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dataPath);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MarkdownStorage:Path"] = _dataPath
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMediator();
        services.AddGraphifyContext(config);

        _provider = services.BuildServiceProvider();
        _tool = new GraphifyCodeTool(_provider.GetRequiredService<IMediator>());
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
        if (Directory.Exists(_dataPath))
        {
            Directory.Delete(_dataPath, recursive: true);
        }
    }

    [Test]
    public async Task UpsertService_ShouldCreateEntity_AndReturnStructuredResponse()
    {
        var response = await _tool.UpsertService(
            new UpsertServiceRequest
            {
                Name = "UserService",
                Description = "Handles user lifecycle",
                RelativeCodePath = "src/services/user"
            },
            CancellationToken.None);

        response.Ok.Should().BeTrue();
        response.Error.Should().BeNull();
        response.Data.Should().NotBeNull();
        response.Data!.EntityType.Should().Be(GraphEntityType.Service);
        response.Data.EntityName.Should().Be("UserService");
        response.Data.Action.Should().Be("created");
        Directory.Exists(Path.Combine(_dataPath, "UserService")).Should().BeTrue();
    }

    [Test]
    public async Task SearchGraph_ShouldReturnValidationError_WhenQueryTooShort()
    {
        var response = await _tool.SearchGraph("a", 10, CancellationToken.None);

        response.Ok.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be("validation_error");
        response.Error.Details.Should().BeOfType<ValidationErrorDetails>();
    }

    [Test]
    public async Task EndToEnd_ShouldSupportCreateAndReadFlow()
    {
        var serviceResult = await _tool.UpsertService(
            new UpsertServiceRequest
            {
                Name = "BillingService",
                Description = "Payment processing service",
                RelativeCodePath = "src/services/billing"
            },
            CancellationToken.None);

        var serviceName = serviceResult.Data!.EntityName;

        var endpointResult = await _tool.UpsertEndpoint(
            new UpsertEndpointRequest
            {
                ServiceName = serviceName,
                Name = "POST /payments",
                Description = "Create payment",
                Type = "http",
                RelativeCodePath = "src/services/billing/payments.ts"
            },
            CancellationToken.None);

        var useCaseResult = await _tool.UpsertUseCase(
            new UpsertUseCaseRequest
            {
                ServiceName = serviceName,
                Name = "Process Payment",
                Description = "Process payment end-to-end",
                InitiatingEndpointName = endpointResult.Data!.EntityName
            },
            CancellationToken.None);

        var relationResult = await _tool.UpsertRelation(
            new UpsertRelationRequest
            {
                ServiceName = serviceName,
                UseCaseName = useCaseResult.Data!.EntityName,
                StepName = "Call gateway",
                StepDescription = "Forward request to payment gateway",
                RelatedServiceName = serviceName,
                EndpointName = endpointResult.Data.EntityName,
                RelativeCodePath = "src/services/billing/gateway.ts"
            },
            CancellationToken.None);

        var getServiceResult = await _tool.GetService(serviceName, includeEndpoints: true, includeUseCases: true, CancellationToken.None);
        var getUseCaseResult = await _tool.GetUseCase(serviceName, useCaseResult.Data.EntityName, CancellationToken.None);
        var listResult = await _tool.ListServices(CancellationToken.None);
        var searchResult = await _tool.SearchGraph("payment", 20, CancellationToken.None);

        relationResult.Ok.Should().BeTrue();
        getServiceResult.Ok.Should().BeTrue();
        getServiceResult.Data!.Service.Services.Should().HaveCount(1);
        getUseCaseResult.Ok.Should().BeTrue();
        listResult.Ok.Should().BeTrue();
        searchResult.Ok.Should().BeTrue();
        searchResult.Data!.Matches.Should().NotBeEmpty();
        File.Exists(Path.Combine(_dataPath, "BillingService", "usecases", "Process Payment.md")).Should().BeTrue();
    }

    [Test]
    public async Task RemoveEntity_ShouldReturnConflict_WhenServiceReferencedInUseCaseSteps()
    {
        var serviceA = await _tool.UpsertService(
            new UpsertServiceRequest { Name = "A", Description = "A service" },
            CancellationToken.None);
        var serviceB = await _tool.UpsertService(
            new UpsertServiceRequest { Name = "B", Description = "B service" },
            CancellationToken.None);

        var endpointB = await _tool.UpsertEndpoint(
            new UpsertEndpointRequest
            {
                ServiceName = serviceB.Data!.EntityName,
                Name = "GET /b",
                Description = "endpoint b",
                Type = "http"
            },
            CancellationToken.None);

        var useCaseB = await _tool.UpsertUseCase(
            new UpsertUseCaseRequest
            {
                ServiceName = serviceB.Data!.EntityName,
                Name = "Cross call",
                Description = "Cross-service use case",
                InitiatingEndpointName = endpointB.Data!.EntityName
            },
            CancellationToken.None);

        await _tool.UpsertRelation(
            new UpsertRelationRequest
            {
                ServiceName = serviceB.Data.EntityName,
                UseCaseName = useCaseB.Data!.EntityName,
                StepName = "Use service A",
                StepDescription = "Depends on service A",
                RelatedServiceName = serviceA.Data!.EntityName
            },
            CancellationToken.None);

        var removeResult = await _tool.RemoveEntity(serviceA.Data.EntityName, GraphEntityType.Service, serviceName: null, CancellationToken.None);

        removeResult.Ok.Should().BeFalse();
        removeResult.Error!.Code.Should().Be("conflict");
        removeResult.Error.Details.Should().BeOfType<ConflictErrorDetails>();
    }

    [Test]
    public async Task ListEndpoints_And_ListUseCases_ShouldReturnServiceScopedCollections()
    {
        var service = await _tool.UpsertService(
            new UpsertServiceRequest { Name = "Orders", Description = "Orders service" },
            CancellationToken.None);

        var endpoint = await _tool.UpsertEndpoint(
            new UpsertEndpointRequest
            {
                ServiceName = service.Data!.EntityName,
                Name = "GET /orders",
                Description = "Get orders",
                Type = "http"
            },
            CancellationToken.None);

        await _tool.UpsertUseCase(
            new UpsertUseCaseRequest
            {
                ServiceName = service.Data.EntityName,
                Name = "List orders",
                Description = "List flow",
                InitiatingEndpointName = endpoint.Data!.EntityName
            },
            CancellationToken.None);

        var endpointsResult = await _tool.ListEndpoints(service.Data.EntityName, CancellationToken.None);
        var useCasesResult = await _tool.ListUseCases(service.Data.EntityName, CancellationToken.None);

        endpointsResult.Ok.Should().BeTrue();
        endpointsResult.Data!.Endpoints.Should().ContainSingle();
        useCasesResult.Ok.Should().BeTrue();
        useCasesResult.Data!.UseCases.Should().ContainSingle();
    }

    [Test]
    public async Task BulkUpsertEndpoint_ShouldSupportPartialSuccess()
    {
        var service = await _tool.UpsertService(
            new UpsertServiceRequest { Name = "Billing", Description = "Billing service" },
            CancellationToken.None);

        var result = await _tool.BulkUpsertEndpoint(
            new BulkUpsertEndpointsRequest
            {
                Items =
                [
                    new UpsertEndpointRequest
                    {
                        ServiceName = service.Data!.EntityName,
                        Name = "GET /billing",
                        Description = "Billing endpoint",
                        Type = "http"
                    },
                    new UpsertEndpointRequest
                    {
                        ServiceName = "missing-service",
                        Name = "GET /invalid",
                        Description = "Invalid endpoint",
                        Type = "http"
                    }
                ]
            },
            CancellationToken.None);

        result.Ok.Should().BeTrue();
        result.Data!.Succeeded.Should().HaveCount(1);
        result.Data.Failed.Should().HaveCount(1);
        result.Warnings.Should().NotBeEmpty();
    }

    [Test]
    public async Task BulkUpsertRelation_ShouldReturnBatchFailed_WhenAllItemsFail()
    {
        var result = await _tool.BulkUpsertRelation(
            new BulkUpsertRelationsRequest
            {
                Items =
                [
                    new UpsertRelationRequest
                    {
                        ServiceName = "missing",
                        UseCaseName = "missing",
                        StepName = "Unknown",
                        StepDescription = "Should fail"
                    }
                ]
            },
            CancellationToken.None);

        result.Ok.Should().BeFalse();
        result.Error!.Code.Should().Be("batch_failed");
        result.Error.Details.Should().BeOfType<BatchErrorDetails>();
    }
}
