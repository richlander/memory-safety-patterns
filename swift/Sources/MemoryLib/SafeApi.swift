// Swift Memory Safety Library - Safe API
//
// This file demonstrates SUPPRESSING unsafety so it doesn't propagate
// to consumers (CROSS-MODULE suppression).
//
// Key techniques in Swift:
// 1. Mark the public API as @safe
// 2. Use `unsafe` expression internally to acknowledge unsafe operations
// 3. Provide bounds checking and proper cleanup

import Foundation

/// A safe wrapper that SUPPRESSES unsafety internally.
///
/// CROSS-MODULE: Consumers can use this without `unsafe` because:
/// 1. The class is marked @safe
/// 2. All @unsafe calls are acknowledged with `unsafe` internally
///
/// This is the Swift equivalent of Rust's safe wrapper pattern.
@safe
public final class SafeBuffer {
    private let ptr: UnsafeMutablePointer<Int>
    private let count: Int

    /// Creates a new buffer. NO `unsafe` required by caller.
    public init(count: Int) {
        precondition(count > 0, "Count must be positive")

        self.count = count

        // CROSS-FUNCTION: We acknowledge the @unsafe call internally
        self.ptr = unsafe UnsafeApi.alloc(count: count)
    }

    deinit {
        // Cleanup - unsafe contained internally
        unsafe UnsafeApi.free(self.ptr, count: self.count)
    }

    /// The number of elements in the buffer.
    public var length: Int {
        return count
    }

    /// Gets the value at the specified index.
    /// Safe - bounds checking prevents undefined behavior.
    public func get(_ index: Int) -> Int? {
        guard index >= 0 && index < count else {
            return nil
        }
        // Unsafety contained - bounds check done above
        return unsafe UnsafeApi.read(self.ptr, offset: index)
    }

    /// Sets the value at the specified index.
    /// Safe - bounds checking prevents undefined behavior.
    @discardableResult
    public func set(_ index: Int, value: Int) -> Bool {
        guard index >= 0 && index < count else {
            return false
        }
        // Unsafety contained - bounds check done above
        unsafe UnsafeApi.write(self.ptr, offset: index, value: value)
        return true
    }

    /// Subscript access with bounds checking.
    public subscript(index: Int) -> Int {
        get {
            guard let value = get(index) else {
                preconditionFailure("Index \(index) out of bounds [0, \(count))")
            }
            return value
        }
        set {
            guard set(index, value: newValue) else {
                preconditionFailure("Index \(index) out of bounds [0, \(count))")
            }
        }
    }
}
