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
    /// Implementation uses minimal unsafe blocks internally.
    /// The unsafe operations are:
    /// 1. Allocating raw memory
    /// 2. Initializing via pointer arithmetic
    /// These are contained and not visible to callers.
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

            // Initialize to zero - ensures no uninitialized memory reads
            for (int i = 0; i < length; i++)
            {
                ptr[i] = 0;
            }

            _buffer = (IntPtr)ptr;
        }
        // After this block, we're back in safe code
    }

    /// <summary>
    /// Gets the length. Safe property, no unsafe needed.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Indexer with bounds checking. NO unsafe required by caller.
    /// </summary>
    /// <remarks>
    /// The bounds check happens BEFORE entering the unsafe block.
    /// This ensures the unsafe operation is always valid.
    ///
    /// CONVENTION: Keep unsafe blocks minimal - only the actual
    /// pointer operation, not the validation logic.
    /// </remarks>
    public int this[int index]
    {
        get
        {
            // Validation in SAFE code
            ThrowIfDisposed();
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_length})");

            // MINIMAL UNSAFE BLOCK: Only the pointer dereference
            unsafe
            {
                return ((int*)_buffer)[index];
            }
        }
        set
        {
            // Validation in SAFE code
            ThrowIfDisposed();
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_length})");

            // MINIMAL UNSAFE BLOCK: Only the pointer dereference
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
    /// Get as Span - safe, bounds-checked view. NO unsafe required.
    /// </summary>
    /// <remarks>
    /// Span provides memory safety through:
    /// 1. Bounds checking on every access
    /// 2. Cannot outlive the source (ref struct)
    /// </remarks>
    public Span<int> AsSpan()
    {
        ThrowIfDisposed();

        // MINIMAL UNSAFE BLOCK: Only the Span construction
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
