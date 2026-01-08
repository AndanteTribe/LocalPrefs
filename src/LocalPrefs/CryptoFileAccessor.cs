using System.Security.Cryptography;

namespace AndanteTribe.IO;

/// <summary>
/// A file accessor that encrypts and decrypts file content using cryptographic transforms.
/// Implements the decorator pattern by wrapping another FileAccessor instance.
/// </summary>
/// <param name="fileAccessor">The underlying file accessor to be decorated with encryption.</param>
/// <param name="encryptor">The crypto transform used for encryption.</param>
/// <param name="decryptor">The crypto transform used for decryption.</param>
public class CryptoFileAccessor(FileAccessor fileAccessor, ICryptoTransform encryptor, ICryptoTransform decryptor) : FileAccessor
{
    /// <inheritdoc />
    protected internal override string SavePath => fileAccessor.SavePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoFileAccessor"/> class with a specified file path.
    /// </summary>
    /// <param name="path">Path to the file where preference data will be stored.</param>
    /// <param name="encryptor">The crypto transform used for encryption.</param>
    /// <param name="decryptor">The crypto transform used for decryption.</param>
    public CryptoFileAccessor(in string path, ICryptoTransform encryptor, ICryptoTransform decryptor) : this(Create(path), encryptor, decryptor)
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

        using var memoryStream = new MemoryStream(encryptedBytes);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read, leaveOpen: true);
        using var decryptedStream = new MemoryStream();
        cryptoStream.CopyTo(decryptedStream);
        return decryptedStream.ToArray();
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var memoryStream = new MemoryStream();
        await using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write, leaveOpen: true);
        cryptoStream.Write(bytes.Span);
        cryptoStream.FlushFinalBlock();
        await fileAccessor.WriteAsync(new(memoryStream.GetBuffer(), 0, (int)memoryStream.Length), cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask DeleteAsync(CancellationToken cancellationToken = default) =>
        fileAccessor.DeleteAsync(cancellationToken);
}