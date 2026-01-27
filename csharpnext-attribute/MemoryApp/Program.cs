// C# Memory Safety Application (Attribute-Based Model)
//
// Demonstrates the ATTRIBUTE-BASED model for unsafe propagation.
//
// KEY DIFFERENCE from the `unsafe` keyword model:
// - `unsafe` keyword: Still means "I'm using pointer types" (unchanged)
// - [RequiresUnsafe]: New attribute meaning "semantically unsafe"
//
// This separation provides:
// - Better backward compatibility
// - Clearer intent (WHY is it unsafe?)
// - Fine-grained control (pointers vs semantic unsafety)
//
// See: https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/

using System;
using System.Runtime.CompilerServices;
using MemoryLib;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== C# Memory Safety Demo (Attribute Model) ===\n");

        DemonstrateCrossModulePropagation();
        DemonstrateCrossModuleSuppression();
        DemonstratePropagationChain();
        DemonstrateSemanticUnsafety();
        DemonstrateUnsafeAsApi();
        DemonstrateSpan();
        PrintSummary();
    }

    /// <summary>
    /// Demonstrates CROSS-MODULE PROPAGATION.
    ///
    /// In the attribute model, we need unsafe blocks for TWO reasons:
    /// 1. Pointer types in signatures (current behavior)
    /// 2. [RequiresUnsafe] attributes (new behavior)
    /// </summary>
    static void DemonstrateCrossModulePropagation()
    {
        Console.WriteLine("--- Cross-Module Propagation ---");
        Console.WriteLine("Methods with pointers AND [RequiresUnsafe] require unsafe.\n");

        // UnsafeApi methods have BOTH:
        // - Pointer types (needs unsafe for pointers)
        // - [RequiresUnsafe] (needs unsafe for semantic acknowledgment)
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
    /// SafeBuffer has:
    /// - No pointer types in public API
    /// - No [RequiresUnsafe] on public members
    ///
    /// So callers need NO unsafe acknowledgment.
    /// </summary>
    static void DemonstrateCrossModuleSuppression()
    {
        Console.WriteLine("--- Cross-Module Suppression ---");
        Console.WriteLine("SafeBuffer has no pointers or [RequiresUnsafe] - fully safe.\n");

        // No unsafe needed - SafeBuffer suppresses everything internally
        using (var buffer = new SafeBuffer(5))
        {
            buffer[0] = 100;
            buffer[1] = 200;
            buffer[2] = 300;

            Console.WriteLine($"buffer[0] = {buffer[0]}");
            Console.WriteLine($"buffer[1] = {buffer[1]}");
            Console.WriteLine($"buffer[2] = {buffer[2]}");

            Console.WriteLine($"buffer.TryGet(100, out _) = {buffer.TryGet(100, out _)}");

            Span<int> span = buffer.AsSpan();
            Console.WriteLine($"span[0] = {span[0]}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates propagation chains.
    /// </summary>
    static void DemonstratePropagationChain()
    {
        Console.WriteLine("--- Propagation Chain ---");
        Console.WriteLine("Shows [RequiresUnsafe] propagation through call chains.\n");

        // Level3Propagate has both pointer return AND [RequiresUnsafe]
        unsafe
        {
            int* ptr = PropagationChain.Level3Propagate();
            Console.WriteLine("Level3Propagate() - needs unsafe (pointers + [RequiresUnsafe])");
            PropagationChain.Cleanup(ptr);
        }

        // Level3Suppress has neither - fully safe
        IntPtr safePtr = PropagationChain.Level3Suppress();
        Console.WriteLine("Level3Suppress() - no unsafe needed (suppressed internally)");
        PropagationChain.CleanupSafe(safePtr);

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates [RequiresUnsafe] WITHOUT pointers.
    ///
    /// This is the KEY BENEFIT of the attribute model:
    /// Methods can be semantically unsafe without using pointers.
    /// </summary>
    static void DemonstrateSemanticUnsafety()
    {
        Console.WriteLine("--- Semantic Unsafety (No Pointers) ---");
        Console.WriteLine("[RequiresUnsafe] can mark methods without pointer types.\n");

        // AllocUninitializedSpan has [RequiresUnsafe] but NO pointers
        // It's unsafe because reading before writing is UB
        unsafe
        {
            Span<int> uninit = SemanticUnsafetyExamples.AllocUninitializedSpan(5);
            // Must write before reading!
            for (int i = 0; i < uninit.Length; i++)
            {
                uninit[i] = i * 10;
            }
            Console.WriteLine($"Initialized uninitialized span: [{string.Join(", ", uninit.ToArray())}]");
        }

        // AllocZeroedSpan has NO [RequiresUnsafe] - safe to use
        Span<int> zeroed = SemanticUnsafetyExamples.AllocZeroedSpan(5);
        Console.WriteLine($"Zeroed span (safe): [{string.Join(", ", zeroed.ToArray())}]");

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Unsafe.As with the attribute model.
    /// </summary>
    static void DemonstrateUnsafeAsApi()
    {
        Console.WriteLine("\n");
        UnsafeAsExample.RunAllDemonstrations();
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Span - the fully safe alternative.
    /// </summary>
    static void DemonstrateSpan()
    {
        Console.WriteLine("\n");
        SpanExample.RunAllDemonstrations();
        Console.WriteLine();
    }

    static void PrintSummary()
    {
        Console.WriteLine("--- Summary: Attribute-Based Model ---");
        Console.WriteLine();
        Console.WriteLine("TWO SEPARATE CONCEPTS:");
        Console.WriteLine("1. `unsafe` keyword: Pointer types (unchanged meaning)");
        Console.WriteLine("2. [RequiresUnsafe]: Semantic unsafety (new concept)");
        Console.WriteLine();
        Console.WriteLine("FOUR CATEGORIES OF METHODS:");
        Console.WriteLine("+-----------------+--------------------+--------------------+");
        Console.WriteLine("| Category        | Pointers?          | [RequiresUnsafe]?  |");
        Console.WriteLine("+-----------------+--------------------+--------------------+");
        Console.WriteLine("| Fully Safe      | No                 | No                 |");
        Console.WriteLine("| Pointer Unsafe  | Yes (`unsafe`)     | No                 |");
        Console.WriteLine("| Semantic Unsafe | No                 | Yes                |");
        Console.WriteLine("| Both Unsafe     | Yes (`unsafe`)     | Yes                |");
        Console.WriteLine("+-----------------+--------------------+--------------------+");
        Console.WriteLine();
        Console.WriteLine("CALLER ACKNOWLEDGMENT:");
        Console.WriteLine("- unsafe block acknowledges BOTH pointer ops and [RequiresUnsafe]");
        Console.WriteLine("- [RequiresUnsafe] on caller propagates the requirement");
        Console.WriteLine("- No annotation = safe, no acknowledgment needed");
        Console.WriteLine();
        Console.WriteLine("BENEFITS vs KEYWORD MODEL:");
        Console.WriteLine("- More backward compatible");
        Console.WriteLine("- Clearer intent (message explains WHY)");
        Console.WriteLine("- Finer granularity (separate concepts)");
    }
}
