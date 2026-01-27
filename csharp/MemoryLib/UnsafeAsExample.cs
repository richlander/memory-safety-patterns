// C# Memory Safety Library - Unsafe.As Example
//
// This file demonstrates that System.Runtime.CompilerServices.Unsafe methods
// are "safe" from the C# compiler's perspective - they don't require an
// unsafe block to call, despite "Unsafe" being in the type name.
//
// Key insight: C# unsafety propagates via POINTER TYPES in signatures,
// not via type or method names. The Unsafe class uses refs, not pointers,
// so callers don't need unsafe blocks.

using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MemoryLib;

/// <summary>
/// Demonstrates that Unsafe.As and related methods don't require unsafe blocks.
///
/// The Unsafe class performs low-level operations that can violate type safety,
/// BUT from the C# compiler's perspective, these are "safe" calls because:
/// 1. No pointer types in signatures (uses refs instead)
/// 2. No unsafe keyword required
///
/// This shows that "unsafe" in C# is about SYNTAX (pointers), not SEMANTICS
/// (what the code actually does).
/// </summary>
public static class UnsafeAsExample
{
    /// <summary>
    /// Demonstrates Unsafe.As for reference type reinterpretation.
    /// NO unsafe block required - "safe" syntax despite dangerous semantics.
    /// </summary>
    public static void DemonstrateUnsafeAs()
    {
        // Reinterpret a string array as an object array
        // This is type-unsafe but doesn't require unsafe keyword
        string[] strings = ["hello", "world"];

        // Unsafe.As reinterprets without any runtime checks
        // No unsafe block needed!
        object[] objects = Unsafe.As<string[], object[]>(ref strings);

        Console.WriteLine("Unsafe.As<string[], object[]> - no unsafe block needed:");
        Console.WriteLine($"  Original: string[{strings.Length}]");
        Console.WriteLine($"  Reinterpreted: object[{objects.Length}]");
        Console.WriteLine($"  Same reference: {ReferenceEquals(strings, objects)}");
    }

    /// <summary>
    /// Demonstrates Unsafe.As for value type reinterpretation via refs.
    /// NO unsafe block required.
    /// </summary>
    public static void DemonstrateUnsafeAsRef()
    {
        // Reinterpret an int as a float (same bit pattern)
        int intValue = 0x40490FDB; // Bit pattern for approximately pi

        // Unsafe.As<TFrom, TTo>(ref TFrom) - no unsafe needed
        ref float floatRef = ref Unsafe.As<int, float>(ref intValue);

        Console.WriteLine("\nUnsafe.As<int, float>(ref int) - no unsafe block needed:");
        Console.WriteLine($"  Int bits: 0x{intValue:X8}");
        Console.WriteLine($"  As float: {floatRef:F6}");
    }

    /// <summary>
    /// Demonstrates Unsafe.Add for pointer-like arithmetic without pointers.
    /// NO unsafe block required.
    /// </summary>
    public static void DemonstrateUnsafeAdd()
    {
        int[] array = [10, 20, 30, 40, 50];

        // Get a ref to the first element
        ref int first = ref array[0];

        // Use Unsafe.Add to get refs to other elements - like pointer arithmetic
        // No unsafe block needed!
        ref int third = ref Unsafe.Add(ref first, 2);

        Console.WriteLine("\nUnsafe.Add(ref first, 2) - no unsafe block needed:");
        Console.WriteLine($"  array[0] = {first}");
        Console.WriteLine($"  array[2] via Unsafe.Add = {third}");

        // Modify through the ref
        third = 999;
        Console.WriteLine($"  After third = 999: array[2] = {array[2]}");
    }

    /// <summary>
    /// Demonstrates Unsafe.SizeOf without unsafe block.
    /// Equivalent to sizeof() but doesn't require unsafe context.
    /// </summary>
    public static void DemonstrateUnsafeSizeOf()
    {
        // Unsafe.SizeOf<T>() - no unsafe needed (unlike sizeof for custom types)
        Console.WriteLine("\nUnsafe.SizeOf<T>() - no unsafe block needed:");
        Console.WriteLine($"  SizeOf<int>: {Unsafe.SizeOf<int>()}");
        Console.WriteLine($"  SizeOf<long>: {Unsafe.SizeOf<long>()}");
        Console.WriteLine($"  SizeOf<Guid>: {Unsafe.SizeOf<Guid>()}");
        Console.WriteLine($"  SizeOf<ExampleStruct>: {Unsafe.SizeOf<ExampleStruct>()}");
    }

    /// <summary>
    /// Demonstrates Unsafe.ReadUnaligned/WriteUnaligned without unsafe.
    /// These read/write values at arbitrary byte offsets.
    /// </summary>
    public static void DemonstrateUnalignedAccess()
    {
        byte[] buffer = new byte[16];

        // Write an int at offset 1 (unaligned) - no unsafe needed
        ref byte startRef = ref buffer[1];
        Unsafe.WriteUnaligned(ref startRef, 0x12345678);

        // Read it back
        int value = Unsafe.ReadUnaligned<int>(ref startRef);

        Console.WriteLine("\nUnsafe.WriteUnaligned/ReadUnaligned - no unsafe block needed:");
        Console.WriteLine($"  Wrote 0x12345678 at unaligned offset 1");
        Console.WriteLine($"  Read back: 0x{value:X8}");
        Console.WriteLine($"  Buffer bytes: [{string.Join(", ", buffer[..8].Select(b => $"0x{b:X2}"))}]");
    }

    /// <summary>
    /// Shows the contrast: equivalent operations WITH pointers require unsafe.
    /// </summary>
    public static void DemonstratePointerEquivalent()
    {
        Console.WriteLine("\nContrast - same operations WITH pointers require unsafe:");

        int value = 42;

        // This REQUIRES unsafe block because of pointer
        unsafe
        {
            int* ptr = &value;
            float* floatPtr = (float*)ptr;
            Console.WriteLine($"  (float*)&intValue requires unsafe: {*floatPtr}");
        }

        // But Unsafe.As does the same thing without unsafe block
        ref float floatRef = ref Unsafe.As<int, float>(ref value);
        Console.WriteLine($"  Unsafe.As<int, float> does NOT require unsafe: {floatRef}");
    }

    /// <summary>
    /// Runs all demonstrations.
    /// Note: This entire method has NO unsafe blocks (except the contrast demo).
    /// </summary>
    public static void RunAllDemonstrations()
    {
        Console.WriteLine("=== Unsafe.As Examples ===");
        Console.WriteLine("Demonstrating that 'Unsafe' type methods don't need unsafe blocks.\n");

        DemonstrateUnsafeAs();
        DemonstrateUnsafeAsRef();
        DemonstrateUnsafeAdd();
        DemonstrateUnsafeSizeOf();
        DemonstrateUnalignedAccess();
        DemonstratePointerEquivalent();

        Console.WriteLine("\n--- Summary ---");
        Console.WriteLine("The System.Runtime.CompilerServices.Unsafe class:");
        Console.WriteLine("- Contains methods that perform low-level memory operations");
        Console.WriteLine("- Uses refs instead of pointers in its API");
        Console.WriteLine("- Therefore does NOT require unsafe blocks to call");
        Console.WriteLine("- Shows that C# 'unsafe' is about SYNTAX (pointers), not semantics");
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
