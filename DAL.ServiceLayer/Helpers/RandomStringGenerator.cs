using System.Buffers;
using System.Security.Cryptography;

namespace DAL.ServiceLayer.Helpers;

public static class RandomStringGenerator
{
    private static readonly char[] AlphanumericChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    private static readonly char[] Base64Chars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/".ToCharArray();

    public static string GenerateDeviceId(int length = 200)
    {
        return GenerateRandomString(length, AlphanumericChars);
    }

    public static string GenerateDeviceToken(int length = 200)
    {
        return GenerateRandomString(length, Base64Chars);
    }

    private static string GenerateRandomString(int length, char[] charSet)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive");

        if (charSet == null || charSet.Length == 0)
            throw new ArgumentException("Character set cannot be empty", nameof(charSet));

        // Calculate required bytes (4 bytes per character for UInt32 conversion)
        int requiredBytes = length * 4;

        // Use stackalloc for small buffers, ArrayPool for larger ones
        byte[]? rentedBytes = null;
        Span<byte> randomBytes = requiredBytes <= 1024
            ? stackalloc byte[requiredBytes]
            : (rentedBytes = ArrayPool<byte>.Shared.Rent(requiredBytes)).AsSpan(0, requiredBytes);

        char[]? rentedChars = null;
        Span<char> buffer = length <= 256
            ? stackalloc char[length]
            : (rentedChars = ArrayPool<char>.Shared.Rent(length)).AsSpan(0, length);

        try
        {
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            for (int i = 0; i < length; i++)
            {
                uint randomNumber = BitConverter.ToUInt32(randomBytes.Slice(i * 4, 4));
                buffer[i] = charSet[randomNumber % (uint)charSet.Length];
            }

            return new string(buffer);
        }
        finally
        {
            if (rentedBytes != null)
                ArrayPool<byte>.Shared.Return(rentedBytes);

            if (rentedChars != null)
                ArrayPool<char>.Shared.Return(rentedChars);
        }
    }
}