// C# Memory Safety Library - Unsafe API (Future Conventions)
//
// This file demonstrates the FUTURE model for unsafe propagation in C#.
//
// KEY CHANGE from current C#:
// - Current: Unsafety propagates only via POINTER TYPES in signatures
// - Future:  Unsafety propagates via MEMBER ANNOTATIONS (`unsafe` modifier)
//
// With MemorySafetyRules enabled:
// - `unsafe` on a method means callers MUST use unsafe context
// - This works like Rust's `unsafe fn` - the annotation itself propagates
// - Unsafe.As, Marshal, etc. will be marked unsafe in the BCL
//
// See: https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/

using System;
using System.Runtime.InteropServices;

namespace MemoryLib;

/// <summary>
/// Internal low-level unsafe operations.
///
/// FUTURE: With MemorySafetyRules, the `unsafe` modifier on methods
/// requires callers to acknowledge unsafety, even without pointers.
/// </summary>
internal static class RawMemory
{
    /// <summary>
    /// Low-level allocation.
    ///
    /// <para><b>Safety:</b> Caller must:</para>
    /// <list type="bullet">
    /// <item>Ensure count > 0</item>
    /// <item>Call RawDealloc with the same count</item>
    /// <item>Not use pointer after deallocation</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// FUTURE: The `unsafe` modifier here means callers must use unsafe context.
    /// Currently, callers need unsafe anyway due to the pointer return type.
    /// </remarks>
    internal static unsafe int* RawAlloc(int count)
    {
        return (int*)Marshal.AllocHGlobal(count * sizeof(int));
    }

    /// <summary>
    /// Low-level deallocation.
    ///
    /// <para><b>Safety:</b> Pointer must have been allocated by RawAlloc.</para>
    /// </summary>
    internal static unsafe void RawDealloc(int* ptr)
    {
        Marshal.FreeHGlobal((IntPtr)ptr);
    }

    /// <summary>
    /// Mid-level allocation that returns uninitialized memory.
    ///
    /// <para><b>Safety:</b> Caller must initialize memory before reading.</para>
    /// </summary>
    /// <remarks>
    /// FUTURE: Even if this returned IntPtr instead of int*, the `unsafe`
    /// modifier would require callers to use unsafe context.
    /// </remarks>
    internal static unsafe int* MidLevelAllocUninit(int count)
    {
        return RawAlloc(count);
    }

    /// <summary>
    /// Mid-level allocation that initializes memory to zero.
    /// Returns IntPtr to demonstrate suppression patterns.
    /// </summary>
    /// <remarks>
    /// This method is SAFE because:
    /// 1. Memory is zero-initialized (no uninitialized read possible)
    /// 2. Returns IntPtr, so no pointer manipulation by caller
    ///
    /// The unsafe block is minimal - only covers the unsafe operations.
    /// </remarks>
    internal static IntPtr MidLevelAllocZeroedAsHandle(int count)
    {
        // MINIMAL UNSAFE BLOCK: Only the pointer operations
        unsafe
        {
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
/// PUBLIC API that PROPAGATES unsafety to consumers.
///
/// FUTURE MODEL (MemorySafetyRules):
/// - The `unsafe` modifier on methods means callers need unsafe context
/// - This works even if we changed signatures to use IntPtr instead of int*
/// - The compiler enforces this based on the ANNOTATION, not just types
///
/// CURRENT MODEL:
/// - Unsafety propagates only because of pointer types in signatures
/// - If we used IntPtr, callers wouldn't need unsafe (a gap!)
/// </summary>
public static class UnsafeApi
{
    /// <summary>
    /// Allocates a buffer of integers.
    ///
    /// <para><b>Safety:</b> Caller must:</para>
    /// <list type="bullet">
    /// <item>Ensure count > 0</item>
    /// <item>Call Free() when done</item>
    /// <item>Not use pointer after Free()</item>
    /// <item>Not read beyond allocated bounds</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// FUTURE: `unsafe` modifier propagates to callers via MemorySafetyRules.
    /// CURRENT: Propagates because of int* return type.
    /// </remarks>
    public static unsafe int* Alloc(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        int* ptr = RawMemory.RawAlloc(count);

        // Initialize to zero - this is a safety measure
        for (int i = 0; i < count; i++)
        {
            ptr[i] = 0;
        }

        return ptr;
    }

    /// <summary>
    /// Frees allocated memory.
    ///
    /// <para><b>Safety:</b> Pointer must have been allocated by Alloc().</para>
    /// </summary>
    public static unsafe void Free(int* ptr)
    {
        RawMemory.RawDealloc(ptr);
    }

    /// <summary>
    /// Reads value at offset.
    ///
    /// <para><b>Safety:</b> Offset must be within allocated bounds.</para>
    /// </summary>
    public static unsafe int Read(int* ptr, int offset)
    {
        return ptr[offset];
    }

    /// <summary>
    /// Writes value at offset.
    ///
    /// <para><b>Safety:</b> Offset must be within allocated bounds.</para>
    /// </summary>
    public static unsafe void Write(int* ptr, int offset, int value)
    {
        ptr[offset] = value;
    }
}

/// <summary>
/// Demonstrates PROPAGATION CHAINS under the future model.
///
/// KEY INSIGHT: In the future model, `unsafe` on a method is like
/// Rust's `unsafe fn` - callers must acknowledge unsafety regardless
/// of whether pointers appear in the signature.
/// </summary>
public static class PropagationChain
{
    /// <summary>
    /// Level 1: Uses pointers internally.
    /// </summary>
    private static unsafe int* Level1Unsafe()
    {
        return RawMemory.RawAlloc(1);
    }

    /// <summary>
    /// Level 2: Calls Level1, propagates unsafety.
    /// </summary>
    private static unsafe int* Level2Unsafe()
    {
        return Level1Unsafe();
    }

    /// <summary>
    /// Level 3 PROPAGATE: Public unsafe method.
    ///
    /// FUTURE: Callers must use unsafe context because of `unsafe` modifier.
    /// CURRENT: Callers must use unsafe context because of int* return type.
    /// </summary>
    public static unsafe int* Level3Propagate()
    {
        return Level2Unsafe();
    }

    /// <summary>
    /// Level 3 SUPPRESS: Public safe method that contains unsafety.
    ///
    /// This method is SAFE because:
    /// 1. No `unsafe` modifier on the method
    /// 2. Unsafe operations are contained in minimal unsafe blocks
    /// 3. Returns IntPtr (safe type)
    ///
    /// Callers do NOT need unsafe context.
    /// </summary>
    public static IntPtr Level3Suppress()
    {
        // MINIMAL UNSAFE BLOCK: Only the conversion
        unsafe
        {
            return (IntPtr)Level2Unsafe();
        }
    }

    /// <summary>
    /// Cleanup - unsafe because of pointer parameter.
    /// </summary>
    public static unsafe void Cleanup(int* ptr)
    {
        RawMemory.RawDealloc(ptr);
    }

    /// <summary>
    /// Cleanup with safe parameter - no unsafe needed by caller.
    /// </summary>
    public static void CleanupSafe(IntPtr ptr)
    {
        Marshal.FreeHGlobal(ptr);
    }
}
