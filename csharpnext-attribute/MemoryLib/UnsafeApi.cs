// C# Memory Safety Library - Unsafe API (Attribute-Based Model)
//
// This file demonstrates the ATTRIBUTE-BASED model for unsafe propagation.
//
// KEY DIFFERENCE from the `unsafe` keyword model:
// - `unsafe` keyword: Still means "I'm using pointer types"
// - [RequiresUnsafe]: New attribute meaning "This is semantically unsafe"
//
// The two can be independent:
// - A method can use pointers (needs `unsafe`) but be safe semantically
// - A method can be unsafe semantically (needs [RequiresUnsafe]) without pointers
// - A method can be both
//
// This gives finer-grained control over what kind of unsafety is involved.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryLib;

/// <summary>
/// Internal low-level unsafe operations.
/// </summary>
internal static class RawMemory
{
    /// <summary>
    /// Low-level allocation.
    ///
    /// Uses BOTH forms of unsafety:
    /// - `unsafe` keyword: Because it returns a pointer
    /// - [RequiresUnsafe]: Because caller must manage lifetime
    /// </summary>
    [RequiresUnsafe("Caller must free memory and not use after free")]
    internal static unsafe int* RawAlloc(int count)
    {
        return (int*)Marshal.AllocHGlobal(count * sizeof(int));
    }

    /// <summary>
    /// Low-level deallocation.
    /// </summary>
    [RequiresUnsafe("Pointer must have been allocated by RawAlloc")]
    internal static unsafe void RawDealloc(int* ptr)
    {
        Marshal.FreeHGlobal((IntPtr)ptr);
    }

    /// <summary>
    /// Mid-level allocation returning uninitialized memory.
    ///
    /// [RequiresUnsafe] because: memory is uninitialized, reading before
    /// writing is undefined behavior.
    /// </summary>
    [RequiresUnsafe("Returns uninitialized memory - must write before reading")]
    internal static unsafe int* MidLevelAllocUninit(int count)
    {
        return RawAlloc(count);
    }

    /// <summary>
    /// Mid-level allocation that initializes memory.
    /// Returns IntPtr to suppress pointer unsafety.
    ///
    /// NOT marked [RequiresUnsafe] because:
    /// - Memory is initialized (safe to read)
    /// - Returns IntPtr (no pointer manipulation by caller)
    /// </summary>
    /// <remarks>
    /// The unsafe block handles pointer operations internally.
    /// The [RequiresUnsafe] from RawAlloc is acknowledged here.
    /// Callers of this method don't need any unsafe acknowledgment.
    /// </remarks>
    internal static IntPtr MidLevelAllocZeroedAsHandle(int count)
    {
        unsafe
        {
            // We're in unsafe block, so [RequiresUnsafe] is acknowledged
            int* ptr = RawAlloc(count);
            for (int i = 0; i < count; i++)
            {
                ptr[i] = 0;
            }
            return (IntPtr)ptr;
        }
    }
}

/// <summary>
/// PUBLIC API that PROPAGATES unsafety via attributes.
///
/// Each method documents its specific unsafety via [RequiresUnsafe].
/// This is more descriptive than just `unsafe` - callers know WHY.
/// </summary>
public static class UnsafeApi
{
    /// <summary>
    /// Allocates a buffer of integers.
    /// </summary>
    /// <remarks>
    /// Two types of unsafety:
    /// - `unsafe` keyword: Returns pointer type
    /// - [RequiresUnsafe]: Caller must manage memory lifetime
    /// </remarks>
    [RequiresUnsafe("Caller must call Free() and not use pointer after free")]
    public static unsafe int* Alloc(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        int* ptr = RawMemory.RawAlloc(count);

        for (int i = 0; i < count; i++)
        {
            ptr[i] = 0;
        }

        return ptr;
    }

    /// <summary>
    /// Frees allocated memory.
    /// </summary>
    [RequiresUnsafe("Pointer must have been allocated by Alloc()")]
    public static unsafe void Free(int* ptr)
    {
        RawMemory.RawDealloc(ptr);
    }

    /// <summary>
    /// Reads value at offset.
    /// </summary>
    [RequiresUnsafe("Offset must be within allocated bounds")]
    public static unsafe int Read(int* ptr, int offset)
    {
        return ptr[offset];
    }

