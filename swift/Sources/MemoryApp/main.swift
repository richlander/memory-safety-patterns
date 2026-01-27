// Swift Memory Safety Application
//
// Demonstrates CROSS-MODULE propagation and suppression in Swift 6.2.
// This module consumes MemoryLib and shows how unsafety crosses
// module boundaries.
//
// Key insight: Swift's @unsafe with StrictMemorySafety is ADVISORY.
// Unlike Rust, it produces warnings, not errors. And library warnings
// are not visible to consumers.

import MemoryLib

print("=== Swift Memory Safety Demo (Swift 6.2) ===\n")

demonstrateCrossModulePropagation()
demonstrateCrossModuleSuppression()
demonstratePropagationChain()
demonstrateSneakyApi()
printSummary()

/// Demonstrates CROSS-MODULE PROPAGATION.
/// @unsafe functions from library require `unsafe` to call (with StrictMemorySafety).
func demonstrateCrossModulePropagation() {
    print("--- Cross-Module Propagation ---")
    print("@unsafe functions from library require `unsafe` expression.\n")

    // Without `unsafe`, this would produce a warning (not error!)
    // let ptr = UnsafeApi.alloc(count: 5)  // WARNING with StrictMemorySafety

    // Proper acknowledgment with `unsafe`
    let ptr = unsafe UnsafeApi.alloc(count: 5)

    unsafe UnsafeApi.write(ptr, offset: 0, value: 100)
    unsafe UnsafeApi.write(ptr, offset: 1, value: 200)
    unsafe UnsafeApi.write(ptr, offset: 2, value: 300)

    print("UnsafeApi.read(ptr, offset: 0) = \(unsafe UnsafeApi.read(ptr, offset: 0))")
    print("UnsafeApi.read(ptr, offset: 1) = \(unsafe UnsafeApi.read(ptr, offset: 1))")
    print("UnsafeApi.read(ptr, offset: 2) = \(unsafe UnsafeApi.read(ptr, offset: 2))")

    unsafe UnsafeApi.free(ptr, count: 5)

    print()
}

/// Demonstrates CROSS-MODULE SUPPRESSION.
/// SafeBuffer is @safe, so no `unsafe` needed by us.
func demonstrateCrossModuleSuppression() {
    print("--- Cross-Module Suppression ---")
    print("SafeBuffer is @safe - no `unsafe` needed here.\n")

    // No `unsafe` anywhere in this function!
    let buffer = SafeBuffer(count: 5)

    buffer[0] = 100
    buffer[1] = 200
    buffer[2] = 300

    print("buffer[0] = \(buffer[0])")
    print("buffer[1] = \(buffer[1])")
    print("buffer[2] = \(buffer[2])")

    // Safe error handling
    print("buffer.get(100) = \(String(describing: buffer.get(100)))")
    print("buffer.set(100, value: 1) = \(buffer.set(100, value: 1))")

    print()
}

/// Demonstrates propagation chains across module boundaries.
func demonstratePropagationChain() {
    print("--- Propagation Chain (Cross-Module) ---")
    print("Shows @unsafe propagation through call chains.\n")

    // level3Propagate is @unsafe - we should use `unsafe`
    let ptr1 = unsafe PropagationChain.level3Propagate()
    print("level3Propagate() returned pointer (unsafe call)")
    unsafe PropagationChain.cleanup(ptr1)

    // level3Suppress is @safe - no `unsafe` needed
    let ptr2 = PropagationChain.level3Suppress()
    print("level3Suppress() returned pointer (safe call)")
    unsafe PropagationChain.cleanup(ptr2)

    print()
}

/// Demonstrates SNEAKY functions - the unique Swift permissiveness.
func demonstrateSneakyApi() {
    print("--- Sneaky API (Swift Permissiveness) ---")
    print("These functions hide @unsafe calls - we don't need `unsafe`!\n")

    // sneakyAllocate calls @unsafe functions internally
    // But it's NOT @unsafe, so we call it like a normal function!
    // The library author saw warnings, but we don't.
    let arr = sneakyAllocate(count: 5)
    print("sneakyAllocate(count: 5) = \(arr)")

    // properAllocate is @safe - properly acknowledges internal unsafety
    let arr2 = properAllocate(count: 5)
    print("properAllocate(count: 5) = \(arr2)")

    // sneakyRetain - not @unsafe, but does unsafe things
    class MyObject { var value = 42 }
    let obj = MyObject()

    // No `unsafe` needed - the unsafety is HIDDEN from us!
    let ptr = sneakyRetain(obj)
    print("sneakyRetain (no unsafe needed): \(unsafe ptr)")

    // properRetain IS @unsafe, so we need `unsafe`
    let ptr2 = unsafe properRetain(obj)
    print("properRetain (unsafe required): \(unsafe ptr2)")

    print()
}

func printSummary() {
    print("--- Summary: Swift Propagation ---")
    print("CROSS-FUNCTION: @unsafe calls should use `unsafe` (warning if not).")
    print("CROSS-MODULE: Same rule applies across module boundaries.")
    print("WARNINGS ONLY: StrictMemorySafety produces warnings, not errors.")
    print("HIDDEN WARNINGS: Library warnings not visible to consumers.")
    print("SNEAKY ALLOWED: Can hide @unsafe calls - Rust forbids this!")
    print("OPT-IN: StrictMemorySafety must be enabled per-module.")
}
