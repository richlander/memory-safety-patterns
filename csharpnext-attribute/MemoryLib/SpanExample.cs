// C# Memory Safety Library - Span<T> Example (Attribute-Based Model)
//
// Span<T> is the SAFE alternative - no `unsafe` and no [RequiresUnsafe] needed.
//
// In the attribute model, Span becomes even more attractive:
// - No pointer types (no `unsafe` keyword needed)
// - No [RequiresUnsafe] attributes (semantically safe)
// - Bounds-checked access prevents memory safety violations
// - Zero-copy slicing for performance

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryLib;

/// <summary>
/// Demonstrates Span&lt;T&gt; as the fully safe alternative.
///
/// In the ATTRIBUTE MODEL, this is especially clean:
/// - No `unsafe` keyword (no pointers)
/// - No [RequiresUnsafe] (no semantic unsafety)
/// - Just safe, high-performance code
/// </summary>
public static class SpanExample
{
    /// <summary>
    /// Basic Span creation - fully safe, no annotations needed.
    /// </summary>
    public static void DemonstrateArraySpan()
    {
        Console.WriteLine("--- Span from Array (Fully Safe) ---");

        int[] array = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        // No unsafe, no [RequiresUnsafe] - just safe code
        Span<int> fullSpan = array.AsSpan();
        Console.WriteLine($"Full span length: {fullSpan.Length}");

        Span<int> slice = array.AsSpan(2, 5);
        Console.WriteLine($"Slice [2..7]: [{string.Join(", ", slice.ToArray())}]");

        slice[0] = 100;
        Console.WriteLine($"After slice[0] = 100, array[2] = {array[2]}");

        Console.WriteLine();
    }

    /// <summary>
    /// Span over stack - safe without unsafe keyword.
    /// </summary>
    public static void DemonstrateStackalloc()
    {
        Console.WriteLine("--- Span from stackalloc (Safe) ---");

        // stackalloc with Span is safe - no unsafe needed
        Span<int> stackSpan = stackalloc int[5];

        for (int i = 0; i < stackSpan.Length; i++)
        {
            stackSpan[i] = i * 10;
        }

        Console.WriteLine($"Stack span: [{string.Join(", ", stackSpan.ToArray())}]");

        Console.WriteLine();
    }

