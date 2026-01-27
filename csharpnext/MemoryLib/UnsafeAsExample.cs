// C# Memory Safety Library - Unsafe.As Example (Future Conventions)
//
// CRITICAL CHANGE IN .NET 11+:
// System.Runtime.CompilerServices.Unsafe methods will be marked `unsafe`
// and require callers to use unsafe context!
//
// Current behavior:
// - Unsafe.As does NOT require unsafe block (despite the type name)
// - This is a SAFETY GAP - dangerous operations without compiler tracking
//
// Future behavior (MemorySafetyRules):
// - Unsafe.As WILL require unsafe block
// - All Unsafe.* methods will require unsafe context
// - The type name finally matches the compiler requirement!
//
// From the design doc:
// "The Unsafe class provides generic, low-level functionality for
// manipulating managed and unmanaged pointers. It does not (at the
// time of writing) require an unsafe context... We plan to change
// these APIs to require callers use an unsafe context."
//
// See: https://github.com/dotnet/runtime/issues/41418
// See: https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/

using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MemoryLib;

/// <summary>
/// Demonstrates Unsafe.As usage under FUTURE conventions.
///
/// KEY CHANGE: In .NET 11+ with MemorySafetyRules, all Unsafe.* methods
/// will require unsafe context, matching their dangerous semantics.
///
/// This file shows:
/// 1. How code will look with required unsafe blocks
/// 2. The safety documentation that should accompany such code
/// 3. Minimal unsafe block conventions
/// </summary>
public static class UnsafeAsExample
{
    /// <summary>
    /// Demonstrates Unsafe.As with FUTURE required unsafe block.
    ///
    /// <para><b>Safety:</b> The arrays must be compatible types.
    /// Reinterpreting incompatible types leads to undefined behavior.</para>
    /// </summary>
    /// <remarks>
    /// CURRENT: No unsafe block required (safety gap!)
    /// FUTURE: unsafe block required (consistent with semantics)
    ///
    /// We show the FUTURE convention here with unsafe blocks.
    /// </remarks>
    public static void DemonstrateUnsafeAs()
    {
        Console.WriteLine("--- Unsafe.As (Future: requires unsafe) ---");

        string[] strings = ["hello", "world"];

        // FUTURE: This will require unsafe context
        // CURRENT: This compiles without unsafe (!)
        //
        // MINIMAL UNSAFE BLOCK: Only the Unsafe.As call
        object[] objects;
        unsafe
        {
            // Safety: string[] is covariant with object[], this is valid
            objects = Unsafe.As<string[], object[]>(ref strings);
        }

        Console.WriteLine($"Reinterpreted string[] as object[]");
        Console.WriteLine($"Same reference: {ReferenceEquals(strings, objects)}");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Unsafe.As for value type reinterpretation.
    ///
    /// <para><b>Safety:</b> Types must have compatible memory layouts.
    /// int and float are both 4 bytes, so this is valid.</para>
    /// </summary>
    public static void DemonstrateUnsafeAsRef()
    {
        Console.WriteLine("--- Unsafe.As<int, float> (Future: requires unsafe) ---");

        int intValue = 0x40490FDB; // Bit pattern for approximately pi

        // FUTURE: This will require unsafe context
        // MINIMAL UNSAFE BLOCK
        float floatValue;
        unsafe
        {
            // Safety: int and float are same size (4 bytes)
            ref float floatRef = ref Unsafe.As<int, float>(ref intValue);
            floatValue = floatRef;
        }

        Console.WriteLine($"Int bits: 0x{intValue:X8}");
        Console.WriteLine($"Reinterpreted as float: {floatValue:F6}");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Unsafe.Add for pointer-like arithmetic.
    ///
    /// <para><b>Safety:</b> Offset must not exceed array bounds.
    /// Going out of bounds is undefined behavior.</para>
    /// </summary>
    public static void DemonstrateUnsafeAdd()
    {
        Console.WriteLine("--- Unsafe.Add (Future: requires unsafe) ---");

        int[] array = [10, 20, 30, 40, 50];
        ref int first = ref array[0];

        // FUTURE: Unsafe.Add will require unsafe context
        // MINIMAL UNSAFE BLOCK
        int thirdValue;
        unsafe
        {
            // Safety: offset 2 is within array bounds [0, 5)
            ref int third = ref Unsafe.Add(ref first, 2);
            thirdValue = third;
        }

        Console.WriteLine($"array[0] = {array[0]}");
        Console.WriteLine($"Unsafe.Add(ref array[0], 2) = {thirdValue}");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Unsafe.SizeOf.
    ///
    /// <para><b>Safety:</b> This is relatively safe but included in Unsafe
    /// class for consistency. No memory access occurs.</para>
    /// </summary>
    public static void DemonstrateUnsafeSizeOf()
    {
        Console.WriteLine("--- Unsafe.SizeOf (Future: requires unsafe) ---");

        // FUTURE: Even SizeOf will require unsafe context for API consistency
        // MINIMAL UNSAFE BLOCKS
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
    /// Demonstrates Unsafe.ReadUnaligned/WriteUnaligned.
    ///
    /// <para><b>Safety:</b> Must ensure buffer has sufficient space.
    /// Writing beyond buffer bounds is undefined behavior.</para>
    /// </summary>
    public static void DemonstrateUnalignedAccess()
    {
        Console.WriteLine("--- Unsafe.WriteUnaligned/ReadUnaligned (Future: requires unsafe) ---");

        byte[] buffer = new byte[16];
        ref byte startRef = ref buffer[1];

        // FUTURE: These will require unsafe context
        // MINIMAL UNSAFE BLOCKS
        unsafe
        {
            // Safety: buffer has 16 bytes, writing 4 bytes at offset 1 is valid
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
    /// Shows the contrast between current and future behavior.
    /// </summary>
    public static void DemonstrateCurrentVsFuture()
    {
        Console.WriteLine("--- Current vs Future Behavior ---");

        int value = 42;

        // CURRENT: Pointer syntax requires unsafe
        unsafe
        {
            int* ptr = &value;
            float* floatPtr = (float*)ptr;
            Console.WriteLine($"Pointer cast (requires unsafe today): {*floatPtr}");
        }

        // CURRENT: Unsafe.As does NOT require unsafe (inconsistent!)
        // FUTURE: Unsafe.As WILL require unsafe (consistent!)
        //
        // We show future convention with unsafe block:
        float reinterpreted;
        unsafe
        {
            reinterpreted = Unsafe.As<int, float>(ref value);
        }
        Console.WriteLine($"Unsafe.As (will require unsafe): {reinterpreted}");

        Console.WriteLine();
        Console.WriteLine("CURRENT: Unsafe.As doesn't require unsafe block");
        Console.WriteLine("FUTURE:  Unsafe.As will require unsafe block");
        Console.WriteLine();
    }

    /// <summary>
    /// Runs all demonstrations.
    /// </summary>
    public static void RunAllDemonstrations()
    {
        Console.WriteLine("=== Unsafe.As Examples (Future Conventions) ===");
        Console.WriteLine("Demonstrating FUTURE behavior where Unsafe.* requires unsafe blocks.\n");

        DemonstrateUnsafeAs();
        DemonstrateUnsafeAsRef();
        DemonstrateUnsafeAdd();
        DemonstrateUnsafeSizeOf();
        DemonstrateUnalignedAccess();
        DemonstrateCurrentVsFuture();

        Console.WriteLine("--- Summary ---");
        Console.WriteLine("CURRENT (.NET 10 and earlier):");
        Console.WriteLine("- Unsafe.* methods don't require unsafe blocks");
        Console.WriteLine("- Safety gap: dangerous operations without tracking");
        Console.WriteLine();
        Console.WriteLine("FUTURE (.NET 11+ with MemorySafetyRules):");
        Console.WriteLine("- Unsafe.* methods WILL require unsafe blocks");
        Console.WriteLine("- Consistent: type name matches compiler requirement");
        Console.WriteLine("- Use minimal unsafe blocks around only the Unsafe.* calls");
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
