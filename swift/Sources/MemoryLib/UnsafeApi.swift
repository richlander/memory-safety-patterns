// Swift Memory Safety Library - Unsafe API
//
// This file demonstrates both CROSS-FUNCTION and CROSS-MODULE propagation
// in Swift 6.2 with StrictMemorySafety enabled.
//
// Swift propagation rules (with StrictMemorySafety):
// - CROSS-FUNCTION: Calling @unsafe function requires `unsafe` expression
// - CROSS-MODULE: Same rule applies across module boundaries
//
// WITHOUT StrictMemorySafety: @unsafe is just documentation, not enforced!

import Foundation

// ============================================================================
// INTERNAL: Cross-Function Propagation (within this module)
// ============================================================================

/// Low-level allocation - marked @unsafe.
/// CROSS-FUNCTION: Callers within this module should use `unsafe` to call.
@unsafe
func rawAlloc(count: Int) -> UnsafeMutablePointer<Int> {
    let ptr = UnsafeMutablePointer<Int>.allocate(capacity: count)
    return ptr
}

/// Low-level deallocation.
@unsafe
func rawDealloc(_ ptr: UnsafeMutablePointer<Int>, count: Int) {
    ptr.deinitialize(count: count)
    ptr.deallocate()
}

/// Mid-level function that PROPAGATES unsafety (still @unsafe).
/// CROSS-FUNCTION: Even within the same module, @unsafe propagates.
@unsafe
func midLevelAllocUninit(count: Int) -> UnsafeMutablePointer<Int> {
    // Call internal @unsafe function
    return rawAlloc(count: count)
}

/// Mid-level function that SUPPRESSES unsafety.
/// Returns pointer but is marked @safe - we take responsibility.
@safe
func midLevelAllocZeroed(count: Int) -> UnsafeMutablePointer<Int> {
    precondition(count > 0, "Count must be positive")

    // CROSS-FUNCTION: Use `unsafe` to acknowledge calling @unsafe function
    let ptr = unsafe rawAlloc(count: count)

    // Initialize to zero - makes it safe to read
    for i in 0..<count {
        ptr[i] = 0
    }

    return ptr
}

// ============================================================================
// PUBLIC: Cross-Module Propagation (exported to consumers)
// ============================================================================

/// PUBLIC UNSAFE API - Propagates unsafety to external consumers.
///
/// CROSS-MODULE: Consumers in other modules must use `unsafe` to call
/// (when StrictMemorySafety is enabled).
public enum UnsafeApi {

    /// Allocates a buffer.
    /// CROSS-MODULE propagation: External callers need `unsafe`.
    @unsafe
    public static func alloc(count: Int) -> UnsafeMutablePointer<Int> {
        precondition(count > 0, "Count must be positive")

        // CROSS-FUNCTION call to internal @unsafe function
        let ptr = rawAlloc(count: count)

        // Initialize to zero
        for i in 0..<count {
            ptr[i] = 0
        }
        return ptr
    }

    /// Frees memory allocated by `alloc()`.
    @unsafe
    public static func free(_ ptr: UnsafeMutablePointer<Int>, count: Int) {
        rawDealloc(ptr, count: count)
    }

    /// Reads a value at the specified offset.
    @unsafe
    public static func read(_ ptr: UnsafePointer<Int>, offset: Int) -> Int {
        return ptr[offset]
    }

    /// Writes a value at the specified offset.
    @unsafe
    public static func write(_ ptr: UnsafeMutablePointer<Int>, offset: Int, value: Int) {
        ptr[offset] = value
    }
}

// ============================================================================
// PROPAGATION CHAINS
// ============================================================================

/// Demonstrates propagation chains within and across modules.
public enum PropagationChain {

    /// Level 1: Calls raw @unsafe function.
    @unsafe
    private static func level1Unsafe() -> UnsafeMutablePointer<Int> {
        return rawAlloc(count: 1)
    }

    /// Level 2: Calls Level1, propagates (still @unsafe).
    @unsafe
    private static func level2Unsafe() -> UnsafeMutablePointer<Int> {
        return level1Unsafe()
    }

    /// Level 3 PROPAGATE: Public and @unsafe.
    /// CROSS-MODULE: External callers must use `unsafe`.
    @unsafe
    public static func level3Propagate() -> UnsafeMutablePointer<Int> {
        return level2Unsafe()
    }

    /// Level 3 SUPPRESS: Public and @safe.
    /// CROSS-MODULE: External callers do NOT need `unsafe`.
    @safe
    public static func level3Suppress() -> UnsafeMutablePointer<Int> {
        // Suppress by using `unsafe` internally
        return unsafe level2Unsafe()
    }

    /// Cleanup - @unsafe, so callers need `unsafe`.
    @unsafe
    public static func cleanup(_ ptr: UnsafeMutablePointer<Int>) {
        rawDealloc(ptr, count: 1)
    }
}
