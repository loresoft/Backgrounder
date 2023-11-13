using MessagePack.Resolvers;
using MessagePack;

namespace Backgrounder;

/// <summary>
/// A MessagePack implementation of IMessageSerializer
/// </summary>
/// <seealso cref="IMessageSerializer" />
public class MessagePackMessageSerializer : IMessageSerializer
{
    private readonly MessagePackSerializerOptions _messagePackSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackMessageSerializer"/> class.
    /// </summary>
    /// <param name="messagePackSerializerOptions">The message pack serializer options.</param>
    public MessagePackMessageSerializer(MessagePackSerializerOptions? messagePackSerializerOptions = null)
    {
        _messagePackSerializerOptions = messagePackSerializerOptions
            ?? ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray);
    }

    /// <summary>
    /// Deserializes the specified byte array into an instance of <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type to deserialize</typeparam>
    /// <param name="byteArray">The byte array to deserialize.</param>
    /// <returns>
    /// An instance of <typeparamref name="T" /> deserialized
    /// </returns>
    public T Deserialize<T>(byte[] byteArray)
    {
        if (byteArray is null)
            throw new ArgumentNullException(nameof(byteArray));

        return MessagePackSerializer.Deserialize<T>(byteArray, _messagePackSerializerOptions);
    }

    /// <summary>
    /// Deserializes the specified byte array into an instance of <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type to deserialize</typeparam>
    /// <param name="byteArray">The byte array to deserialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// An instance of <typeparamref name="T" /> deserialized
    /// </returns>
    public Task<T> DeserializeAsync<T>(byte[] byteArray, CancellationToken cancellationToken = default)
    {
        if (byteArray is null)
            throw new ArgumentNullException(nameof(byteArray));

        var value = MessagePackSerializer.Deserialize<T>(byteArray, _messagePackSerializerOptions, cancellationToken);
        return Task.FromResult(value);
    }

    /// <summary>
    /// Serializes the specified instance to a byte array for caching.
    /// </summary>
    /// <typeparam name="T">The type to serialize</typeparam>
    /// <param name="instance">The instance to serialize.</param>
    /// <returns>
    /// The byte array of the serialized instance
    /// </returns>
    public byte[] Serialize<T>(T instance)
    {
        if (instance is null)
            return Array.Empty<byte>();

        return MessagePackSerializer.Serialize(instance, _messagePackSerializerOptions);
    }

    /// <summary>
    /// Serializes the specified instance to a byte array for caching.
    /// </summary>
    /// <typeparam name="T">The type to serialize</typeparam>
    /// <param name="instance">The instance to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The byte array of the serialized instance
    /// </returns>
    public Task<byte[]> SerializeAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        if (instance is null)
            return Task.FromResult(Array.Empty<byte>());

        var value = MessagePackSerializer.Serialize(instance, _messagePackSerializerOptions, cancellationToken);
        return Task.FromResult(value);
    }
}
