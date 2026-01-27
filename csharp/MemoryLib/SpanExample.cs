// C# Memory Safety Library - Span<T> Example
//
// Span<T> provides safe, bounds-checked access to contiguous memory.
// It's a non-owning view that can point to:
// - Arrays
// - Stack-allocated memory (stackalloc)
// - Native memory
//
// Key properties:
// - Bounds checking at runtime (safe)
// - Zero-copy slicing
// - No heap allocation (it's a ref struct)
// - Cannot escape to heap (cannot be stored in fields of classes)

using System;
using System.Runtime.InteropServices;

namespace MemoryLib;

/// <summary>
/// Demonstrates Span&lt;T&gt; as a safe abstraction for contiguous memory.
///
/// Span provides memory safety through:
/// 1. Bounds checking on every access
/// 2. Lifetime constraints (ref struct cannot escape)
/// 3. Clear ownership semantics (non-owning view)
/// </summary>
public static class SpanExample
{
    /// <summary>
    /// Basic Span creation from an array - safe, no copies.
    /// </summary>
    public static void DemonstrateArraySpan()
    {
        Console.WriteLine("--- Span from Array ---");

        int[] array = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        // Create a span over the entire array - zero copy
        Span<int> fullSpan = array.AsSpan();
        Console.WriteLine($"Full span length: {fullSpan.Length}");

        // Create a span over a slice - still zero copy
        Span<int> slice = array.AsSpan(2, 5); // elements 3,4,5,6,7
        Console.WriteLine($"Slice [2..7]: [{string.Join(", ", slice.ToArray())}]");

        // Modifications through span affect the original array
        slice[0] = 100;
        Console.WriteLine($"After slice[0] = 100, array[2] = {array[2]}");

        // Bounds checking - this would throw IndexOutOfRangeException:
        // slice[10] = 0; // Runtime error, not undefined behavior

        Console.WriteLine();
    }

    /// <summary>
    /// Span over stack-allocated memory - safe access to stack.
    /// </summary>
    public static void DemonstrateStackalloc()
    {
        Console.WriteLine("--- Span from stackalloc ---");

        // stackalloc with Span - no unsafe needed!
        Span<int> stackSpan = stackalloc int[5];

        // Initialize
        for (int i = 0; i < stackSpan.Length; i++)
        {
            stackSpan[i] = i * 10;
        }

        Console.WriteLine($"Stack span: [{string.Join(", ", stackSpan.ToArray())}]");

        // Bounds checked - safe despite being stack memory
        Console.WriteLine($"stackSpan[2] = {stackSpan[2]}");

        Console.WriteLine();
    }

