// C# Memory Safety Library - Safe API
//
// This file demonstrates SUPPRESSING unsafety so it doesn't propagate
// to consumers (CROSS-MODULE suppression).
//
// The key technique: Don't expose pointer types in public signatures.
// Use IntPtr, Span<T>, or wrapper classes instead.

using System;
using System.Runtime.InteropServices;

namespace MemoryLib;

/// <summary>
/// A safe wrapper that SUPPRESSES unsafety internally.
///
/// CROSS-MODULE: Consumers can use this without unsafe contexts because:
/// 1. No pointer types in public signatures
/// 2. All unsafe operations contained in internal unsafe blocks
///
/// This is the C# equivalent of Rust's pattern of wrapping unsafe code
/// in safe abstractions.
/// </summary>
public sealed class SafeBuffer : IDisposable
{
    private IntPtr _buffer;  // IntPtr is "safe" - no unsafe needed to hold it
    private readonly int _length;
    private bool _disposed;

    /// <summary>
    /// Creates a new buffer. NO unsafe context required by caller.
    /// </summary>
    public SafeBuffer(int length)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be positive", nameof(length));

        _length = length;

        // CROSS-FUNCTION: We use unsafe internally, but it's contained
        unsafe
        {
            // Call internal unsafe function
            int* ptr = RawMemory.RawAlloc(length);

            // Initialize to zero
            for (int i = 0; i < length; i++)
            {
                ptr[i] = 0;
            }

            // Store as IntPtr - this "suppresses" the unsafety
            _buffer = (IntPtr)ptr;
        }
    }

    /// <summary>
    /// Gets the length. Safe property, no unsafe needed.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Indexer with bounds checking. NO unsafe required by caller.
    /// </summary>
    public int this[int index]
    {
        get
        {
            ThrowIfDisposed();
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_length})");

            // Unsafe operation contained internally
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

            // Unsafe operation contained internally
            unsafe
            {
                ((int*)_buffer)[index] = value;
            }
        }
    }

    /// <summary>
    /// Try-pattern for safe access. NO unsafe required by caller.
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
    /// Try-pattern for safe mutation. NO unsafe required by caller.
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
    /// Get as Span - safe, bounds-checked view. NO unsafe required.
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
            // Cleanup using safe IntPtr-based API
            Marshal.FreeHGlobal(_buffer);
            _buffer = IntPtr.Zero;
            _disposed = true;
        }
    }
}
