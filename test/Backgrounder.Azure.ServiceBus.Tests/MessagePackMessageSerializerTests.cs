using FluentAssertions;

using MessagePack;
using MessagePack.Resolvers;

namespace Backgrounder.Azure.ServiceBus.Tests;

public class MessagePackMessageSerializerTests
{
    public record Person(string? Name, string? Email);

    [Fact]
    public void SerializeRecord()
    {
        var options = ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray);
        var serializer = new MessagePackMessageSerializer(options);

        var person = new Person("Test User", "test@email.com");

        var buffer = serializer.Serialize(person);
        buffer.Should().NotBeEmpty();
    }

    [Fact]
    public void DeserializeRecord()
    {
        var options = ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray);
        var serializer = new MessagePackMessageSerializer(options);

        var person = new Person("Test User", "test@email.com");

        var buffer = serializer.Serialize(person);
        buffer.Should().NotBeEmpty();

        var newPerson = serializer.Deserialize<Person>(buffer);

        newPerson.Should().Be(person);
    }
}

