using FluentAssertions;
using NUnit.Framework;
using System;

namespace GraphifyCode.Markdown.Tests;

[TestFixture]
public class SerializeDeserializeTests
{
    [Test]
    public void Serialize_CustomObj_MarkdownShouldBeExpected()
    {
        // Arrange
        var data = new CustomObj
        {
            Id = 1,
            Name = "SomeName",
            IsSomeFlag = true
        };


        // Act
        var markdown = MarkdownSerializer.Serialize(data);


        // Assert
        markdown.Should().Be("""
            # CustomObj
            - Id: 1
            - Name: SomeName
            - IsSomeFlag: True
            """);
    }

    [Test]
    public void Serialize_ArrayOfCustomObjects_MarkdownShouldBeExpected()
    {
        // Arrange
        var data = new ArrayOfCustomObjects()
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


        // Act
        var markdown = MarkdownSerializer.Serialize(data);


        // Assert
        markdown.Should().Be("""
            # ArrayOfCustomObjects

            ## CustomObj
            - Id: 1
            - Name: SomeName1
            - IsSomeFlag: True

            ## CustomObj
            - Id: 2
            - Name: SomeName2
            - IsSomeFlag: False
            """);
    }

    [Test]
    public void Serialize_ArraysOfPrimitives_MarkdownShouldBeExpected()
    {
        // Arrange
        var data = new ArraysOfPrimitives
        {
            Names = ["A", "B"],
            Indexes = [1, 2],
            Ids = [Guid.Parse("89b71ddd-553a-4861-9383-f9ce24494c3e"), Guid.Parse("c97aa83a-8947-49d9-b1a3-d61bc47e361e")]
        };


        // Act
        var markdown = MarkdownSerializer.Serialize(data);


        // Assert
        markdown.Should().Be("""
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
            """);
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