    /// <summary>
    /// Writes value at offset.
    /// </summary>
    [RequiresUnsafe("Offset must be within allocated bounds")]
    public static unsafe void Write(int* ptr, int offset, int value)
    {
        ptr[offset] = value;
    }
}

/// <summary>
/// Demonstrates different propagation scenarios with attributes.
/// </summary>
public static class PropagationChain
{
    /// <summary>
    /// Level 1: Uses pointers and is semantically unsafe.
    /// </summary>
    [RequiresUnsafe("Returns unmanaged memory")]
    private static unsafe int* Level1Unsafe()
    {
        return RawMemory.RawAlloc(1);
    }

    /// <summary>
    /// Level 2: Propagates both pointer and semantic unsafety.
    /// </summary>
    [RequiresUnsafe("Propagates Level1 unsafety")]
    private static unsafe int* Level2Unsafe()
    {
        return Level1Unsafe();
    }

    /// <summary>
    /// Level 3 PROPAGATE: Public method that propagates unsafety.
    ///
    /// Marked with [RequiresUnsafe] so callers must acknowledge.
    /// </summary>
    [RequiresUnsafe("Returns pointer to unmanaged memory - caller must free")]
    public static unsafe int* Level3Propagate()
    {
        return Level2Unsafe();
    }

    /// <summary>
    /// Level 3 SUPPRESS: Public SAFE method.
    ///
    /// NOT marked [RequiresUnsafe] - we contain the unsafety here.
    /// The unsafe block acknowledges both pointer ops and [RequiresUnsafe].
    /// </summary>
    public static IntPtr Level3Suppress()
    {
        unsafe
        {
            // unsafe block acknowledges [RequiresUnsafe] on Level2Unsafe
            return (IntPtr)Level2Unsafe();
        }
    }

    /// <summary>
    /// Cleanup with pointer - both unsafe and [RequiresUnsafe].
    /// </summary>
    [RequiresUnsafe("Pointer must have been from Level3Propagate")]
    public static unsafe void Cleanup(int* ptr)
    {
        RawMemory.RawDealloc(ptr);
    }

    /// <summary>
    /// Cleanup with IntPtr - safe, no attributes needed.
    /// </summary>
    public static void CleanupSafe(IntPtr ptr)
    {
        Marshal.FreeHGlobal(ptr);
    }
}

/// <summary>
/// Demonstrates [RequiresUnsafe] WITHOUT pointer types.
///
/// This is the key benefit of the attribute model: we can mark
/// methods as unsafe even when they don't use pointers.
/// </summary>
public static class SemanticUnsafetyExamples
{
    /// <summary>
    /// Returns uninitialized memory as a span.
    ///
    /// No pointers in the signature, but still unsafe because
    /// reading before writing is undefined behavior.
    /// </summary>
    [RequiresUnsafe("Returns uninitialized memory - write before reading")]
    public static Span<int> AllocUninitializedSpan(int length)
    {
        // Using GC.AllocateUninitializedArray which doesn't zero memory
        int[] array = GC.AllocateUninitializedArray<int>(length);
        return array.AsSpan();
    }

    /// <summary>
    /// Safe version - initializes memory.
    ///
    /// No [RequiresUnsafe] because memory is zeroed.
    /// </summary>
    public static Span<int> AllocZeroedSpan(int length)
    {
        int[] array = new int[length]; // Zero-initialized
        return array.AsSpan();
    }

    /// <summary>
    /// Demonstrates that [RequiresUnsafe] and `unsafe` are independent.
    ///
    /// This method:
    /// - Has [RequiresUnsafe]: Semantic contract about array types
    /// - No `unsafe` keyword: Doesn't use pointers
    /// </summary>
    [RequiresUnsafe("Arrays must be compatible types or behavior is undefined")]
    public static TTo[] ReinterpretArray<TFrom, TTo>(TFrom[] array)
        where TFrom : class
        where TTo : class
    {
        // Would use Unsafe.As internally
        // No pointer types, but semantically unsafe
        unsafe
        {
            return Unsafe.As<TFrom[], TTo[]>(array);
        }
    }
}
