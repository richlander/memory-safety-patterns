// C# Memory Safety Library - Unsafe.As Example (Attribute-Based Model)
//
// In the ATTRIBUTE MODEL:
// - Unsafe.As will be marked with [RequiresUnsafe]
// - It does NOT get the `unsafe` keyword (no pointers involved)
// - Callers must acknowledge via unsafe block or [RequiresUnsafe]
//
// This shows the key benefit: [RequiresUnsafe] can mark methods
// that are semantically unsafe WITHOUT requiring pointers.

using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MemoryLib;

/// <summary>
/// Simulates how Unsafe.As would look with [RequiresUnsafe].
///
/// In the BCL, these methods would gain the attribute:
/// [RequiresUnsafe("Reinterprets memory without type checking")]
/// public static TTo As&lt;TFrom, TTo&gt;(ref TFrom source);
/// </summary>
public static class UnsafeAsExample
{
    /// <summary>
    /// Demonstrates calling Unsafe.As with acknowledgment.
    ///
    /// ATTRIBUTE MODEL: Unsafe.As has [RequiresUnsafe], so we need
    /// an unsafe block to call it. The `unsafe` keyword here is
    /// acknowledging the ATTRIBUTE, not enabling pointer operations.
    /// </summary>
    public static void DemonstrateUnsafeAs()
    {
        Console.WriteLine("--- Unsafe.As (Attribute Model) ---");

        string[] strings = ["hello", "world"];

        // Unsafe.As will have [RequiresUnsafe] attribute
        // We acknowledge it with unsafe block
        // Note: No pointers involved, but we still need unsafe block
        // to acknowledge the [RequiresUnsafe] attribute
        object[] objects;
        unsafe
        {
            objects = Unsafe.As<string[], object[]>(ref strings);
        }

        Console.WriteLine($"Reinterpreted string[] as object[]");
        Console.WriteLine($"Same reference: {ReferenceEquals(strings, objects)}");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Unsafe.As for value type reinterpretation.
    /// </summary>
    public static void DemonstrateUnsafeAsRef()
    {
        Console.WriteLine("--- Unsafe.As<int, float> (Attribute Model) ---");

        int intValue = 0x40490FDB;

        // [RequiresUnsafe] acknowledgment via unsafe block
        float floatValue;
        unsafe
        {
            ref float floatRef = ref Unsafe.As<int, float>(ref intValue);
            floatValue = floatRef;
        }

        Console.WriteLine($"Int bits: 0x{intValue:X8}");
        Console.WriteLine($"Reinterpreted as float: {floatValue:F6}");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Unsafe.Add - pointer arithmetic without pointers.
    /// </summary>
    public static void DemonstrateUnsafeAdd()
    {
        Console.WriteLine("--- Unsafe.Add (Attribute Model) ---");

        int[] array = [10, 20, 30, 40, 50];
        ref int first = ref array[0];

        // [RequiresUnsafe] because it can go out of bounds
        int thirdValue;
        unsafe
        {
            ref int third = ref Unsafe.Add(ref first, 2);
            thirdValue = third;
        }

        Console.WriteLine($"array[0] = {array[0]}");
        Console.WriteLine($"Unsafe.Add(ref array[0], 2) = {thirdValue}");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Unsafe.SizeOf.
    /// </summary>
    public static void DemonstrateUnsafeSizeOf()
    {
        Console.WriteLine("--- Unsafe.SizeOf (Attribute Model) ---");

        // SizeOf is relatively safe but part of Unsafe class
        // May or may not get [RequiresUnsafe] in final design
        int intSize, guidSize, structSize;
        unsafe
        {
            intSize = Unsafe.SizeOf<int>();
            guidSize = Unsafe.SizeOf<Guid>();
            structSize = Unsafe.SizeOf<ExampleStruct>();
        }

        Console.WriteLine($"SizeOf<int>: {intSize}");
        Console.WriteLine($"SizeOf<Guid>: {guidSize}");
        Console.WriteLine($"SizeOf<ExampleStruct>: {structSize}");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates unaligned access.
    /// </summary>
    public static void DemonstrateUnalignedAccess()
    {
        Console.WriteLine("--- Unsafe.WriteUnaligned/ReadUnaligned (Attribute Model) ---");

        byte[] buffer = new byte[16];
        ref byte startRef = ref buffer[1];

        // [RequiresUnsafe] because it can violate memory safety
        unsafe
        {
            Unsafe.WriteUnaligned(ref startRef, 0x12345678);
        }

        int value;
        unsafe
        {
            value = Unsafe.ReadUnaligned<int>(ref startRef);
        }

        Console.WriteLine($"Wrote 0x12345678 at unaligned offset 1");
        Console.WriteLine($"Read back: 0x{value:X8}");
        Console.WriteLine($"Buffer bytes: [{string.Join(", ", buffer[..8].Select(b => $"0x{b:X2}"))}]");
        Console.WriteLine();
    }

    /// <summary>
    /// Shows how a method can PROPAGATE [RequiresUnsafe].
    /// </summary>
    [RequiresUnsafe("Reinterprets types without validation")]
    public static TTo ReinterpretAs<TFrom, TTo>(ref TFrom source)
    {
        // We propagate [RequiresUnsafe] to our callers
        // So we can call Unsafe.As without unsafe block
        // (Our own [RequiresUnsafe] acknowledges it)
        //
        // NOTE: In actual implementation, the compiler would need
        // to recognize that [RequiresUnsafe] acknowledges other
        // [RequiresUnsafe] methods. For now, we still use unsafe.
        unsafe
        {
            return Unsafe.As<TFrom, TTo>(ref source);
        }
    }

    /// <summary>
    /// Shows how a method can SUPPRESS [RequiresUnsafe].
    /// </summary>
    public static int SafeReinterpretBoolAsInt(ref bool source)
    {
        // We SUPPRESS [RequiresUnsafe] - our method is safe
        // because bool->int reinterpretation is well-defined:
        // false = 0, true = 1 (or any non-zero)
        unsafe
        {
            return Unsafe.As<bool, int>(ref source) != 0 ? 1 : 0;
        }
    }

    /// <summary>
    /// Contrast: Attribute model vs Keyword model.
    /// </summary>
    public static void DemonstrateAttributeVsKeyword()
    {
        Console.WriteLine("--- Attribute Model vs Keyword Model ---");
        Console.WriteLine();

        Console.WriteLine("KEYWORD MODEL (csharpnext):");
        Console.WriteLine("- `unsafe` on methods propagates to callers");
        Console.WriteLine("- `unsafe` means both 'pointers' and 'semantically unsafe'");
        Console.WriteLine("- Single concept, overloaded meaning");
        Console.WriteLine();

        Console.WriteLine("ATTRIBUTE MODEL (csharpnext-attribute):");
        Console.WriteLine("- `unsafe` keyword: Only for pointers (unchanged)");
        Console.WriteLine("- [RequiresUnsafe]: Semantic unsafety (new)");
        Console.WriteLine("- Two separate concepts, clearer intent");
        Console.WriteLine("- Can have [RequiresUnsafe] WITHOUT pointers");
        Console.WriteLine("- More backward compatible");
        Console.WriteLine();

        // Example: A method that is semantically unsafe but has no pointers
        Console.WriteLine("Example - Unsafe.As in each model:");
        Console.WriteLine("  Keyword model:    unsafe TTo As<TFrom, TTo>(...) { }");
        Console.WriteLine("  Attribute model:  [RequiresUnsafe] TTo As<TFrom, TTo>(...) { }");
        Console.WriteLine();
    }

    /// <summary>
    /// Runs all demonstrations.
    /// </summary>
    public static void RunAllDemonstrations()
    {
        Console.WriteLine("=== Unsafe.As Examples (Attribute Model) ===");
        Console.WriteLine("Showing [RequiresUnsafe] as separate from `unsafe` keyword.\n");

        DemonstrateUnsafeAs();
        DemonstrateUnsafeAsRef();
        DemonstrateUnsafeAdd();
        DemonstrateUnsafeSizeOf();
        DemonstrateUnalignedAccess();
        DemonstrateAttributeVsKeyword();

        Console.WriteLine("--- Summary ---");
        Console.WriteLine("ATTRIBUTE MODEL benefits:");
        Console.WriteLine("- Clear separation: pointers vs semantic unsafety");
        Console.WriteLine("- [RequiresUnsafe] can include a MESSAGE explaining why");
        Console.WriteLine("- More backward compatible with existing code");
        Console.WriteLine("- Fine-grained: method can be one, both, or neither");
    }
}

/// <summary>
/// Example struct for SizeOf demonstration.
/// </summary>
public struct ExampleStruct
{
    public int A;
    public long B;
    public byte C;
}
