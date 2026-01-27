// C# Memory Safety Library - Unsafe API
//
// This file demonstrates CROSS-FUNCTION propagation (within the same assembly)
// and sets up CROSS-MODULE propagation (to consuming assemblies).
//
// C# propagation rules:
// - CROSS-FUNCTION: Calling a method with pointers requires unsafe context
// - CROSS-MODULE: Same rule - pointers in signature require unsafe in caller
//
// Unlike Rust, C# doesn't have an "unsafe method" marker in the signature.
// Instead, unsafety propagates through POINTER TYPES in parameters/returns.

using System;
using System.Runtime.InteropServices;

namespace MemoryLib;

/// <summary>
/// Internal low-level unsafe operations.
/// Demonstrates CROSS-FUNCTION propagation within the assembly.
/// </summary>
internal static class RawMemory
{
    /// <summary>
    /// Low-level allocation. Called by other methods in this assembly.
    /// CROSS-FUNCTION: Callers within this assembly must use unsafe context.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS:
    /// - count must be > 0 (not validated here for performance)
    /// - Caller must eventually call RawDealloc with the returned pointer
    /// - Caller must not use the pointer after RawDealloc
    /// - Memory is UNINITIALIZED - caller must write before reading
    /// </remarks>
    internal static unsafe int* RawAlloc(int count)
    {
        return (int*)Marshal.AllocHGlobal(count * sizeof(int));
    }

    /// <summary>
    /// Low-level deallocation.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS:
    /// - ptr must have been returned by RawAlloc
    /// - ptr must not have been previously freed
    /// - Caller must not use ptr after this call
    /// </remarks>
    internal static unsafe void RawDealloc(int* ptr)
    {
        Marshal.FreeHGlobal((IntPtr)ptr);
    }

    /// <summary>
    /// Mid-level function that PROPAGATES unsafety.
    /// Still uses pointers, so callers need unsafe context.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS: Same as RawAlloc - memory is uninitialized.
    /// </remarks>
    internal static unsafe int* MidLevelAllocUninit(int count)
    {
        // CROSS-FUNCTION: Even within same assembly, we're in unsafe context
        // because we're working with pointers
        return RawAlloc(count);
    }

    /// <summary>
    /// Mid-level function that SUPPRESSES unsafety for the return value.
    /// Returns IntPtr (safe) instead of int* (unsafe).
    /// Callers don't need unsafe to receive the return value.
    /// </summary>
    /// <remarks>
    /// SAFETY DISCHARGE for RawAlloc obligations:
    /// - count > 0: Assumed by caller (could add validation)
    /// - Memory initialized: Zero-filled in loop below
    /// - Deallocation: Caller still responsible (not fully discharged!)
    ///
    /// CALLER OBLIGATIONS (remaining):
    /// - Must call Marshal.FreeHGlobal on the returned IntPtr
    /// - Must not use IntPtr after freeing
    /// </remarks>
    internal static IntPtr MidLevelAllocAsSafeHandle(int count)
    {
        unsafe
        {
            int* ptr = RawAlloc(count);

            // SAFETY DISCHARGE: Initialize to zero (no uninitialized reads)
            for (int i = 0; i < count; i++)
            {
                ptr[i] = 0;
            }

            return (IntPtr)ptr;
        }
    }
}

/// <summary>
/// PUBLIC API that PROPAGATES unsafety to consumers (CROSS-MODULE).
///
/// Methods with pointer parameters or return types force callers
/// (even in other assemblies) to use unsafe contexts.
/// </summary>
public static class UnsafeApi
{
    /// <summary>
    /// Allocates a buffer. CROSS-MODULE propagation via pointer return type.
    /// Callers in other assemblies MUST use unsafe context.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS:
    /// - Must call Free() exactly once with the returned pointer
    /// - Must not use the pointer after calling Free()
    /// - Must not read/write beyond [0, count) bounds
    /// - Memory IS zero-initialized (safe to read immediately)
    ///
    /// Example of correct usage:
    /// <code>
    /// unsafe {
    ///     int* ptr = UnsafeApi.Alloc(10);
    ///     try {
    ///         UnsafeApi.Write(ptr, 0, 42);
    ///         int value = UnsafeApi.Read(ptr, 0);
    ///     } finally {
    ///         UnsafeApi.Free(ptr);
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public static unsafe int* Alloc(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        int* ptr = RawMemory.RawAlloc(count);

        // SAFETY DISCHARGE for RawAlloc's "uninitialized memory" obligation:
        // Initialize to zero so callers can safely read
        for (int i = 0; i < count; i++)
        {
            ptr[i] = 0;
        }

        return ptr;
    }

