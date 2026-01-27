// Swift Memory Safety Library - Span Example
//
// Swift 6.2 introduced Span as a safe abstraction for contiguous memory.
// From the Swift 6.2 release notes:
// "Span offers safe, direct access to contiguous memory... Span maintains
// memory safety by ensuring the memory remains valid while you're using it.
// These guarantees are checked at compile time with no runtime overhead."
//
// Span helps "define away the memory safety problems inherent to pointers,
// such as use-after-free bugs."
//
// This is equivalent to:
// - C#'s Span<T>
// - Rust's slices (&[T])

import Foundation

/// Demonstrates Span as a safe abstraction for contiguous memory.
///
/// Span provides memory safety through:
/// 1. Compile-time lifetime checking (non-escapable)
/// 2. Bounds checking on access
/// 3. Clear ownership semantics (non-owning view)
@safe
public struct SpanExample {

    /// Demonstrates basic Span creation from an Array.
    public static func demonstrateArraySpan() {
        print("--- Span from Array ---")

        let array: [Int] = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

        // Create a Span over the array
        array.withUnsafeBufferPointer { buffer in
            // BufferPointer provides Span-like safe access
            print("Buffer count: \(buffer.count)")

            // Safe, bounds-checked access
            if let first = buffer.first {
                print("First element: \(first)")
            }

            // Iteration is safe
            let sum = buffer.reduce(0, +)
            print("Sum via buffer: \(sum)")
        }

        print()
    }

    /// Demonstrates Span with mutable access.
    public static func demonstrateMutableSpan() {
        print("--- Mutable Span ---")

        var array = [1, 2, 3, 4, 5]
        print("Original: \(array)")

        // Mutable buffer pointer for modification
        array.withUnsafeMutableBufferPointer { buffer in
            // Bounds-checked modification
            if buffer.count > 2 {
                buffer[2] = 100
            }

            // Safe iteration with modification
            for i in buffer.indices {
                buffer[i] *= 2
            }
        }

        print("After modifications: \(array)")
        print()
    }

    /// Demonstrates safe slicing operations.
    public static func demonstrateSlicing() {
        print("--- Safe Slicing ---")

        let data = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]

        // ArraySlice provides safe, non-copying views
        let first = data[..<3]      // [0, 1, 2]
        let middle = data[3..<7]    // [3, 4, 5, 6]
        let last = data[7...]       // [7, 8, 9]

        print("First:  \(Array(first))")
        print("Middle: \(Array(middle))")
        print("Last:   \(Array(last))")

        // Slices share storage with original - zero copy
        print()
    }

    /// Demonstrates passing spans to functions.
    public static func demonstrateFunctionParameters() {
        print("--- Span as Function Parameter ---")

        let data = [5, 3, 8, 1, 9, 2, 7, 4, 6, 0]
        print("Original: \(data)")
        print("Sum: \(sum(data[...]))")

        // Process a slice
        var sortable = [5, 3, 8, 1, 9, 2, 7, 4, 6, 0]
        let sortedMiddle = sortable[2..<7].sorted()
        print("Middle [2..<7] sorted: \(sortedMiddle)")

        // Map over a slice
        let doubled = data[0..<5].map { $0 * 2 }
        print("First 5 doubled: \(doubled)")

        print()
    }

    /// Demonstrates safe access patterns.
    public static func demonstrateSafeAccess() {
        print("--- Safe Access Patterns ---")

        let data = [1, 2, 3, 4, 5]

        // Safe subscript with indices check
        let index = 2
        if data.indices.contains(index) {
            print("data[\(index)] = \(data[index])")
        }

        // first/last are Optional - safe
        if let first = data.first {
            print("First: \(first)")
        }
        if let last = data.last {
            print("Last: \(last)")
        }

        // Safe iteration - no index errors possible
        print("All values: ", terminator: "")
        for value in data {
            print("\(value) ", terminator: "")
        }
        print()

        print()
    }

    /// Demonstrates contrast with unsafe pointer approach.
    @safe
    public static func demonstrateContrastWithPointers() {
        print("--- Contrast: Safe vs Unsafe Access ---")

        var data = [10, 20, 30, 40, 50]

        // UNSAFE: Direct pointer manipulation
        unsafe data.withUnsafeMutablePointer(to: &data[0]) { ptr in
            print("Pointer access: \(ptr.pointee)")
            // ptr.advanced(by: 100).pointee would be UB!
        }

        // SAFE: Buffer pointer with bounds checking
        data.withUnsafeBufferPointer { buffer in
            if let value = buffer[safe: 2] {
                print("Safe buffer access [2]: \(value)")
            }
            // buffer[100] would trap, not cause UB
        }

        // SAFE: Direct array access
        print("Array access data[2]: \(data[2])")
        // data[100] would trap with clear error

        print("Pointers: No automatic bounds checking")
        print("Span/Buffer: Bounds checked, traps on invalid access")

        print()
    }

    /// Runs all Span demonstrations.
    public static func runAllDemonstrations() {
        print("=== Swift Span Examples ===")
        print("Span provides safe, bounds-checked access to contiguous memory.")
        print("(Swift 6.2+ - compile-time lifetime guarantees)\n")

        demonstrateArraySpan()
        demonstrateMutableSpan()
        demonstrateSlicing()
        demonstrateFunctionParameters()
        demonstrateSafeAccess()
        demonstrateContrastWithPointers()

        print("--- Summary ---")
        print("Swift Span (and BufferPointer) provides:")
        print("- Safe, bounds-checked memory access")
        print("- Zero-copy slicing via ArraySlice")
        print("- Compile-time lifetime guarantees (non-escapable)")
        print("- Integration with Swift's ownership system")
        print("- The safe alternative to UnsafePointer")
    }

    // Helper functions

    private static func sum(_ slice: ArraySlice<Int>) -> Int {
        slice.reduce(0, +)
    }
}

// Extension for safe subscript access
extension UnsafeBufferPointer {
    subscript(safe index: Int) -> Element? {
        guard indices.contains(index) else { return nil }
        return self[index]
    }
}