    /// <summary>
    /// Span over native memory - safe view of unsafe allocation.
    /// </summary>
    public static void DemonstrateNativeMemory()
    {
        Console.WriteLine("--- Span from Native Memory ---");

        // Allocate native memory
        IntPtr ptr = Marshal.AllocHGlobal(5 * sizeof(int));

        try
        {
            // Create a safe Span view over the native memory
            // This single unsafe block creates the safe abstraction
            Span<int> nativeSpan;
            unsafe
            {
                nativeSpan = new Span<int>((void*)ptr, 5);
            }

            // Now all access is safe and bounds-checked
            for (int i = 0; i < nativeSpan.Length; i++)
            {
                nativeSpan[i] = i + 1;
            }

            Console.WriteLine($"Native span: [{string.Join(", ", nativeSpan.ToArray())}]");
            Console.WriteLine($"nativeSpan[3] = {nativeSpan[3]}");

            // This would throw, not cause undefined behavior:
            // nativeSpan[100] = 0;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Zero-copy slicing and processing.
    /// </summary>
    public static void DemonstrateSlicing()
    {
        Console.WriteLine("--- Zero-Copy Slicing ---");

        int[] data = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        Span<int> span = data;

        // Slice syntax - no copies made
        Span<int> first = span[..3];    // [0, 1, 2]
        Span<int> middle = span[3..7];  // [3, 4, 5, 6]
        Span<int> last = span[7..];     // [7, 8, 9]

        Console.WriteLine($"First:  [{string.Join(", ", first.ToArray())}]");
        Console.WriteLine($"Middle: [{string.Join(", ", middle.ToArray())}]");
        Console.WriteLine($"Last:   [{string.Join(", ", last.ToArray())}]");

        // All slices are views into the same underlying memory
        middle[0] = 999;
        Console.WriteLine($"After middle[0] = 999, data[3] = {data[3]}");

        Console.WriteLine();
    }

    /// <summary>
    /// Passing spans to functions - efficient, safe parameter passing.
    /// </summary>
    public static void DemonstrateFunctionParameters()
    {
        Console.WriteLine("--- Span as Function Parameter ---");

        int[] data = [5, 3, 8, 1, 9, 2, 7, 4, 6, 0];

        Console.WriteLine($"Original: [{string.Join(", ", data)}]");
        Console.WriteLine($"Sum: {Sum(data)}");

        // Sort a slice without affecting the rest
        BubbleSort(data.AsSpan(2, 5));
        Console.WriteLine($"After sorting [2..7]: [{string.Join(", ", data)}]");

        // Fill a region
        Fill(data.AsSpan(0, 3), 42);
        Console.WriteLine($"After filling [0..3] with 42: [{string.Join(", ", data)}]");

        Console.WriteLine();
    }

    /// <summary>
    /// ReadOnlySpan for immutable views.
    /// </summary>
    public static void DemonstrateReadOnlySpan()
    {
        Console.WriteLine("--- ReadOnlySpan ---");

        int[] data = [1, 2, 3, 4, 5];

        // ReadOnlySpan prevents modification
        ReadOnlySpan<int> roSpan = data;

        Console.WriteLine($"ReadOnlySpan[2] = {roSpan[2]}");
        // roSpan[2] = 10; // Compile error - cannot modify

        // String is backed by ReadOnlySpan<char>
        ReadOnlySpan<char> chars = "Hello, World!".AsSpan();
        ReadOnlySpan<char> word = chars[7..12]; // "World"
        Console.WriteLine($"Sliced string: {word.ToString()}");

        Console.WriteLine();
    }

    /// <summary>
    /// Contrast with unsafe pointer approach.
    /// </summary>
    public static void DemonstrateContrastWithPointers()
    {
        Console.WriteLine("--- Contrast: Span vs Pointers ---");

        int[] data = [10, 20, 30, 40, 50];

        // UNSAFE: Pointer approach - no bounds checking
        unsafe
        {
            fixed (int* ptr = data)
            {
                Console.WriteLine($"Pointer access ptr[2]: {ptr[2]}");
                // ptr[100] would compile and run - undefined behavior!
            }
        }

        // SAFE: Span approach - bounds checked
        Span<int> span = data;
        Console.WriteLine($"Span access span[2]: {span[2]}");
        // span[100] throws IndexOutOfRangeException - defined behavior

        Console.WriteLine("Pointers: No bounds checking, undefined behavior possible");
        Console.WriteLine("Span: Bounds checked, throws on invalid access");

        Console.WriteLine();
    }

    /// <summary>
    /// THE COMPELLING CASE: Methods that RETURN Spans.
    ///
    /// Returning a Span allows callers to get a safe, bounds-checked view
    /// into internal state without copying and without exposing pointers.
    /// </summary>
    public static void DemonstrateReturningSpans()
    {
        Console.WriteLine("--- Returning Spans (The Compelling Case) ---");

        var container = new DataContainer([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        // Get a safe view into the container's internal data
        // No copying, no pointers exposed, fully bounds-checked
        Span<int> firstHalf = container.GetFirstHalf();
        Span<int> lastHalf = container.GetLastHalf();
        ReadOnlySpan<int> readOnlyView = container.AsReadOnlySpan();

        Console.WriteLine($"First half: [{string.Join(", ", firstHalf.ToArray())}]");
        Console.WriteLine($"Last half: [{string.Join(", ", lastHalf.ToArray())}]");

        // Modify through the returned span - changes the container
        firstHalf[0] = 100;
        Console.WriteLine($"After firstHalf[0] = 100, container[0] = {container[0]}");

        // ReadOnlySpan prevents modification
        // readOnlyView[0] = 999; // Compile error!
        Console.WriteLine($"ReadOnly view[0] = {readOnlyView[0]}");

        // Subslicing - get a view of a view
        Span<int> subSlice = container.GetRange(2, 4);
        Console.WriteLine($"Range [2..6]: [{string.Join(", ", subSlice.ToArray())}]");

        Console.WriteLine();
    }

    // Helper functions that work with Span

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
        Console.WriteLine("=== C# Span<T> Examples ===");
        Console.WriteLine("Span provides safe, bounds-checked access to contiguous memory.\n");

        DemonstrateArraySpan();
        DemonstrateStackalloc();
        DemonstrateNativeMemory();
        DemonstrateSlicing();
        DemonstrateFunctionParameters();
        DemonstrateReadOnlySpan();
        DemonstrateReturningSpans();
        DemonstrateContrastWithPointers();

        Console.WriteLine("--- Summary ---");
        Console.WriteLine("Span<T> provides:");
        Console.WriteLine("- Safe, bounds-checked memory access");
        Console.WriteLine("- Zero-copy slicing and views");
        Console.WriteLine("- Works with arrays, stack memory, and native memory");
        Console.WriteLine("- Cannot escape to heap (ref struct)");
        Console.WriteLine("- Can be RETURNED from methods - the compelling case!");
        Console.WriteLine("- The safe alternative to pointer manipulation");
    }
}

/// <summary>
/// Example class that RETURNS Spans into its internal data.
///
/// This demonstrates the compelling use case: exposing internal state
/// safely without copying and without pointers.
/// </summary>
public class DataContainer
{
    private readonly int[] _data;

    public DataContainer(int[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public int Length => _data.Length;

    public int this[int index] => _data[index];

    /// <summary>
    /// Returns a Span over the first half of the data.
    ///
    /// SAFETY DISCHARGE: This is safe because:
    /// - _data is readonly and cannot be replaced
    /// - Span's ref struct nature prevents it from outliving this container
    ///   in typical usage (cannot be stored in fields)
    /// - Bounds are computed from _data.Length, guaranteed valid
    /// </summary>
    public Span<int> GetFirstHalf()
    {
        return _data.AsSpan(0, _data.Length / 2);
    }

    /// <summary>
    /// Returns a Span over the last half of the data.
    ///
    /// SAFETY DISCHARGE: Same as GetFirstHalf - bounds computed safely.
    /// </summary>
    public Span<int> GetLastHalf()
    {
        int midpoint = _data.Length / 2;
        return _data.AsSpan(midpoint, _data.Length - midpoint);
    }

    /// <summary>
    /// Returns a Span over a specified range.
    ///
    /// SAFETY DISCHARGE: Safe because AsSpan validates start/length
    /// and throws ArgumentOutOfRangeException for invalid ranges.
    /// </summary>
    public Span<int> GetRange(int start, int length)
    {
        return _data.AsSpan(start, length);
    }

    /// <summary>
    /// Returns a read-only view of all data.
    ///
    /// SAFETY DISCHARGE: Safe - returns immutable view of valid array.
    /// </summary>
    public ReadOnlySpan<int> AsReadOnlySpan()
    {
        return _data.AsSpan();
    }
}
