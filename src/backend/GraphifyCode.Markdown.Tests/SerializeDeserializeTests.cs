using FluentAssertions;
using NUnit.Framework;
using System;

namespace GraphifyCode.Markdown.Tests;

[TestFixture]
public class SerializeDeserializeTests
{
    private static readonly CustomObj CustomObjData = new()
    {
        Id = 1,
        Name = "SomeName",
        IsSomeFlag = true
    };

    private const string CustomObjMarkdown = """
        # CustomObj
        - Id: 1
        - Name: SomeName
        - IsSomeFlag: True
        """;

    private static readonly ArrayOfCustomObjects ArrayOfCustomObjectsData = new()
    {
        Items =
        [
            new CustomObj
            {
                Id = 1,
                Name = "SomeName1",
                IsSomeFlag = true
            },
            new CustomObj
            {
                Id = 2,
                Name = "SomeName2",
                IsSomeFlag = false
            }
        ]
    };

    private const string ArrayOfCustomObjectsMarkdown = """
        # ArrayOfCustomObjects

        ## CustomObj
        - Id: 1
        - Name: SomeName1
        - IsSomeFlag: True

        ## CustomObj
        - Id: 2
        - Name: SomeName2
        - IsSomeFlag: False
        """;

    private static readonly ArraysOfPrimitives ArraysOfPrimitivesData = new()
    {
        Names = ["A", "B"],
        Indexes = [1, 2],
        Ids = [Guid.Parse("89b71ddd-553a-4861-9383-f9ce24494c3e"), Guid.Parse("c97aa83a-8947-49d9-b1a3-d61bc47e361e")]
    };

    private const string ArraysOfPrimitivesMarkdown = """
        # ArraysOfPrimitives

        ## Names
        - A
        - B

        ## Indexes
        - 1
        - 2

        ## Ids
        - 89b71ddd-553a-4861-9383-f9ce24494c3e
        - c97aa83a-8947-49d9-b1a3-d61bc47e361e
        """;

    private static readonly ObjWithNested ObjWithNestedData = new()
    {
        Id = 1,
        Name = "Parent",
        IsSomeFlag = true,
        Nested = new CustomObj
        {
            Id = 2,
            Name = "Child",
            IsSomeFlag = false
        }
    };

    private const string ObjWithNestedMarkdown = """
        # ObjWithNested
        - Id: 1
        - Name: Parent
        - IsSomeFlag: True

        ## Nested
        - Id: 2
        - Name: Child
        - IsSomeFlag: False
        """;

    [Test]
    public void Serialize_CustomObj_MarkdownShouldBeExpected()
    {
        // Act
        var markdown = MarkdownSerializer.Serialize(CustomObjData);

        // Assert
        markdown.Should().Be(CustomObjMarkdown);
    }

    [Test]
    public void Serialize_ArrayOfCustomObjects_MarkdownShouldBeExpected()
    {
        // Act
        var markdown = MarkdownSerializer.Serialize(ArrayOfCustomObjectsData);

        // Assert
        markdown.Should().Be(ArrayOfCustomObjectsMarkdown);
    }

    [Test]
    public void Serialize_ArraysOfPrimitives_MarkdownShouldBeExpected()
    {
        // Act
        var markdown = MarkdownSerializer.Serialize(ArraysOfPrimitivesData);

        // Assert
        markdown.Should().Be(ArraysOfPrimitivesMarkdown);
    }

    [Test]
    public void Deserialize_CustomObj_ObjectShouldBeExpected()
    {
        // Act
        var obj = MarkdownSerializer.Deserialize<CustomObj>(CustomObjMarkdown);

        // Assert
        obj.Should().BeEquivalentTo(CustomObjData);
    }

    [Test]
    public void Deserialize_ArrayOfCustomObjects_ObjectShouldBeExpected()
    {
        // Act
        var obj = MarkdownSerializer.Deserialize<ArrayOfCustomObjects>(ArrayOfCustomObjectsMarkdown);

        // Assert
        obj.Should().BeEquivalentTo(ArrayOfCustomObjectsData);
    }

    [Test]
    public void Deserialize_ArraysOfPrimitives_ObjectShouldBeExpected()
    {
        // Act
        var obj = MarkdownSerializer.Deserialize<ArraysOfPrimitives>(ArraysOfPrimitivesMarkdown);

        // Assert
        obj.Should().BeEquivalentTo(ArraysOfPrimitivesData);
    }

    [Test]
    public void Serialize_ObjWithNested_MarkdownShouldBeExpected()
    {
        // Act
        var markdown = MarkdownSerializer.Serialize(ObjWithNestedData);

        // Assert
        markdown.Should().Be(ObjWithNestedMarkdown);
    }

    [Test]
    public void Deserialize_ObjWithNested_ObjectShouldBeExpected()
    {
        // Act
        var obj = MarkdownSerializer.Deserialize<ObjWithNested>(ObjWithNestedMarkdown);

        // Assert
        obj.Should().BeEquivalentTo(ObjWithNestedData);
    }
}

[MarkdownSerializable]
public partial class CustomObj
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public bool IsSomeFlag { get; set; }
}

[MarkdownSerializable]
public partial class ArrayOfCustomObjects
{
    public required CustomObj[] Items { get; set; }
}

[MarkdownSerializable]
public partial class ArraysOfPrimitives
{
    public required string[] Names { get; set; }

    public required int[] Indexes { get; set; }

    public required Guid[] Ids { get; set; }
}

[MarkdownSerializable]
public partial class ObjWithNested
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public bool IsSomeFlag { get; set; }

    public required CustomObj Nested { get; set; }
}