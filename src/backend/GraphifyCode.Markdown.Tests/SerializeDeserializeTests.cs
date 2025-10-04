using FluentAssertions;
using GraphifyCode.Data.Entities;
using NUnit.Framework;
using System;

namespace GraphifyCode.Markdown.Tests;

[TestFixture]
public class SerializeDeserializeTests
{
    private static readonly Service ServiceData = new()
    {
        Id = Guid.Parse("89b71ddd-553a-4861-9383-f9ce24494c3e"),
        Name = "UserService",
        Description = "Handles user authentication and management",
        LastAnalyzedAt = new DateTime(2024, 10, 15, 14, 30, 0),
        RelativeCodePath = "src/services/UserService.cs"
    };

    private const string ServiceMarkdown = """
        # UserService
        - Id: 89b71ddd-553a-4861-9383-f9ce24494c3e
        - Description: Handles user authentication and management
        - LastAnalyzedAt: 15.10.2024 14:30:00
        - RelativeCodePath: src/services/UserService.cs
        """;

    [Test]
    public void Serialize_Service_MarkdownShouldBeExpected()
    {
        // Act
        var markdown = ServiceData.ToMarkdown();

        // Assert
        markdown.Should().Be(ServiceMarkdown);
    }

    [Test]
    public void Deserialize_Service_ObjectShouldBeExpected()
    {
        // Act
        var obj = Service.FromMarkdown(ServiceMarkdown);

        // Assert
        obj.Should().BeEquivalentTo(ServiceData);
    }

    private static readonly Endpoints EndpointsData = new()
    {
        EndpointList =
        [
            new Endpoint
            {
                Id = Guid.Parse("c97aa83a-8947-49d9-b1a3-d61bc47e361e"),
                Name = "GetUser",
                Description = "Retrieves user by ID",
                Type = "http",
                LastAnalyzedAt = new DateTime(2024, 10, 15, 15, 0, 0),
                RelativeCodePath = "src/controllers/UserController.cs"
            },
            new Endpoint
            {
                Id = Guid.Parse("89b71ddd-553a-4861-9383-f9ce24494c3e"),
                Name = "CreateUser",
                Description = "Creates a new user",
                Type = "http",
                LastAnalyzedAt = new DateTime(2024, 10, 15, 16, 0, 0),
                RelativeCodePath = "src/controllers/UserController.cs"
            }
        ]
    };

    private const string EndpointsMarkdown = """
        # Endpoints

        ## GetUser
        - Id: c97aa83a-8947-49d9-b1a3-d61bc47e361e
        - Description: Retrieves user by ID
        - Type: http
        - LastAnalyzedAt: 15.10.2024 15:00:00
        - RelativeCodePath: src/controllers/UserController.cs

        ## CreateUser
        - Id: 89b71ddd-553a-4861-9383-f9ce24494c3e
        - Description: Creates a new user
        - Type: http
        - LastAnalyzedAt: 15.10.2024 16:00:00
        - RelativeCodePath: src/controllers/UserController.cs
        """;

    [Test]
    public void Serialize_Endpoints_MarkdownShouldBeExpected()
    {
        // Act
        var markdown = EndpointsData.ToMarkdown();

        // Assert
        markdown.Should().Be(EndpointsMarkdown);
    }

    [Test]
    public void Deserialize_Endpoints_ObjectShouldBeExpected()
    {
        // Act
        var obj = Endpoints.FromMarkdown(EndpointsMarkdown);

        // Assert
        obj.Should().BeEquivalentTo(EndpointsData);
    }

    private static readonly Relations RelationsData = new()
    {
        TargetEndpointIds =
        [
            Guid.Parse("89b71ddd-553a-4861-9383-f9ce24494c3e"),
            Guid.Parse("c97aa83a-8947-49d9-b1a3-d61bc47e361e")
        ]
    };

    private const string RelationsMarkdown = """
        # Relations

        ## TargetEndpointIds
        - 89b71ddd-553a-4861-9383-f9ce24494c3e
        - c97aa83a-8947-49d9-b1a3-d61bc47e361e
        """;

    [Test]
    public void Serialize_Relations_MarkdownShouldBeExpected()
    {
        // Act
        var markdown = RelationsData.ToMarkdown();

        // Assert
        markdown.Should().Be(RelationsMarkdown);
    }

    [Test]
    public void Deserialize_Relations_ObjectShouldBeExpected()
    {
        // Act
        var obj = Relations.FromMarkdown(RelationsMarkdown);

        // Assert
        obj.Should().BeEquivalentTo(RelationsData);
    }
}