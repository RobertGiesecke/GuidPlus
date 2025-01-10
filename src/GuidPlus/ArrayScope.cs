using System;
using System.Buffers;

namespace GuidPlus;

/// <summary>
/// create new <see cref="ArrayScope{T}"/> instances/>
/// </summary>
public static class ArrayScope
{
    /// <summary>
    /// create a new <see cref="ArrayScope{T}"/>./>
    /// </summary>
    /// <param name="size">the size of the rented array</param>
    /// <param name="clearOnReturn"></param>
    /// <typeparam name="T">true if contents should be cleared before returning</typeparam>
    /// <returns>new <see cref="ArrayScope{T}"/></returns>
    public static ArrayScope<T> Rent<T>(int size, bool clearOnReturn = false) =>
        new(ArrayPool<T>.Shared.Rent(size), size, clearOnReturn);
}

/// <summary>
/// Will return an array to <see cref="ArrayPool{T}"/> when disposed./>
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct ArrayScope<T> : IDisposable
{
    /// <summary>
    /// the rented Array
    /// </summary>
    public T[] Array { get; }

    /// <summary>
    /// the requested size of the rented Array
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// returns a span of the exact size that was rented
    /// </summary>
    /// <returns></returns>
    public Span<T> AsSpan() => Array.AsSpan(0, Size);

    private readonly bool _clearOnReturn;

    /// <summary>
    /// creates a new instance
    /// </summary>
    /// <param name="array">the array to return after dispose</param>
    /// <param name="size">the size of the requested array.</param>
    /// <param name="clearOnReturn">true if contents should be cleared before returning</param>
    public ArrayScope(T[] array, int size, bool clearOnReturn = false)
    {
        Array = array ?? throw new ArgumentNullException(nameof(array));
        Size = size;
        if (size > array.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                $"{nameof(size)} cannot be larger than {nameof(array)}{nameof(array.Length)}({array.Length}).");
        }
        _clearOnReturn = clearOnReturn;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(Array, _clearOnReturn);
    }
}
