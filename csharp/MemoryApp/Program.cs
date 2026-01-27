// C# Memory Safety Application
//
// Demonstrates CROSS-MODULE propagation and suppression.
// This assembly consumes MemoryLib and shows how unsafety crosses
// assembly boundaries.
//
// Key insight: In C#, unsafety propagates via POINTER TYPES.
// If a method signature contains pointers, callers need unsafe context.

using System;
using MemoryLib;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== C# Memory Safety Demo ===\n");

        DemonstrateCrossModulePropagation();
        DemonstrateCrossModuleSuppression();
        DemonstratePropagationChain();
        DemonstrateUnsafeAsApi();
        PrintSummary();
    }

    /// <summary>
    /// Demonstrates that Unsafe.As methods don't require unsafe blocks.
    /// Shows that "unsafe" in C# is about syntax (pointers), not type names.
    /// </summary>
    static void DemonstrateUnsafeAsApi()
    {
        Console.WriteLine("\n");
        UnsafeAsExample.RunAllDemonstrations();
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates CROSS-MODULE PROPAGATION.
    /// UnsafeApi methods have pointer types, so we need unsafe context.
    /// </summary>
    static void DemonstrateCrossModulePropagation()
    {
        Console.WriteLine("--- Cross-Module Propagation ---");
        Console.WriteLine("Methods with pointer types require unsafe context.\n");

        // This would NOT compile without unsafe:
        // int* ptr = UnsafeApi.Alloc(5);  // ERROR: Pointers require unsafe

        // We MUST use unsafe context because of pointer types
        unsafe
        {
            int* ptr = UnsafeApi.Alloc(5);

            UnsafeApi.Write(ptr, 0, 100);
            UnsafeApi.Write(ptr, 1, 200);
            UnsafeApi.Write(ptr, 2, 300);

            Console.WriteLine($"UnsafeApi.Read(ptr, 0) = {UnsafeApi.Read(ptr, 0)}");
            Console.WriteLine($"UnsafeApi.Read(ptr, 1) = {UnsafeApi.Read(ptr, 1)}");
            Console.WriteLine($"UnsafeApi.Read(ptr, 2) = {UnsafeApi.Read(ptr, 2)}");

            UnsafeApi.Free(ptr);
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates CROSS-MODULE SUPPRESSION.
    /// SafeBuffer has no pointers in its public API, so no unsafe needed.
    /// </summary>
    static void DemonstrateCrossModuleSuppression()
    {
        Console.WriteLine("--- Cross-Module Suppression ---");
        Console.WriteLine("SafeBuffer hides pointers - no unsafe needed.\n");

        // No unsafe blocks anywhere in this method!
        using (var buffer = new SafeBuffer(5))
        {
            buffer[0] = 100;
            buffer[1] = 200;
            buffer[2] = 300;

            Console.WriteLine($"buffer[0] = {buffer[0]}");
            Console.WriteLine($"buffer[1] = {buffer[1]}");
            Console.WriteLine($"buffer[2] = {buffer[2]}");

            // Safe error handling
            Console.WriteLine($"buffer.TryGet(100, out _) = {buffer.TryGet(100, out _)}");
            Console.WriteLine($"buffer.TrySet(100, 1) = {buffer.TrySet(100, 1)}");

            // Can use Span<T> for efficient access without unsafe
            Span<int> span = buffer.AsSpan();
            Console.WriteLine($"span[0] = {span[0]}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates how propagation chains work across module boundaries.
    /// </summary>
    static void DemonstratePropagationChain()
    {
        Console.WriteLine("--- Propagation Chain (Cross-Module) ---");
        Console.WriteLine("Shows how pointer types affect call chains.\n");

        // Level3Propagate returns int* - requires unsafe
        unsafe
        {
            int* ptr = PropagationChain.Level3Propagate();
            Console.WriteLine("Level3Propagate() returned pointer (unsafe call)");
            PropagationChain.Cleanup(ptr);
        }

        // Level3Suppress returns IntPtr - no unsafe needed
        IntPtr safePtr = PropagationChain.Level3Suppress();
        Console.WriteLine("Level3Suppress() returned IntPtr (safe call)");
        PropagationChain.CleanupSafe(safePtr);

        Console.WriteLine();
    }

    static void PrintSummary()
    {
        Console.WriteLine("--- Summary: C# Propagation ---");
        Console.WriteLine("CROSS-FUNCTION: Pointer types require unsafe context.");
        Console.WriteLine("CROSS-MODULE: Same rule - pointers = unsafe required.");
        Console.WriteLine("MECHANISM: Propagation is via TYPES, not method attributes.");
        Console.WriteLine("SUPPRESSION: Convert pointers to IntPtr or wrap in classes.");
        Console.WriteLine("PROJECT FLAG: AllowUnsafeBlocks must be true in .csproj.");
    }
}