    /// <summary>
    /// Span over native memory - minimal unsafe for construction.
    /// </summary>
    /// <remarks>
    /// In the attribute model, the unsafe block:
    /// - Enables pointer operations (current meaning)
    /// - Acknowledges any [RequiresUnsafe] (new meaning)
    ///
    /// After construction, Span access is fully safe.
    /// </remarks>
    public static void DemonstrateNativeMemory()
    {
        Console.WriteLine("--- Span from Native Memory (Minimal Unsafe) ---");

        IntPtr ptr = Marshal.AllocHGlobal(5 * sizeof(int));

        try
        {
            // Unsafe block for pointer operation only
            Span<int> nativeSpan;
            unsafe
            {
                nativeSpan = new Span<int>((void*)ptr, 5);
            }

            // All subsequent access is safe - no unsafe needed
            for (int i = 0; i < nativeSpan.Length; i++)
            {
                nativeSpan[i] = i + 1;
            }

            Console.WriteLine($"Native span: [{string.Join(", ", nativeSpan.ToArray())}]");
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Zero-copy slicing - all safe operations.
    /// </summary>
    public static void DemonstrateSlicing()
    {
        Console.WriteLine("--- Zero-Copy Slicing (Safe) ---");

        int[] data = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        Span<int> span = data;

        // All slicing is safe
        Span<int> first = span[..3];
        Span<int> middle = span[3..7];
        Span<int> last = span[7..];

        Console.WriteLine($"First:  [{string.Join(", ", first.ToArray())}]");
        Console.WriteLine($"Middle: [{string.Join(", ", middle.ToArray())}]");
        Console.WriteLine($"Last:   [{string.Join(", ", last.ToArray())}]");

        Console.WriteLine();
    }

    /// <summary>
    /// Function parameters - safe and efficient.
    /// </summary>
    public static void DemonstrateFunctionParameters()
    {
        Console.WriteLine("--- Span as Function Parameter (Safe) ---");

        int[] data = [5, 3, 8, 1, 9, 2, 7, 4, 6, 0];

        Console.WriteLine($"Original: [{string.Join(", ", data)}]");
        Console.WriteLine($"Sum: {Sum(data)}");

        BubbleSort(data.AsSpan(2, 5));
        Console.WriteLine($"After sorting [2..7]: [{string.Join(", ", data)}]");

        Console.WriteLine();
    }

    /// <summary>
    /// ReadOnlySpan - immutable views.
    /// </summary>
    public static void DemonstrateReadOnlySpan()
    {
        Console.WriteLine("--- ReadOnlySpan (Safe, Immutable) ---");

        int[] data = [1, 2, 3, 4, 5];
        ReadOnlySpan<int> roSpan = data;

        Console.WriteLine($"ReadOnlySpan[2] = {roSpan[2]}");

        ReadOnlySpan<char> chars = "Hello, World!".AsSpan();
        ReadOnlySpan<char> word = chars[7..12];
        Console.WriteLine($"Sliced string: {word.ToString()}");

        Console.WriteLine();
    }

    /// <summary>
    /// Contrast: Three levels of safety in the attribute model.
    /// </summary>
    public static void DemonstrateThreeLevels()
    {
        Console.WriteLine("--- Three Levels of Safety (Attribute Model) ---");
        Console.WriteLine();

        Console.WriteLine("1. FULLY SAFE (no annotations):");
        Console.WriteLine("   - Span<T>, arrays, safe APIs");
        Console.WriteLine("   - No `unsafe`, no [RequiresUnsafe]");
        Console.WriteLine("   - Example: span[i] = value;");
        Console.WriteLine();

        Console.WriteLine("2. POINTER UNSAFE (`unsafe` keyword):");
        Console.WriteLine("   - Uses pointer types");
        Console.WriteLine("   - Requires AllowUnsafeBlocks in project");
        Console.WriteLine("   - Example: int* ptr = &value;");
        Console.WriteLine();

        Console.WriteLine("3. SEMANTIC UNSAFE ([RequiresUnsafe]):");
        Console.WriteLine("   - No pointers, but violates safety contracts");
        Console.WriteLine("   - Example: Unsafe.As, uninitialized memory");
        Console.WriteLine("   - Message explains WHY it's unsafe");
        Console.WriteLine();

        Console.WriteLine("4. BOTH (`unsafe` + [RequiresUnsafe]):");
        Console.WriteLine("   - Uses pointers AND has semantic contracts");
        Console.WriteLine("   - Example: raw memory allocation");
        Console.WriteLine();
    }

    // Helper functions - all safe, no annotations

    private static int Sum(ReadOnlySpan<int> span)
    {
        int sum = 0;
        foreach (int value in span)
        {
            sum += value;
        }
        return sum;
    }

    private static void BubbleSort(Span<int> span)
    {
        for (int i = 0; i < span.Length - 1; i++)
        {
            for (int j = 0; j < span.Length - i - 1; j++)
            {
                if (span[j] > span[j + 1])
                {
                    (span[j], span[j + 1]) = (span[j + 1], span[j]);
                }
            }
        }
    }

    /// <summary>
    /// Runs all demonstrations.
    /// </summary>
    public static void RunAllDemonstrations()
    {
        Console.WriteLine("=== C# Span<T> Examples (Attribute Model) ===");
        Console.WriteLine("Span is fully safe - no `unsafe` or [RequiresUnsafe] needed.\n");

        DemonstrateArraySpan();
        DemonstrateStackalloc();
        DemonstrateNativeMemory();
        DemonstrateSlicing();
        DemonstrateFunctionParameters();
        DemonstrateReadOnlySpan();
        DemonstrateThreeLevels();

        Console.WriteLine("--- Summary ---");
        Console.WriteLine("Attribute model provides clear categorization:");
        Console.WriteLine("- Safe code: No annotations");
        Console.WriteLine("- Pointer code: `unsafe` keyword");
        Console.WriteLine("- Semantic unsafety: [RequiresUnsafe]");
        Console.WriteLine("- Span<T> is in the first category - fully safe!");
    }
}
