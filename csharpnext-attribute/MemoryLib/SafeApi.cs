// C# Memory Safety Library - Safe API (Attribute-Based Model)
//
// This file demonstrates SUPPRESSING unsafety in the attribute model.
//
// KEY INSIGHT: The unsafe block serves double duty:
// 1. Enables pointer operations (current meaning)
// 2. Acknowledges [RequiresUnsafe] attributes (new meaning)
//
// A method is SAFE (no [RequiresUnsafe]) when:
// - All [RequiresUnsafe] calls are in unsafe blocks
// - No pointer types in its public signature
// - It provides bounds checking and proper cleanup

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryLib;

/// <summary>
/// A safe wrapper that SUPPRESSES unsafety internally.
///
/// ATTRIBUTE MODEL:
/// - This class has NO [RequiresUnsafe] on its public members
/// - All unsafe operations (both pointer and semantic) contained internally
/// - Callers don't need `unsafe` blocks or [RequiresUnsafe]
///
/// The unsafe blocks inside acknowledge both:
/// 1. Pointer operations
/// 2. [RequiresUnsafe] on called methods
/// </summary>
public sealed class SafeBuffer : IDisposable
{
    private IntPtr _buffer;
    private readonly int _length;
    private bool _disposed;

    /// <summary>
    /// Creates a new buffer. NO unsafe acknowledgment required by caller.
    /// </summary>
    /// <remarks>
    /// Implementation contains unsafety:
    /// - Pointer operations for initialization
    /// - [RequiresUnsafe] from RawMemory.RawAlloc
    ///
    /// Both are acknowledged by the unsafe block.
    /// </remarks>
    public SafeBuffer(int length)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be positive", nameof(length));

        _length = length;

        // unsafe block acknowledges:
        // 1. Pointer operations (int*)
        // 2. [RequiresUnsafe] on RawMemory.RawAlloc
        unsafe
        {
            int* ptr = RawMemory.RawAlloc(length);

            // Initialize to zero - ensures safety of reads
            for (int i = 0; i < length; i++)
            {
                ptr[i] = 0;
            }

            _buffer = (IntPtr)ptr;
        }
    }

    /// <summary>
    /// Gets the length. Safe property.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Indexer with bounds checking. NO [RequiresUnsafe] needed.
    /// </summary>
    /// <remarks>
    /// Safe because:
    /// - Bounds checking before pointer access
    /// - Disposed checking before access
    /// - Pointer operations contained in unsafe blocks
    /// </remarks>
    public int this[int index]
    {
        get
        {
            ThrowIfDisposed();
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_length})");

            // Pointer operation contained
            unsafe
            {
                return ((int*)_buffer)[index];
            }
        }
        set
        {
            ThrowIfDisposed();
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_length})");

            unsafe
            {
                ((int*)_buffer)[index] = value;
            }
        }
    }

    /// <summary>
    /// Try-pattern for safe access.
    /// </summary>
    public bool TryGet(int index, out int value)
    {
        if (_disposed || index < 0 || index >= _length)
        {
            value = default;
            return false;
        }

        unsafe
        {
            value = ((int*)_buffer)[index];
            return true;
        }
    }

    /// <summary>
    /// Try-pattern for safe mutation.
    /// </summary>
    public bool TrySet(int index, int value)
    {
        if (_disposed || index < 0 || index >= _length)
        {
            return false;
        }

        unsafe
        {
            ((int*)_buffer)[index] = value;
            return true;
        }
    }

    /// <summary>
    /// Get as Span - safe, bounds-checked view.
    /// </summary>
    public Span<int> AsSpan()
    {
        ThrowIfDisposed();

        unsafe
        {
            return new Span<int>((void*)_buffer, _length);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SafeBuffer));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Marshal.FreeHGlobal(_buffer);
            _buffer = IntPtr.Zero;
            _disposed = true;
        }
    }
}

/// <summary>
/// Demonstrates the difference between [RequiresUnsafe] propagation
/// and suppression in the attribute model.
/// </summary>
public static class PropagationVsSuppression
{
    /// <summary>
    /// PROPAGATES: Marked [RequiresUnsafe], callers must acknowledge.
    /// </summary>
    [RequiresUnsafe("Returns reference to uninitialized memory")]
    public static ref int GetUninitializedRef()
    {
        var array = GC.AllocateUninitializedArray<int>(1);
        return ref array[0];
    }

    /// <summary>
    /// SUPPRESSES: No [RequiresUnsafe], safe for callers.
    ///
    /// Uses unsafe block to acknowledge [RequiresUnsafe] internally.
    /// </summary>
    public static ref int GetZeroedRef()
    {
        // Safe: uses zero-initialized array
        var array = new int[1];
        return ref array[0];
    }

    /// <summary>
    /// Shows explicit suppression with documentation.
    ///
    /// The unsafe block acknowledges the semantic unsafety,
    /// and we document WHY it's safe in this context.
    /// </summary>
    public static string[] ConvertToStringArray(object[] objects)
    {
        // Validate all elements are strings first (makes it safe)
        foreach (var obj in objects)
        {
            if (obj is not string)
                throw new InvalidCastException("All elements must be strings");
        }

        // Now the Unsafe.As is actually safe because we validated
        // The unsafe block acknowledges [RequiresUnsafe] on Unsafe.As
        unsafe
        {
            return Unsafe.As<object[], string[]>(objects);
        }
    }
}
