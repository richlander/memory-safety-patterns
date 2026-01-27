// Swift Memory Safety Library - Sneaky Unsafe Code
//
// This file demonstrates Swift's PERMISSIVENESS compared to Rust.
//
// The key insight: In Swift (even with StrictMemorySafety), library authors
// CAN call @unsafe functions internally WITHOUT:
// 1. Marking their own function @unsafe
// 2. Using `unsafe` expression
//
// The result: WARNINGS during library compilation, but:
// - The code still compiles and runs
// - Consumers see a "normal" API with no @unsafe marking
// - The unsafety is HIDDEN from consumers
//
// This is IMPOSSIBLE in Rust - `unsafe fn` is part of the public contract.

import Foundation

// ============================================================================
// SNEAKY CROSS-FUNCTION: Calling @unsafe without acknowledgment
// ============================================================================

/// This function calls @unsafe functions WITHOUT proper acknowledgment.
///
/// CROSS-FUNCTION sneakiness:
/// - Calls UnsafeApi.alloc/free which are @unsafe
/// - Does NOT use `unsafe` expression
/// - Does NOT mark itself @unsafe
///
/// With StrictMemorySafety: Produces WARNINGS during library build.
/// But the warnings are NOT visible to consumers!
public func sneakyAllocate(count: Int) -> [Int] {
    // WARNING: expression uses unsafe constructs but is not marked with 'unsafe'
    let ptr = UnsafeApi.alloc(count: count)

    var result: [Int] = []
    for i in 0..<count {
        // WARNING: uses unsafe constructs
        result.append(UnsafeApi.read(ptr, offset: i))
    }

    // WARNING: uses unsafe constructs
    UnsafeApi.free(ptr, count: count)
    return result
}

/// Same function but PROPERLY annotated.
/// No warnings because unsafety is acknowledged.
@safe
public func properAllocate(count: Int) -> [Int] {
    let ptr = unsafe UnsafeApi.alloc(count: count)

    var result: [Int] = []
    for i in 0..<count {
        result.append(unsafe UnsafeApi.read(ptr, offset: i))
    }

    unsafe UnsafeApi.free(ptr, count: count)
    return result
}

// ============================================================================
// SNEAKY CROSS-MODULE: Hidden unsafety across module boundaries
// ============================================================================

/// This function is callable WITHOUT `unsafe` by external consumers,
/// even though it does unsafe things internally.
///
/// CROSS-MODULE sneakiness:
/// - Consumers in other modules see a "normal" function
/// - No @unsafe in the signature
/// - The warnings happened during THIS module's compilation
/// - Consumers don't see the warnings
public func sneakyRetain<T: AnyObject>(_ object: T) -> UnsafeMutableRawPointer {
    // WARNING: uses unsafe constructs (only library author sees this)
    return properRetainInternal(object)
}

/// The PROPER version - marked @unsafe so consumers must acknowledge.
@unsafe
public func properRetain<T: AnyObject>(_ object: T) -> UnsafeMutableRawPointer {
    return unsafe properRetainInternal(object)
}

@unsafe
private func properRetainInternal<T: AnyObject>(_ object: T) -> UnsafeMutableRawPointer {
    let unmanaged = unsafe Unmanaged.passRetained(object)
    return unsafe unmanaged.toOpaque()
}

// ============================================================================
// Summary of Swift's Permissiveness
// ============================================================================
//
// In Rust:
//   - Calling `unsafe fn` REQUIRES either:
//     a) Being `unsafe fn` yourself (propagate), OR
//     b) Using `unsafe {}` block (suppress)
//   - This is ENFORCED at compile time
//   - There is NO way to hide unsafety from callers
//
// In Swift (with StrictMemorySafety):
//   - Calling @unsafe function WITHOUT acknowledgment produces WARNING
//   - But warnings are just warnings - code still compiles
//   - Consumers don't see the warnings from library compilation
//   - @unsafe is OPT-IN documentation, not enforced contract
//
// This means Swift allows "sneaky" patterns that Rust forbids.
