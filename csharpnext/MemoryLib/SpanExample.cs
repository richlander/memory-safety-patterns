// C# Memory Safety Library - Span<T> Example (Future Conventions)
//
// Span<T> is the RECOMMENDED safe alternative to pointer manipulation.
// It provides bounds-checked access with near-zero overhead.
//
// Under MemorySafetyRules, Span becomes even more important:
// - Unsafe.* methods require unsafe context
// - Pointer operations require unsafe context
// - Span provides the SAFE path for high-performance code
//
// Key properties of Span:
// - Bounds checking at runtime (safe)
// - Zero-copy slicing
// - No heap allocation (ref struct)
// - Cannot escape to heap (lifetime-safe by construction)

using System;
using System.Runtime.InteropServices;

namespace MemoryLib;

/// <summary>
/// Demonstrates Span&lt;T&gt; as the safe, high-performance alternative.
///
/// Under future conventions, this is the RECOMMENDED approach for
/// working with contiguous memory. No unsafe blocks needed!
/// </summary>
public static class SpanExample
{
    /// <summary>
    /// Basic Span creation from an array - safe, no copies.
    /// </summary>
    /// <remarks>
    /// NO unsafe blocks needed. This is the recommended approach.
    /// </remarks>
    public static void DemonstrateArraySpan()
    {
        Console.WriteLine("--- Span from Array (Safe) ---");

        int[] array = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        // Zero-copy view - no unsafe needed
        Span<int> fullSpan = array.AsSpan();
        Console.WriteLine($"Full span length: {fullSpan.Length}");

        // Slicing - no unsafe needed
        Span<int> slice = array.AsSpan(2, 5);
        Console.WriteLine($"Slice [2..7]: [{string.Join(", ", slice.ToArray())}]");

        // Modification through span - safe, bounds-checked
        slice[0] = 100;
        Console.WriteLine($"After slice[0] = 100, array[2] = {array[2]}");

        // Bounds checking prevents buffer overflows
        // slice[10] = 0; // Would throw IndexOutOfRangeException

        Console.WriteLine();
    }

    /// <summary>
    /// Span over stack-allocated memory - safe access to stack.
    /// </summary>
    /// <remarks>
    /// stackalloc with Span is SAFE - no unsafe block needed.
    /// This is the recommended way to use stack allocation.
    /// </remarks>
    public static void DemonstrateStackalloc()
    {
        Console.WriteLine("--- Span from stackalloc (Safe) ---");

        // Safe stack allocation - no unsafe needed!
        Span<int> stackSpan = stackalloc int[5];

        for (int i = 0; i < stackSpan.Length; i++)
        {
            stackSpan[i] = i * 10;
        }

        Console.WriteLine($"Stack span: [{string.Join(", ", stackSpan.ToArray())}]");
        Console.WriteLine($"stackSpan[2] = {stackSpan[2]}");

        Console.WriteLine();
    }

