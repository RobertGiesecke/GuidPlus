using System;
using System.Security.Cryptography;

namespace GuidPlus;

/// <summary>
/// helper class to randomize a byte array or span
/// </summary>
internal static class RandomBytes
{
    public static void GetBytes(byte[] bytes, int length)
    {
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(bytes, 0, length);
    }

#if !NETSTANDARD2_0
    public static void GetBytes(Span<byte> bytes)
    {
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(bytes);
    }

    public static void GetBytes(Span<byte> bytes, int length)
    {
        if (bytes.Length != length)
        {
            bytes = bytes[..length];
        }
        GetBytes(bytes);
    }
#endif
}
