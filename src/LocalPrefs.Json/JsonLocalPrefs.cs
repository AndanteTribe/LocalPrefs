using System.Text.Json;
using System.Text.Json.Serialization;
using AndanteTribe.IO.Internal;

namespace AndanteTribe.IO.Json;

/// <summary>
/// JSON-based implementation of <see cref="ILocalPrefs"/> that provides efficient key-value storage
/// with persistent data on the filesystem. This implementation uses <see cref="System.Text.Json"/> for serialization
/// and maintains an in-memory index of stored data for optimized reading and writing operations.
/// </summary>
public class JsonLocalPrefs : ILocalPrefs
{
    private static readonly JsonSerializerOptions s_headerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new HeaderJsonConverter() },
    };

    private readonly JsonSerializerOptions? _options;
    private readonly FileAccessor _fileAccessor;
    private readonly Dictionary<string, (int offset, int count)> _header;
    private readonly ByteBufferWriter _writer;
    private readonly Utf8JsonWriter _jsonWriter;

    private int _headerSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonLocalPrefs"/> class.
    /// This constructor loads existing data from the specified file path if available,
    /// or initializes a new storage structure if the file doesn't exist.
    /// The implementation maintains a header dictionary that maps keys to their position
    /// in the data buffer for efficient retrieval and updates.
    /// </summary>
    /// <param name="savePath">The file path where preference data will be stored. The file will be created if it doesn't exist.</param>
    /// <param name="options">Optional JSON serializer options to customize serialization behavior. If null, default options are used.</param>
    public JsonLocalPrefs(in string savePath, JsonSerializerOptions? options = null) : this(FileAccessor.Create(savePath), options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonLocalPrefs"/> class.
    /// This constructor loads existing data from the specified file path if available,
    /// or initializes a new storage structure if the file doesn't exist.
    /// The implementation maintains a header dictionary that maps keys to their position
    /// in the data buffer for efficient retrieval and updates.
    /// </summary>
    /// <param name="fileAccessor">Optional file system accessor for reading/writing operations. If null, the default implementation is used.</param>
    /// <param name="options">Optional JSON serializer options to customize serialization behavior. If null, default options are used.</param>
    public JsonLocalPrefs(FileAccessor fileAccessor, JsonSerializerOptions? options = null)
    {
        _fileAccessor = fileAccessor;
        _options = options ?? JsonSerializerOptions.Default;

        var dataArray = _fileAccessor.ReadAllBytes();
        _writer = new ByteBufferWriter(dataArray);
        if (dataArray.Length > 0)
        {
            var reader = new Utf8JsonReader(dataArray);
            _header = JsonSerializer.Deserialize<Dictionary<string, (int, int)>>(ref reader, s_headerOptions) ?? new();
            _headerSize = (int)reader.BytesConsumed;
            _writer.CurrentOffset = dataArray.Length;
        }
        else
        {
            _header = new();
        }

        _jsonWriter = new Utf8JsonWriter(_writer);
    }

    /// <inheritdoc />
    public T? Load<T>(string key)
    {
        if (_header.TryGetValue(key, out var v))
        {
            var (offset, count) = v;
            var reader = new Utf8JsonReader(_writer.WrittenSpan.Slice(_headerSize + offset, count));
            return JsonSerializer.Deserialize<T>(ref reader, _options);
        }

        return default;
    }

    /// <inheritdoc />
    public ValueTask SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_header.TryGetValue(key, out var prev))
        {
            using (var scope = _writer.GetWriteBlockScope(_headerSize + prev.offset, prev.count))
            {
                JsonSerializer.Serialize(_jsonWriter, value, _options);
                _header[key] = prev with { count = scope.Consumed };
                _jsonWriter.Reset();
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
            JsonSerializer.Serialize(_jsonWriter, value, _options);
            _header.Add(key, (offset, _writer.CurrentOffset - currentOffset));
            _jsonWriter.Reset();
        }

        using (var scope = _writer.GetWriteBlockScope(0, _headerSize))
        {
            JsonSerializer.Serialize(_jsonWriter, _header, s_headerOptions);
            _headerSize = scope.Consumed;
            _jsonWriter.Reset();
        }

        return _fileAccessor.WriteAsync(_writer.WrittenMemory, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default)
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
            JsonSerializer.Serialize(_jsonWriter, _header, s_headerOptions);
            _headerSize = scope.Consumed;
            _jsonWriter.Reset();
        }

        return _fileAccessor.WriteAsync(_writer.WrittenMemory, cancellationToken);
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