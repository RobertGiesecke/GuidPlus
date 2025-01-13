using System;
using System.Security.Cryptography;

namespace GuidPlus.Internal;

/// <summary>
/// helper class to randomize a byte array or span
/// </summary>
internal static class RandomBytes
{
    static readonly RandomNumberGenerator SharedRandomNumberGenerator = RandomNumberGenerator.Create();

    [ThreadStatic] private static Random _random;

    private static Random GetRandom() => _random ??= new Random(GetRandomSeed());

    private static int GetRandomSeed()
    {
        using var bytes = ArrayScope.Rent<byte>(4);
        SharedRandomNumberGenerator.GetBytes(bytes.Array, 0, 4);
        var seed = BitConverter.ToInt32(bytes.Array, 0);
        return seed;
    }

#if NETSTANDARD2_0
    public static void GetBytes(byte[] bytes, int length)
    {
        GetRandom().NextBytes(bytes);
    }

    public static void GetBytes(Span<byte> bytes)
    {
        GetBytes(bytes, bytes.Length);
    }

    public static void GetBytes(Span<byte> bytes, int length)
    {
        using var copy = ArrayScope.Rent<byte>(length);
        RandomBytes.GetBytes(copy.Array, length);
        copy.AsSpan().CopyTo(bytes);
    }
#else
    public static void GetBytes(byte[] bytes, int length)
    {
        if (bytes.Length == length)
        {
            GetRandom().NextBytes(bytes);
            return;
        }

        var span = bytes.AsSpan().Slice(0, length);
        GetRandom().NextBytes(span);
    }
#endif

#if !NETSTANDARD2_0
    public static void GetBytes(Span<byte> bytes)
    {
        GetRandom().NextBytes(bytes);
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
