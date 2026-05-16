using System.Buffers;
using System.Security.Cryptography;

namespace AndanteTribe.IO;

/// <summary>
/// A file accessor that encrypts and decrypts file content using AES-CBC encryption
/// with HMAC-SHA256 integrity verification. Implements the decorator pattern by wrapping
/// another <see cref="FileAccessor"/> instance.
/// </summary>
/// <remarks>
/// The on-disk format is: <c>[HMAC-SHA256 (32 bytes)][random IV (16 bytes)][AES-CBC ciphertext]</c>.
/// A fresh random IV is generated for every write, so the same plaintext never produces the
/// same ciphertext twice. The HMAC covers both the IV and the ciphertext, so any tampering is
/// detected before decryption (a <see cref="CryptographicException"/> is thrown when
/// verification fails).
/// </remarks>
public class CryptoFileAccessor : FileAccessor
{
    private const int HmacSize = 32;  // HMAC-SHA256 output length (256 bits)
    private const int IvSize = 16;    // AES block size / CBC IV length (128 bits)

    private readonly FileAccessor _fileAccessor;
    private readonly byte[] _aesKey;
    private readonly byte[] _hmacKey;

    /// <inheritdoc />
    protected internal override string SavePath => _fileAccessor.SavePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoFileAccessor"/> class.
    /// </summary>
    /// <param name="fileAccessor">The underlying file accessor to be decorated with encryption.</param>
    /// <param name="key">The master encryption key used for AES-CBC encryption. Must be 128, 192, or 256 bits (16, 24, or 32 bytes). A separate HMAC key is derived from this key automatically.</param>
    public CryptoFileAccessor(FileAccessor fileAccessor, byte[] key)
    {
        _fileAccessor = fileAccessor;
        _aesKey = key;
        _hmacKey = DeriveHmacKey(key);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoFileAccessor"/> class with a specified file path.
    /// </summary>
    /// <param name="path">Path to the file where preference data will be stored.</param>
    /// <param name="key">The master encryption key used for AES-CBC encryption. Must be 128, 192, or 256 bits (16, 24, or 32 bytes). A separate HMAC key is derived from this key automatically.</param>
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

        if (data.Length < HmacSize + IvSize)
        {
            throw new CryptographicException("Encrypted data is too short to contain a valid HMAC and IV.");
        }

        // Verify HMAC over [IV || ciphertext] before decrypting
        using var hmac = new HMACSHA256(_hmacKey);
        var computedHmac = hmac.ComputeHash(data, HmacSize, data.Length - HmacSize);
        if (!CryptographicOperations.FixedTimeEquals(computedHmac, data.AsSpan(0, HmacSize)))
        {
            throw new CryptographicException("HMAC verification failed. The data may be corrupted or tampered with.");
        }

        // Decrypt using the stored IV
        var iv = data.AsSpan(HmacSize, IvSize).ToArray();
        using var aes = CreateAes(iv);
        using var decryptor = aes.CreateDecryptor();
        using var ciphertextStream = new MemoryStream(data, HmacSize + IvSize, data.Length - HmacSize - IvSize);
        using var cryptoStream = new CryptoStream(ciphertextStream, decryptor, CryptoStreamMode.Read);
        using var decryptedStream = new MemoryStream();
        cryptoStream.CopyTo(decryptedStream);
        return decryptedStream.ToArray();
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Generate a fresh random IV for every write
        var iv = new byte[IvSize];
        RandomNumberGenerator.Fill(iv);

        // Encrypt the plaintext with AES-CBC
        byte[] ciphertext;
        using var aes = CreateAes(iv);
        using var encryptor = aes.CreateEncryptor();
        using (var ciphertextStream = new MemoryStream())
        {
            using var cryptoStream = new CryptoStream(ciphertextStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(bytes.Span);
            cryptoStream.FlushFinalBlock();
            ciphertext = ciphertextStream.ToArray();
        }

        // Rent a single output buffer: [HMAC (32)][IV (16)][ciphertext]
        // The source span buffer[HmacSize..] and destination span buffer[0..HmacSize] do not overlap,
        // so TryComputeHash can safely write the HMAC directly into the buffer prefix.
        var totalSize = HmacSize + IvSize + ciphertext.Length;
        var buffer = ArrayPool<byte>.Shared.Rent(totalSize);
        try
        {
            iv.CopyTo(buffer, HmacSize);
            ciphertext.CopyTo(buffer, HmacSize + IvSize);
            using var hmac = new HMACSHA256(_hmacKey);
            hmac.TryComputeHash(buffer.AsSpan(HmacSize, IvSize + ciphertext.Length), buffer.AsSpan(0, HmacSize), out _);
            await _fileAccessor.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, totalSize), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    /// <inheritdoc />
    public override ValueTask DeleteAsync(CancellationToken cancellationToken = default) =>
        _fileAccessor.DeleteAsync(cancellationToken);

    /// <summary>
    /// Creates an AES-CBC algorithm instance configured with the provided IV.
    /// </summary>
    /// <param name="iv">The 16-byte initialization vector for this operation.</param>
    /// <returns>A configured <see cref="Aes"/> instance.</returns>
    private Aes CreateAes(byte[] iv)
    {
        var aes = Aes.Create();
        aes.Key = _aesKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        return aes;
    }

    /// <summary>
    /// Derives a dedicated HMAC key from the master key by computing
    /// HMAC-SHA256 over a fixed label using the master key as the HMAC key.
    /// This prevents the same key material from being used for both AES-CBC encryption
    /// and MAC computation.
    /// </summary>
    /// <param name="masterKey">The master encryption key.</param>
    /// <returns>A 32-byte HMAC key derived from <paramref name="masterKey"/>.</returns>
    private static byte[] DeriveHmacKey(byte[] masterKey)
    {
        using var hmac = new HMACSHA256(masterKey);
        return hmac.ComputeHash("LocalPrefs.HMAC"u8.ToArray());
    }
}