    /// <summary>
    /// Span over native memory - safe view of unsafe allocation.
    /// </summary>
    /// <remarks>
    /// The unsafe block is MINIMAL - only the Span construction.
    /// After construction, all access is safe and bounds-checked.
    ///
    /// FUTURE: This pattern becomes more important as Unsafe.* APIs
    /// require unsafe context.
    /// </remarks>
    public static void DemonstrateNativeMemory()
    {
        Console.WriteLine("--- Span from Native Memory (Minimal Unsafe) ---");

        IntPtr ptr = Marshal.AllocHGlobal(5 * sizeof(int));

        try
        {
            // MINIMAL UNSAFE BLOCK: Only Span construction
            Span<int> nativeSpan;
            unsafe
            {
                nativeSpan = new Span<int>((void*)ptr, 5);
            }
            // After this, all access is SAFE

            // Safe, bounds-checked operations
            for (int i = 0; i < nativeSpan.Length; i++)
            {
                nativeSpan[i] = i + 1;
            }

            Console.WriteLine($"Native span: [{string.Join(", ", nativeSpan.ToArray())}]");
            Console.WriteLine($"nativeSpan[3] = {nativeSpan[3]}");
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Zero-copy slicing and processing - all safe operations.
    /// </summary>
    public static void DemonstrateSlicing()
    {
        Console.WriteLine("--- Zero-Copy Slicing (Safe) ---");

        int[] data = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        Span<int> span = data;

        // All slicing is safe - no unsafe needed
        Span<int> first = span[..3];
        Span<int> middle = span[3..7];
        Span<int> last = span[7..];

        Console.WriteLine($"First:  [{string.Join(", ", first.ToArray())}]");
        Console.WriteLine($"Middle: [{string.Join(", ", middle.ToArray())}]");
        Console.WriteLine($"Last:   [{string.Join(", ", last.ToArray())}]");

        middle[0] = 999;
        Console.WriteLine($"After middle[0] = 999, data[3] = {data[3]}");

        Console.WriteLine();
    }

    /// <summary>
    /// Passing spans to functions - efficient, safe parameter passing.
    /// </summary>
    public static void DemonstrateFunctionParameters()
    {
        Console.WriteLine("--- Span as Function Parameter (Safe) ---");

        int[] data = [5, 3, 8, 1, 9, 2, 7, 4, 6, 0];

        Console.WriteLine($"Original: [{string.Join(", ", data)}]");
        Console.WriteLine($"Sum: {Sum(data)}");

        // Sort a slice - safe operation
        BubbleSort(data.AsSpan(2, 5));
        Console.WriteLine($"After sorting [2..7]: [{string.Join(", ", data)}]");

        // Fill a region - safe operation
        Fill(data.AsSpan(0, 3), 42);
        Console.WriteLine($"After filling [0..3] with 42: [{string.Join(", ", data)}]");

        Console.WriteLine();
    }

    /// <summary>
    /// ReadOnlySpan for immutable views.
    /// </summary>
    public static void DemonstrateReadOnlySpan()
    {
        Console.WriteLine("--- ReadOnlySpan (Safe, Immutable) ---");

        int[] data = [1, 2, 3, 4, 5];

        // ReadOnlySpan prevents modification - compile-time safety
        ReadOnlySpan<int> roSpan = data;

        Console.WriteLine($"ReadOnlySpan[2] = {roSpan[2]}");
        // roSpan[2] = 10; // Compile error - enforced at compile time

        // String slicing is safe and zero-copy
        ReadOnlySpan<char> chars = "Hello, World!".AsSpan();
        ReadOnlySpan<char> word = chars[7..12];
        Console.WriteLine($"Sliced string: {word.ToString()}");

        Console.WriteLine();
    }

    /// <summary>
    /// Contrast: Span vs Pointers under future conventions.
    /// </summary>
    public static void DemonstrateContrastWithPointers()
    {
        Console.WriteLine("--- Contrast: Span (Safe) vs Pointers (Unsafe) ---");

        int[] data = [10, 20, 30, 40, 50];

        // UNSAFE: Pointer approach - requires unsafe, no bounds checking
        unsafe
        {
            fixed (int* ptr = data)
            {
                Console.WriteLine($"Pointer access ptr[2]: {ptr[2]}");
                // ptr[100] would compile - undefined behavior!
            }
        }

        // SAFE: Span approach - no unsafe needed, bounds checked
        Span<int> span = data;
        Console.WriteLine($"Span access span[2]: {span[2]}");
        // span[100] throws IndexOutOfRangeException

        Console.WriteLine();
        Console.WriteLine("FUTURE RECOMMENDATION:");
        Console.WriteLine("- Use Span<T> for safe, high-performance memory access");
        Console.WriteLine("- Avoid pointers and Unsafe.* when possible");
        Console.WriteLine("- When unsafe is needed, keep blocks MINIMAL");

        Console.WriteLine();
    }

    // Helper functions - all safe, no unsafe blocks

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

    private static void Fill(Span<int> span, int value)
    {
        span.Fill(value);
    }

    /// <summary>
    /// Runs all Span demonstrations.
    /// </summary>
    public static void RunAllDemonstrations()
    {
        Console.WriteLine("=== C# Span<T> Examples (Future Conventions) ===");
        Console.WriteLine("Span is the RECOMMENDED safe alternative to pointers.\n");

        DemonstrateArraySpan();
        DemonstrateStackalloc();
        DemonstrateNativeMemory();
        DemonstrateSlicing();
        DemonstrateFunctionParameters();
        DemonstrateReadOnlySpan();
        DemonstrateContrastWithPointers();

        Console.WriteLine("--- Summary ---");
        Console.WriteLine("Under MemorySafetyRules:");
        Console.WriteLine("- Unsafe.* methods require unsafe context");
        Console.WriteLine("- Pointer operations require unsafe context");
        Console.WriteLine("- Span<T> provides the SAFE path");
        Console.WriteLine("- Use minimal unsafe blocks when unavoidable");
        Console.WriteLine("- Span is bounds-checked with near-zero overhead");
    }
}
