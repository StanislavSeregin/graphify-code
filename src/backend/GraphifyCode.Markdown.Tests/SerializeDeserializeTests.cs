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
        Name = "UserService",
        Description = "Handles user authentication and management",
        LastAnalyzedAt = new DateTime(2024, 10, 15, 14, 30, 0),
        RelativeCodePath = "src/services/UserService.cs",
        UseCases = []
    };

    private static readonly string ServiceMarkdown = """
        # UserService
        - Description: Handles user authentication and management
        - LastAnalyzedAt: 15.10.2024 14:30:00
        - RelativeCodePath: src/services/UserService.cs
        """.ReplaceLineEndings("\n");

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
        obj.Should().BeEquivalentTo(ServiceData, opts => opts.Excluding(s => s.UseCases).Excluding(s => s.Endpoints));
    }

    private static readonly Service ServiceDataWithMultiline = new()
    {
        Name = "UserService",
        Description = """
        Handles user authentication and management.
        And some else...
        """.ReplaceLineEndings("\n"),
        LastAnalyzedAt = new DateTime(2024, 10, 15, 14, 30, 0),
        RelativeCodePath = "src/services/UserService.cs",
        UseCases = []
    };

    private static readonly string ServiceMarkdownWithMultiline = """
        # UserService
        - Description: Handles user authentication and management.
        And some else...
        - LastAnalyzedAt: 15.10.2024 14:30:00
        - RelativeCodePath: src/services/UserService.cs
        """.ReplaceLineEndings("\n");

    [Test]
    public void Serialize_ServiceWithMultiline_MarkdownShouldBeExpected()
    {
        // Act
        var markdown = ServiceDataWithMultiline.ToMarkdown();

        // Assert
        markdown.Should().Be(ServiceMarkdownWithMultiline);
    }

    [Test]
    public void Deserialize_ServiceMultiline_ObjectShouldBeExpected()
    {
        // Act
        var obj = Service.FromMarkdown(ServiceMarkdownWithMultiline);

        // Assert
        obj.Should().BeEquivalentTo(ServiceDataWithMultiline, opts => opts.Excluding(s => s.UseCases).Excluding(s => s.Endpoints));
    }

    private static readonly Endpoints EndpointsData = new()
    {
        EndpointList =
        [
            new Endpoint
            {
                Name = "GetUser",
                Description = "Retrieves user by ID",
                Type = "http",
                LastAnalyzedAt = new DateTime(2024, 10, 15, 15, 0, 0),
                RelativeCodePath = "src/controllers/UserController.cs"
            },
            new Endpoint
            {
                Name = "CreateUser",
                Description = "Creates a new user",
                Type = "http",
                LastAnalyzedAt = new DateTime(2024, 10, 15, 16, 0, 0),
                RelativeCodePath = "src/controllers/UserController.cs"
            }
        ],
        Parent = null!
    };

    private static readonly string EndpointsMarkdown = """
        # Endpoints

        ## GetUser
        - Description: Retrieves user by ID
        - Type: http
        - LastAnalyzedAt: 15.10.2024 15:00:00
        - RelativeCodePath: src/controllers/UserController.cs

        ## CreateUser
        - Description: Creates a new user
        - Type: http
        - LastAnalyzedAt: 15.10.2024 16:00:00
        - RelativeCodePath: src/controllers/UserController.cs
        """.ReplaceLineEndings("\n");

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
        obj.Should().BeEquivalentTo(EndpointsData, opts => opts.Excluding(e => e.Parent));
    }

    private static readonly UseCase UseCaseData = new()
    {
        Name = "User Registration",
        Description = "Complete user registration flow",
        InitiatingEndpointName = "GetUser",
        LastAnalyzedAt = new DateTime(2024, 10, 15, 14, 30, 0),
        Steps =
        [
            new UseCaseStep
            {
                Name = "Validate Input",
                Description = "Validate user input data",
                ServiceName = "UserService",
                EndpointName = null,
                RelativeCodePath = "src/validators/UserValidator.cs"
            },
            new UseCaseStep
            {
                Name = "Create User",
                Description = "Create user in database",
                ServiceName = null,
                EndpointName = "CreateUser",
                RelativeCodePath = null
            }
        ],
        Parent = null!
    };

    private static readonly string UseCaseMarkdown = """
        # User Registration
        - Description: Complete user registration flow
        - InitiatingEndpointName: GetUser
        - LastAnalyzedAt: 15.10.2024 14:30:00

        ## Validate Input
        - Description: Validate user input data
        - ServiceName: UserService
        - RelativeCodePath: src/validators/UserValidator.cs

        ## Create User
        - Description: Create user in database
        - EndpointName: CreateUser
        """.ReplaceLineEndings("\n");

    [Test]
    public void Serialize_UseCase_MarkdownShouldBeExpected()
    {
        // Act
        var markdown = UseCaseData.ToMarkdown();

        // Assert
        markdown.Should().Be(UseCaseMarkdown);
    }

    [Test]
    public void Deserialize_UseCase_ObjectShouldBeExpected()
    {
        // Act
        var obj = UseCase.FromMarkdown(UseCaseMarkdown);

        // Assert
        obj.Should().BeEquivalentTo(UseCaseData, opts => opts.Excluding(u => u.Parent));
    }
}
