using System.Security.Cryptography;

namespace AndanteTribe.IO;

/// <summary>
/// A file accessor that encrypts and decrypts file content using AES encryption.
/// Implements the decorator pattern by wrapping another FileAccessor instance.
/// </summary>
/// <param name="fileAccessor">The underlying file accessor to be decorated with encryption.</param>
/// <param name="key">The encryption key used for AES encryption.</param>
/// <param name="iv">The initialization vector used for AES encryption.</param>
/// <param name="mode">The cipher mode to use for AES encryption. Defaults to CBC.</param>
public class CryptoFileAccessor(FileAccessor fileAccessor, byte[] key, byte[] iv, CipherMode mode = CipherMode.CBC) : FileAccessor
{
    /// <inheritdoc />
    protected internal override string SavePath => fileAccessor.SavePath;

    /// <summary>
    /// The underlying file accessor that performs actual file operations.
    /// </summary>
    /// <param name="fileAccessor">FileAccessor to be decorated with encryption.</param>
    /// <param name="key">Encryption key used for AES encryption.</param>
    public CryptoFileAccessor(FileAccessor fileAccessor, byte[] key) : this(fileAccessor, key, [], CipherMode.ECB)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoFileAccessor"/> class with a specified file path.
    /// </summary>
    /// <param name="path">Path to the file where preference data will be stored.</param>
    /// <param name="key">Encryption key used for AES encryption.</param>
    /// <param name="iv">Initialization vector used for AES encryption.</param>
    /// <param name="mode">Cipher mode to use for AES encryption. Defaults to CBC.</param>
    public CryptoFileAccessor(in string path, byte[] key, byte[] iv, CipherMode mode = CipherMode.CBC) : this(Create(path), key, iv, mode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoFileAccessor"/> class with a specified file path and encryption key.
    /// </summary>
    /// <param name="path">Path to the file where preference data will be stored.</param>
    /// <param name="key">Encryption key used for AES encryption.</param>
    public CryptoFileAccessor(in string path, byte[] key) : this(Create(path), key, [], CipherMode.ECB)
    {
    }

    /// <inheritdoc />
    public override byte[] ReadAllBytes()
    {
        var encryptedBytes = fileAccessor.ReadAllBytes();
        if (encryptedBytes.Length == 0)
        {
            return [];
        }

        using var aes = CreateAes();
        using var decryptor = aes.CreateDecryptor();
        using var memoryStream = new MemoryStream(encryptedBytes);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var decryptedStream = new MemoryStream();
        cryptoStream.CopyTo(decryptedStream);
        return decryptedStream.ToArray();
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var aes = CreateAes();
        using var encryptor = aes.CreateEncryptor();
        using var memoryStream = new MemoryStream();
        await using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        cryptoStream.Write(bytes.Span);
        cryptoStream.FlushFinalBlock();
        await fileAccessor.WriteAsync(new(memoryStream.GetBuffer(), 0, (int)memoryStream.Length), cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask DeleteAsync(CancellationToken cancellationToken = default) =>
        fileAccessor.DeleteAsync(cancellationToken);

    /// <summary>
    /// Creates an AES algorithm instance with the provided key, IV, and cipher mode.
    /// </summary>
    /// <returns>A configured AES algorithm instance.</returns>
    private Aes CreateAes()
    {
        var aes = Aes.Create();
        aes.Key = key;
        if (iv.Length != 0)
        {
            aes.IV = iv;
        }
        aes.Mode = mode;
        return aes;
    }
}