// C# Memory Safety Application (Future Conventions)
//
// Demonstrates CROSS-MODULE propagation under MemorySafetyRules.
//
// KEY CHANGES from current C#:
// 1. `unsafe` modifier on methods propagates to callers (like Rust)
// 2. Unsafe.As and other BCL APIs will require unsafe context
// 3. Unsafe blocks should be MINIMAL - only the dangerous operation
//
// The goal: Make unsafe code visually distinct and auditable.
// "Keep unsafe blocks as small as reasonably possible."
//
// See: https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/

using System;
using MemoryLib;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== C# Memory Safety Demo (Future Conventions) ===\n");

        DemonstrateCrossModulePropagation();
        DemonstrateCrossModuleSuppression();
        DemonstratePropagationChain();
        DemonstrateUnsafeAsApi();
        DemonstrateSpan();
        PrintSummary();
    }

    /// <summary>
    /// Demonstrates CROSS-MODULE PROPAGATION.
    ///
    /// FUTURE: `unsafe` modifier on methods (not just pointer types)
    /// requires callers to use unsafe context.
    ///
    /// CURRENT: Only pointer types in signatures propagate unsafety.
    /// </summary>
    static void DemonstrateCrossModulePropagation()
    {
        Console.WriteLine("--- Cross-Module Propagation ---");
        Console.WriteLine("Methods with pointer types require unsafe context.\n");

        // CURRENT + FUTURE: Pointer types require unsafe context
        // FUTURE ADDITION: `unsafe` modifier on methods also requires it
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
    ///
    /// SafeBuffer has NO `unsafe` modifier on its public API.
    /// We can use it without any unsafe blocks.
    ///
    /// This is the RECOMMENDED pattern: wrap unsafe internals
    /// in a safe public API.
    /// </summary>
    static void DemonstrateCrossModuleSuppression()
    {
        Console.WriteLine("--- Cross-Module Suppression ---");
        Console.WriteLine("SafeBuffer hides pointers - no unsafe needed.\n");

        // No unsafe blocks anywhere in this code!
        using (var buffer = new SafeBuffer(5))
        {
            buffer[0] = 100;
            buffer[1] = 200;
            buffer[2] = 300;

            Console.WriteLine($"buffer[0] = {buffer[0]}");
            Console.WriteLine($"buffer[1] = {buffer[1]}");
            Console.WriteLine($"buffer[2] = {buffer[2]}");

            // Safe error handling - no undefined behavior
            Console.WriteLine($"buffer.TryGet(100, out _) = {buffer.TryGet(100, out _)}");
            Console.WriteLine($"buffer.TrySet(100, 1) = {buffer.TrySet(100, 1)}");

            // Span provides safe access to underlying memory
            Span<int> span = buffer.AsSpan();
            Console.WriteLine($"span[0] = {span[0]}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates propagation chains across module boundaries.
    /// </summary>
    static void DemonstratePropagationChain()
    {
        Console.WriteLine("--- Propagation Chain (Cross-Module) ---");
        Console.WriteLine("Shows how `unsafe` modifier propagates.\n");

        // Level3Propagate returns int* - requires unsafe
        // FUTURE: Would also require unsafe due to method modifier
        unsafe
        {
            int* ptr = PropagationChain.Level3Propagate();
            Console.WriteLine("Level3Propagate() returned pointer (unsafe call)");
            PropagationChain.Cleanup(ptr);
        }

        // Level3Suppress returns IntPtr - no unsafe needed
        // The library contained the unsafety internally
        IntPtr safePtr = PropagationChain.Level3Suppress();
        Console.WriteLine("Level3Suppress() returned IntPtr (safe call)");
        PropagationChain.CleanupSafe(safePtr);

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Unsafe.As under future conventions.
    ///
    /// CRITICAL: In .NET 11+ with MemorySafetyRules, Unsafe.As
    /// and other Unsafe.* methods will require unsafe context!
    /// </summary>
    static void DemonstrateUnsafeAsApi()
    {
        Console.WriteLine("\n");
        UnsafeAsExample.RunAllDemonstrations();
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Span as the safe alternative.
    ///
    /// This is the RECOMMENDED approach for memory manipulation
    /// under future conventions.
    /// </summary>
    static void DemonstrateSpan()
    {
        Console.WriteLine("\n");
        SpanExample.RunAllDemonstrations();
        Console.WriteLine();
    }

    static void PrintSummary()
    {
        Console.WriteLine("--- Summary: Future C# Conventions ---");
        Console.WriteLine();
        Console.WriteLine("CURRENT MODEL:");
        Console.WriteLine("- Unsafety propagates via POINTER TYPES only");
        Console.WriteLine("- Unsafe.As doesn't require unsafe (safety gap!)");
        Console.WriteLine();
        Console.WriteLine("FUTURE MODEL (MemorySafetyRules):");
        Console.WriteLine("- `unsafe` modifier on methods propagates to callers");
        Console.WriteLine("- Unsafe.As and BCL APIs will require unsafe context");
        Console.WriteLine("- P/Invoke methods will require unsafe context");
        Console.WriteLine();
        Console.WriteLine("CONVENTIONS:");
        Console.WriteLine("- Keep unsafe blocks MINIMAL");
        Console.WriteLine("- Document safety requirements on unsafe members");
        Console.WriteLine("- Wrap unsafe internals in safe public APIs");
        Console.WriteLine("- Prefer Span<T> over pointers when possible");
        Console.WriteLine();
        Console.WriteLine("PROJECT FLAGS:");
        Console.WriteLine("- <AllowUnsafeBlocks>true</AllowUnsafeBlocks>");
        Console.WriteLine("- <MemorySafetyRules>preview</MemorySafetyRules>");
    }
}
