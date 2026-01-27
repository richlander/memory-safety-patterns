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
    internal static unsafe int* RawAlloc(int count)
    {
        return (int*)Marshal.AllocHGlobal(count * sizeof(int));
    }

    /// <summary>
    /// Low-level deallocation.
    /// </summary>
    internal static unsafe void RawDealloc(int* ptr)
    {
        Marshal.FreeHGlobal((IntPtr)ptr);
    }

    /// <summary>
    /// Mid-level function that PROPAGATES unsafety.
    /// Still uses pointers, so callers need unsafe context.
    /// </summary>
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
    internal static IntPtr MidLevelAllocAsSafeHandle(int count)
    {
        unsafe
        {
            // CROSS-FUNCTION: We use unsafe internally
            int* ptr = RawAlloc(count);
            // Initialize to zero
            for (int i = 0; i < count; i++)
            {
                ptr[i] = 0;
            }
            // Return as IntPtr - callers don't need unsafe for this
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
    public static unsafe int* Alloc(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        // CROSS-FUNCTION call to internal function
        int* ptr = RawMemory.RawAlloc(count);

        // Initialize to zero
        for (int i = 0; i < count; i++)
        {
            ptr[i] = 0;
        }

        return ptr;
    }

    /// <summary>
    /// Frees memory. Pointer parameter forces unsafe context in caller.
    /// </summary>
    public static unsafe void Free(int* ptr)
    {
        RawMemory.RawDealloc(ptr);
    }

    /// <summary>
    /// Reads at offset. Pointer parameter = unsafe propagates.
    /// </summary>
    public static unsafe int Read(int* ptr, int offset)
    {
        return ptr[offset];
    }

    /// <summary>
    /// Writes at offset. Pointer parameter = unsafe propagates.
    /// </summary>
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
    private static unsafe int* Level1Unsafe()
    {
        return RawMemory.RawAlloc(1);
    }

    /// <summary>
    /// Level 2: Calls Level1, propagates (still uses pointers).
    /// </summary>
    private static unsafe int* Level2Unsafe()
    {
        return Level1Unsafe();
    }

    /// <summary>
    /// Level 3 PROPAGATE: Public, uses pointers.
    /// CROSS-MODULE: External callers must use unsafe.
    /// </summary>
    public static unsafe int* Level3Propagate()
    {
        return Level2Unsafe();
    }

    /// <summary>
    /// Level 3 SUPPRESS: Public, returns IntPtr (safe type).
    /// CROSS-MODULE: External callers do NOT need unsafe.
    /// </summary>
    public static IntPtr Level3Suppress()
    {
        unsafe
        {
            // Suppress by converting to safe type
            return (IntPtr)Level2Unsafe();
        }
    }

    /// <summary>
    /// Cleanup - pointer parameter means unsafe propagates.
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
