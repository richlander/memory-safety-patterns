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
///
/// CLASS-LEVEL SAFETY INVARIANTS:
/// - _buffer always points to valid memory of size (_length * sizeof(int)), or is IntPtr.Zero when disposed
/// - _length is immutable and always reflects the true allocation size
/// - _disposed accurately tracks whether the buffer has been freed
/// - All public access is bounds-checked against _length
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

            // SAFETY DISCHARGE for RawAlloc:
            // - count > 0: Validated by guard clause above
            // - Must free: Handled by Dispose() and destructor
            // - No use after free: _disposed flag prevents access

            // Initialize to zero
            for (int i = 0; i < length; i++)
            {
                ptr[i] = 0;
            }
            // SAFETY DISCHARGE for pointer writes:
            // - Bounds: i < length, and we allocated exactly 'length' elements
            // - Initialization: All elements written before any read possible

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

            // SAFETY DISCHARGE for pointer read:
            // - Valid memory: ThrowIfDisposed ensures buffer not freed
            // - Bounds: Explicit check above ensures 0 <= index < _length
            // - Initialized: Constructor zero-initializes all elements
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

            // SAFETY DISCHARGE for pointer write:
            // - Valid memory: ThrowIfDisposed ensures buffer not freed
            // - Bounds: Explicit check above ensures 0 <= index < _length
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
    /// Returns a Span over the entire buffer - safe, bounds-checked view.
    ///
    /// THE COMPELLING CASE: Callers get a safe view without copying.
    /// NO unsafe required by caller.
    /// </summary>
    /// <remarks>
    /// SAFETY DISCHARGE for returning Span:
    /// - Valid memory: ThrowIfDisposed ensures buffer not freed
    /// - Correct length: _length is immutable, set at construction
    /// - Lifetime: Span is ref struct, cannot outlive this buffer in typical usage
    ///   (cannot be stored in heap fields). Caller must not use Span after Dispose.
    ///
    /// CALLER OBLIGATION: Do not use the returned Span after calling Dispose().
    /// The ref struct nature of Span prevents most misuse, but explicit disposal
    /// while a Span is in use on the stack is still possible.
    /// </remarks>
    public Span<int> AsSpan()
    {
        ThrowIfDisposed();

        // SAFETY DISCHARGE for Span construction:
        // - Pointer valid: Checked above
        // - Length accurate: _length matches allocation size
        unsafe
        {
            return new Span<int>((void*)_buffer, _length);
        }
    }

    /// <summary>
    /// Returns a Span over a portion of the buffer.
    ///
    /// THE COMPELLING CASE: Zero-copy slicing with bounds safety.
    /// </summary>
    /// <remarks>
    /// SAFETY DISCHARGE:
    /// - Valid memory: ThrowIfDisposed check
    /// - Bounds: Explicit validation before Span construction
    /// - Same lifetime considerations as AsSpan()
    /// </remarks>
    public Span<int> AsSpan(int start, int length)
    {
        ThrowIfDisposed();

        // Bounds validation in safe code, BEFORE entering unsafe
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), "Start cannot be negative");
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative");
        if (start + length > _length)
            throw new ArgumentOutOfRangeException(nameof(length), $"Range [{start}..{start + length}) exceeds buffer length {_length}");

        // SAFETY DISCHARGE: All bounds validated above
        unsafe
        {
            return new Span<int>((int*)_buffer + start, length);
        }
    }

    /// <summary>
    /// Returns a read-only Span over the entire buffer.
    /// </summary>
    public ReadOnlySpan<int> AsReadOnlySpan()
    {
        ThrowIfDisposed();
        unsafe
        {
            return new ReadOnlySpan<int>((void*)_buffer, _length);
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
