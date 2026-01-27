// C# Memory Safety Library - Safe API (Future Conventions)
//
// This file demonstrates SUPPRESSING unsafety under the future model.
//
// The key technique remains the same:
// - Don't expose `unsafe` members in public API
// - Contain all unsafe operations in minimal unsafe blocks
// - Provide bounds checking and proper cleanup
//
// FUTURE CHANGE: The `unsafe` modifier on members (not just blocks)
// now propagates to callers. This means:
// - A method marked `unsafe void Foo()` requires callers to use unsafe
// - A method with `unsafe { }` blocks but no unsafe modifier is SAFE
//
// See: https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/

using System;
using System.Runtime.InteropServices;

namespace MemoryLib;

/// <summary>
/// A safe wrapper that SUPPRESSES unsafety internally.
///
/// FUTURE MODEL (MemorySafetyRules):
/// - This class has NO `unsafe` modifier on its public members
/// - All unsafe operations are in MINIMAL unsafe blocks
/// - Callers do NOT need unsafe context
///
/// This demonstrates the "safe abstraction over unsafe internals" pattern,
/// similar to Rust's approach.
///
/// <para><b>Safety Contract:</b></para>
/// The class maintains these invariants:
/// <list type="bullet">
/// <item>_buffer always points to valid, allocated memory (or IntPtr.Zero)</item>
/// <item>_length accurately reflects the allocation size</item>
/// <item>All public access is bounds-checked</item>
/// </list>
/// </summary>
public sealed class SafeBuffer : IDisposable
{
    // FUTURE: Fields can be marked `unsafe` when they carry safety contracts.
    // This field carries the invariant that it always points to valid memory.
    // Accessing it directly would require unsafe context.
    //
    // For now, we use IntPtr which is "safe" syntactically.
    private IntPtr _buffer;
    private readonly int _length;
    private bool _disposed;

    /// <summary>
    /// Creates a new buffer. NO unsafe context required by caller.
    /// </summary>
    /// <remarks>
    /// SAFETY DISCHARGE for RawAlloc obligations:
    /// - count > 0: Validated by guard clause above
    /// - Must free: Handled by Dispose()
    /// - No use after free: _disposed flag prevents access
    /// - Initialization: Zero-filled in loop below
    /// </remarks>
    public SafeBuffer(int length)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be positive", nameof(length));

        _length = length;

        // MINIMAL UNSAFE BLOCK: Contains only the unsafe operations
        unsafe
        {
            int* ptr = RawMemory.RawAlloc(length);

            // SAFETY DISCHARGE: Initialize to zero (no uninitialized reads)
            for (int i = 0; i < length; i++)
            {
                ptr[i] = 0;
            }

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
    /// <remarks>
    /// SAFETY DISCHARGE for pointer access:
    /// - Valid memory: ThrowIfDisposed ensures buffer not freed
    /// - Bounds: Explicit check ensures 0 &lt;= index &lt; _length
    /// - Initialized: Constructor zero-initializes all elements
    /// </remarks>
    public int this[int index]
    {
        get
        {
            // Validation in SAFE code, BEFORE entering unsafe
            ThrowIfDisposed();
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_length})");

            // MINIMAL UNSAFE BLOCK: Only the pointer dereference
            // SAFETY DISCHARGE: bounds checked above, ptr valid by invariant
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

            // SAFETY DISCHARGE: bounds checked above, ptr valid
            unsafe
            {
                ((int*)_buffer)[index] = value;
            }
        }
    }

    /// <summary>
    /// Try-pattern for safe access. NO unsafe required by caller.
    /// </summary>
    /// <remarks>
    /// Returns false instead of throwing for invalid access.
    /// This is a fully safe API.
    /// </remarks>
    public bool TryGet(int index, out int value)
    {
        if (_disposed || index < 0 || index >= _length)
        {
            value = default;
            return false;
        }

        // MINIMAL UNSAFE BLOCK
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

        // MINIMAL UNSAFE BLOCK
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
    /// </summary>
    /// <remarks>
    /// SAFETY DISCHARGE for returning Span:
    /// - Valid memory: ThrowIfDisposed ensures buffer not freed
    /// - Correct length: _length is immutable, set at construction
    /// - Lifetime: Span is ref struct, cannot outlive this buffer
    ///
    /// CALLER OBLIGATION: Do not use the returned Span after Dispose().
    /// </remarks>
    public Span<int> AsSpan()
    {
        ThrowIfDisposed();

        // SAFETY DISCHARGE: Pointer valid, length accurate
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

    ~SafeBuffer() => Dispose();

    /// <summary>
    /// Releases the unmanaged memory.
    /// </summary>
    /// <remarks>
    /// Uses the safe Marshal.FreeHGlobal API (IntPtr-based).
    /// No unsafe block needed here.
    /// </remarks>
    public void Dispose()
    {
        if (!_disposed)
        {
            // Safe API - no unsafe block needed
            Marshal.FreeHGlobal(_buffer);
            _buffer = IntPtr.Zero;
            _disposed = true;
        }
    }
}
