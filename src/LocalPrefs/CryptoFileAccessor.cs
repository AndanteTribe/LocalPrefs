using System.Buffers;
using System.Security.Cryptography;

namespace AndanteTribe.IO;

/// <summary>
/// A file accessor that encrypts and decrypts file content using AES-GCM authenticated encryption.
/// Implements the decorator pattern by wrapping another <see cref="FileAccessor"/> instance.
/// </summary>
/// <remarks>
/// The on-disk format is: <c>[nonce (12 bytes)][authentication tag (16 bytes)][AES-GCM ciphertext]</c>.
/// A fresh random nonce is generated for every write, providing semantic security so that the same
/// plaintext never produces the same ciphertext twice. AES-GCM provides both confidentiality and
/// integrity/authenticity in a single pass; any tampering is detected before plaintext is returned
/// (a <see cref="CryptographicException"/> is thrown when authentication fails).
/// <para>
/// <strong>Unity / Mono:</strong> <see cref="AesGcm"/> is not supported on Unity's Mono runtime
/// (see <see href="https://github.com/mono/mono/issues/19285"/>). Do not use
/// <see cref="CryptoFileAccessor"/> in Unity projects.
/// </para>
/// </remarks>
public class CryptoFileAccessor : FileAccessor
{
    private const int NonceSize = 12; // AES-GCM standard nonce length (96 bits)
    private const int TagSize = 16;   // AES-GCM authentication tag length (128 bits)

    private readonly FileAccessor _fileAccessor;
    private readonly byte[] _key;

    /// <inheritdoc />
    protected internal override string SavePath => _fileAccessor.SavePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoFileAccessor"/> class.
    /// </summary>
    /// <param name="fileAccessor">The underlying file accessor to be decorated with encryption.</param>
    /// <param name="key">The encryption key used for AES-GCM. Must be 128, 192, or 256 bits (16, 24, or 32 bytes).</param>
    public CryptoFileAccessor(FileAccessor fileAccessor, byte[] key)
    {
        _fileAccessor = fileAccessor;
        _key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoFileAccessor"/> class with a specified file path.
    /// </summary>
    /// <param name="path">Path to the file where preference data will be stored.</param>
    /// <param name="key">The encryption key used for AES-GCM. Must be 128, 192, or 256 bits (16, 24, or 32 bytes).</param>
    public CryptoFileAccessor(in string path, byte[] key) : this(Create(path), key)
    {
    }

    /// <inheritdoc />
    public override byte[] ReadAllBytes()
    {
        var data = _fileAccessor.ReadAllBytes();
        if (data.Length == 0)
        {
            return [];
        }

        if (data.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Encrypted data is too short to contain a valid nonce and authentication tag.");
        }

        var nonce = data.AsSpan(0, NonceSize);
        var tag = data.AsSpan(NonceSize, TagSize);
        var ciphertext = data.AsSpan(NonceSize + TagSize);

        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Rent a single output buffer: [nonce (12)][tag (16)][ciphertext (N)]
        var outputSize = NonceSize + TagSize + bytes.Length;
        var buffer = ArrayPool<byte>.Shared.Rent(outputSize);
        try
        {
            var nonce = buffer.AsSpan(0, NonceSize);
            var tag = buffer.AsSpan(NonceSize, TagSize);
            var ciphertext = buffer.AsSpan(NonceSize + TagSize, bytes.Length);

            RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(_key);
            aes.Encrypt(nonce, bytes.Span, ciphertext, tag);

            await _fileAccessor.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, outputSize), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    /// <inheritdoc />
    public override ValueTask DeleteAsync(CancellationToken cancellationToken = default) =>
        _fileAccessor.DeleteAsync(cancellationToken);
}
