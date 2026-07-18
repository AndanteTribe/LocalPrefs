using AndanteTribe.IO.Internal;
using MessagePack;
using MessagePack.Formatters;

namespace AndanteTribe.IO.MessagePack;

/// <summary>
/// MessagePack-based implementation of <see cref="ILocalPrefs"/> that provides efficient key-value storage
/// with persistent data on the filesystem. This implementation uses <see cref="MessagePack"/> for serialization,
/// which offers high-performance binary serialization with smaller output size compared to JSON.
/// The class maintains an in-memory index of stored data for optimized reading and writing operations,
/// and supports LZ4 compression by default.
/// </summary>
public class MessagePackLocalPrefs : ILocalPrefs
{
    private readonly MessagePackSerializerOptions? _options;
    private readonly FileAccessor _fileAccessor;
    private readonly Dictionary<string, (int offset, int count)> _header;
    private readonly ByteBufferWriter _writer;

    private int _headerSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackLocalPrefs"/> class with custom serializer options.
    /// This constructor loads existing data from the specified file path if available,
    /// or initializes a new storage structure if the file doesn't exist.
    /// The implementation maintains a header dictionary that maps keys to their position
    /// in the data buffer for efficient retrieval and updates.
    /// LZ4 block compression is applied by default to reduce storage size.
    /// </summary>
    /// <param name="savePath">The file path where preference data will be stored. The file will be created if it doesn't exist.</param>
    /// <param name="resolver">Optional custom formatter resolver for <see cref="MessagePack"/> serialization. If null, default resolver is used.</param>
    public MessagePackLocalPrefs(in string savePath, IFormatterResolver? resolver)
        : this(FileAccessor.Create(savePath), resolver == null ? null : MessagePackSerializer.DefaultOptions.WithResolver(resolver))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackLocalPrefs"/> class with a custom formatter resolver.
    /// This constructor provides a convenient way to specify a custom formatter resolver for <see cref="MessagePack"/> serialization.
    /// The resolver will be applied to the default MessagePack serializer options.
    /// </summary>
    /// <param name="fileAccessor">Optional file system accessor for reading/writing operations. If null, the default implementation is used.</param>
    /// <param name="resolver">Optional custom formatter resolver for <see cref="MessagePack"/> serialization. If null, default resolver is used.</param>
    public MessagePackLocalPrefs(FileAccessor fileAccessor, IFormatterResolver? resolver)
        : this(fileAccessor, resolver == null ? null : MessagePackSerializer.DefaultOptions.WithResolver(resolver))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackLocalPrefs"/> class with custom serializer options.
    /// This constructor loads existing data from the specified file path if available,
    /// or initializes a new storage structure if the file doesn't exist.
    /// The implementation maintains a header dictionary that maps keys to their position
    /// in the data buffer for efficient retrieval and updates.
    /// LZ4 block compression is applied by default to reduce storage size.
    /// </summary>
    /// <param name="savePath">The file path where preference data will be stored. The file will be created if it doesn't exist.</param>
    /// <param name="options">Optional MessagePack serializer options to customize serialization behavior. If null, default options are used with LZ4 compression.</param>
    public MessagePackLocalPrefs(in string savePath, MessagePackSerializerOptions? options = null)
        : this(FileAccessor.Create(savePath), options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackLocalPrefs"/> class with custom serializer options.
    /// This constructor loads existing data from the specified file path if available,
    /// or initializes a new storage structure if the file doesn't exist.
    /// The implementation maintains a header dictionary that maps keys to their position
    /// in the data buffer for efficient retrieval and updates.
    /// LZ4 block compression is applied by default to reduce storage size.
    /// </summary>
    /// <param name="fileAccessor">Optional file system accessor for reading/writing operations. If null, the default implementation is used.</param>
    /// <param name="options">Optional MessagePack serializer options to customize serialization behavior. If null, default options are used with LZ4 compression.</param>
    public MessagePackLocalPrefs(FileAccessor fileAccessor, MessagePackSerializerOptions? options = null)
    {
        _options = (options ?? MessagePackSerializer.DefaultOptions).WithCompression(MessagePackCompression.Lz4Block);
        _fileAccessor = fileAccessor;

        var dataArray = _fileAccessor.ReadAllBytes();
        _writer = new ByteBufferWriter(dataArray);
        if (dataArray.Length > 0)
        {
            var reader = new MessagePackReader(dataArray);
            var formatter = new DictionaryFormatter<string, (int, int)>();
            _header = formatter.Deserialize(ref reader, HeaderFormatterResolver.StandardOptions) ?? new();
            _headerSize = (int)reader.Consumed;
            _writer.CurrentOffset = dataArray.Length;
        }
        else
        {
            _header = new();
        }
    }

    /// <inheritdoc />
    public T? Load<T>(string key)
    {
        if (_header.TryGetValue(key, out var v))
        {
            var (offset, count) = v;
            var reader = new MessagePackReader(_writer.WrittenMemory.Slice(_headerSize + offset, count));
            return MessagePackSerializer.Deserialize<T>(ref reader, _options);
        }

        return default;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_header.TryGetValue(key, out var prev))
        {
            using (var scope = _writer.GetWriteBlockScope(_headerSize + prev.offset, prev.count))
            {
                MessagePackSerializer.Serialize(_writer, value, _options, cancellationToken);
                _header[key] = prev with { count = scope.Consumed };
            }

            using var updateKeys = new PooledList<KeyValuePair<string, (int, int)>>(_header.Count);
            foreach (var v in _header)
            {
                if (v.Value.offset > prev.offset)
                {
                    updateKeys.Add(v);
                }
            }

            var offsetDelta = _header[key].count - prev.count;
            foreach (var (k, (o, c)) in updateKeys.AsSpan())
            {
                _header[k] = (o + offsetDelta, c);
            }
        }
        else
        {
            var currentOffset = _writer.CurrentOffset;
            var offset = currentOffset - _headerSize;
            MessagePackSerializer.Serialize(_writer, value, _options, cancellationToken);
            _header.Add(key, (offset, _writer.CurrentOffset - currentOffset));
        }

        using (var scope = _writer.GetWriteBlockScope(0, _headerSize))
        {
            MessagePackSerializer.Serialize(_writer, _header, HeaderFormatterResolver.StandardOptions, cancellationToken);
            _headerSize = scope.Consumed;
        }

        await _fileAccessor.WriteAsync(_writer.WrittenMemory, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var prev = _header[key];
        _header.Remove(key);

        // Delete the key's data from the buffer
        using (_writer.GetWriteBlockScope(_headerSize + prev.offset, prev.count))
        {
        }

        using (var updateKeys = new PooledList<KeyValuePair<string, (int, int)>>(_header.Count))
        {
            foreach (var v in _header)
            {
                if (v.Value.offset > prev.offset)
                {
                    updateKeys.Add(v);
                }
            }

            foreach (var (k, (o, c)) in updateKeys.AsSpan())
            {
                _header[k] = (o - prev.count, c);
            }
        }

        using (var scope = _writer.GetWriteBlockScope(0, _headerSize))
        {
            MessagePackSerializer.Serialize(_writer, _header, HeaderFormatterResolver.StandardOptions, cancellationToken);
            _headerSize = scope.Consumed;
        }

        await _fileAccessor.WriteAsync(_writer.WrittenMemory, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _header.Clear();
        _writer.Clear();

        return _fileAccessor.DeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public bool HasKey(string key) => _header.ContainsKey(key);
}