    /// <summary>
    /// Frees memory. Pointer parameter forces unsafe context in caller.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS:
    /// - ptr must have been returned by Alloc()
    /// - ptr must not have been previously freed
    /// - Caller must not use ptr after this call returns
    /// </remarks>
    public static unsafe void Free(int* ptr)
    {
        RawMemory.RawDealloc(ptr);
    }

    /// <summary>
    /// Reads at offset. Pointer parameter = unsafe propagates.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS:
    /// - ptr must be valid (from Alloc, not yet freed)
    /// - offset must be in bounds [0, allocated_count)
    /// </remarks>
    public static unsafe int Read(int* ptr, int offset)
    {
        return ptr[offset];
    }

    /// <summary>
    /// Writes at offset. Pointer parameter = unsafe propagates.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS:
    /// - ptr must be valid (from Alloc, not yet freed)
    /// - offset must be in bounds [0, allocated_count)
    /// </remarks>
    public static unsafe void Write(int* ptr, int offset, int value)
    {
        ptr[offset] = value;
    }
}

/// <summary>
/// Demonstrates PROPAGATION CHAINS within and across modules.
/// </summary>
public static class PropagationChain
{
    /// <summary>
    /// Level 1: Uses pointers, propagates to caller.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS: Same as RawMemory.RawAlloc.
    /// </remarks>
    private static unsafe int* Level1Unsafe()
    {
        return RawMemory.RawAlloc(1);
    }

    /// <summary>
    /// Level 2: Calls Level1, propagates (still uses pointers).
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS: Same as Level1Unsafe (memory uninitialized, must free).
    /// </remarks>
    private static unsafe int* Level2Unsafe()
    {
        return Level1Unsafe();
    }

    /// <summary>
    /// Level 3 PROPAGATE: Public, uses pointers.
    /// CROSS-MODULE: External callers must use unsafe.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS:
    /// - Memory is UNINITIALIZED - must write before reading
    /// - Must call Cleanup() exactly once
    /// - Must not use pointer after Cleanup()
    /// </remarks>
    public static unsafe int* Level3Propagate()
    {
        return Level2Unsafe();
    }

    /// <summary>
    /// Level 3 SUPPRESS: Public, returns IntPtr (safe type).
    /// CROSS-MODULE: External callers do NOT need unsafe.
    /// </summary>
    /// <remarks>
    /// SAFETY DISCHARGE: Converting to IntPtr suppresses the pointer-type
    /// propagation, but does NOT discharge all obligations.
    ///
    /// CALLER OBLIGATIONS (remaining):
    /// - Memory is UNINITIALIZED - must initialize before reading
    /// - Must call CleanupSafe() exactly once
    /// - Must not use IntPtr after CleanupSafe()
    ///
    /// NOTE: This demonstrates PARTIAL suppression - the syntax is safe
    /// but the semantic obligations remain. A FULL suppression would
    /// also initialize the memory and manage lifetime internally.
    /// </remarks>
    public static IntPtr Level3Suppress()
    {
        unsafe
        {
            return (IntPtr)Level2Unsafe();
        }
    }

    /// <summary>
    /// Cleanup - pointer parameter means unsafe propagates.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS:
    /// - ptr must be from Level3Propagate()
    /// - ptr must not have been previously freed
    /// </remarks>
    public static unsafe void Cleanup(int* ptr)
    {
        RawMemory.RawDealloc(ptr);
    }

    /// <summary>
    /// Cleanup with safe parameter - no unsafe needed by caller.
    /// </summary>
    /// <remarks>
    /// CALLER OBLIGATIONS:
    /// - ptr must be from Level3Suppress()
    /// - ptr must not have been previously freed
    /// </remarks>
    public static void CleanupSafe(IntPtr ptr)
    {
        Marshal.FreeHGlobal(ptr);
    }
}
