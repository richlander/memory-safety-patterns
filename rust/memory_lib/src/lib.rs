//! Rust Memory Safety Library
//!
//! This library demonstrates two types of unsafety propagation:
//! 1. Cross-function: within the same module/crate
//! 2. Cross-module: from library to consumer
//!
//! Rust enforces both types equally - `unsafe fn` requires `unsafe` to call
//! regardless of whether the caller is in the same module or a different crate.

use std::alloc::{alloc, dealloc, Layout};

pub mod span_example;

// ============================================================================
// CROSS-FUNCTION PROPAGATION (within this module)
// ============================================================================
// These examples show how unsafety propagates between functions in the SAME
// module. In Rust, this works exactly the same as cross-module - you MUST
// use `unsafe` blocks or mark the calling function as `unsafe fn`.

/// Low-level allocation - marked unsafe, requires caller to use unsafe
unsafe fn raw_alloc(count: usize) -> *mut i32 {
    let layout = Layout::array::<i32>(count).expect("Invalid layout");
    let ptr = alloc(layout) as *mut i32;
    if ptr.is_null() {
        panic!("Allocation failed");
    }
    ptr
}

/// Low-level deallocation - marked unsafe
unsafe fn raw_dealloc(ptr: *mut i32, count: usize) {
    let layout = Layout::array::<i32>(count).expect("Invalid layout");
    dealloc(ptr as *mut u8, layout);
}

/// Mid-level function that PROPAGATES unsafety (still unsafe fn)
///
/// This function calls raw_alloc/raw_dealloc but doesn't add safety.
/// It remains `unsafe fn` because callers still need to uphold invariants.
///
/// CROSS-FUNCTION: Even within the same module, we must acknowledge the unsafe
/// calls - either by being `unsafe fn` ourselves, or using `unsafe {}` blocks.
unsafe fn mid_level_alloc_uninit(count: usize) -> *mut i32 {
    // Calling another unsafe fn in the same module still requires acknowledgment
    raw_alloc(count)
    // Note: no initialization - caller must handle this
}

/// Mid-level function that SUPPRESSES unsafety (safe fn with unsafe internals)
///
/// This function contains unsafety internally but provides a safe interface.
/// The `unsafe {}` block acknowledges we've verified the safety requirements.
fn mid_level_alloc_zeroed(count: usize) -> *mut i32 {
    assert!(count > 0, "Count must be positive");

    // CROSS-FUNCTION propagation contained with unsafe block
    let ptr = unsafe { raw_alloc(count) };

    // Initialize to zero - this makes it safe to read
    for i in 0..count {
        unsafe { ptr.add(i).write(0) };
    }

    ptr
}

// ============================================================================
// CROSS-MODULE PROPAGATION (exported to consumers)
// ============================================================================
// These are the PUBLIC APIs that demonstrate how unsafety propagates to
// code in OTHER crates/modules that depend on this library.

/// PUBLIC UNSAFE API - Propagates unsafety to external consumers
///
/// When a consumer in another crate calls this, they MUST use `unsafe`.
/// The Rust compiler enforces this at the module boundary.
///
/// # Safety
/// - Caller must ensure `count > 0`
/// - Caller must call `unsafe_free` with the same count
/// - Caller must not use pointer after free
pub unsafe fn unsafe_alloc(count: usize) -> *mut i32 {
    // Cross-function call to internal unsafe fn
    let ptr = raw_alloc(count);

    // Initialize to zero
    for i in 0..count {
        ptr.add(i).write(0);
    }
    ptr
}

/// PUBLIC UNSAFE API - Frees memory
pub unsafe fn unsafe_free(ptr: *mut i32, count: usize) {
    raw_dealloc(ptr, count);
}

/// PUBLIC UNSAFE API - Read at offset
pub unsafe fn unsafe_read(ptr: *const i32, offset: usize) -> i32 {
    *ptr.add(offset)
}

/// PUBLIC UNSAFE API - Write at offset
pub unsafe fn unsafe_write(ptr: *mut i32, offset: usize, value: i32) {
    *ptr.add(offset) = value;
}

// ============================================================================
// SAFE PUBLIC API (unsafety suppressed internally)
// ============================================================================

/// PUBLIC SAFE API - SafeBuffer
///
/// This struct suppresses all unsafety internally. External consumers
/// can use it without any `unsafe` blocks.
///
/// CROSS-MODULE: The unsafety does NOT propagate to consumers.
///
/// # Safety Invariants
///
/// This struct maintains the following invariants that make the public API safe:
/// - `ptr` always points to valid memory of size `len * sizeof(i32)`
/// - `len` is immutable and accurately reflects the allocation size
/// - Memory is zero-initialized at construction (safe to read)
/// - Memory is freed exactly once in Drop
pub struct SafeBuffer {
    ptr: *mut i32,
    len: usize,
}

impl SafeBuffer {
    /// Creates a new buffer - NO unsafe required by caller
    ///
    /// # Safety Discharge
    ///
    /// - `mid_level_alloc_zeroed` requires count > 0: ensured by assert
    /// - Memory must be freed: handled by Drop impl
    /// - No use after free: Drop only called once, Rust ownership prevents aliasing
    pub fn new(len: usize) -> Self {
        assert!(len > 0, "Buffer length must be positive");

        // SAFETY DISCHARGE: count > 0 validated above, memory zero-initialized
        let ptr = mid_level_alloc_zeroed(len);

        SafeBuffer { ptr, len }
    }

    pub fn len(&self) -> usize {
        self.len
    }

    pub fn is_empty(&self) -> bool {
        self.len == 0
    }

    /// Safe read with bounds checking
    ///
    /// # Safety Discharge
    ///
    /// - Pointer valid: struct invariant, maintained by construction and Drop
    /// - Bounds: explicit check `index < self.len` before access
    pub fn get(&self, index: usize) -> Option<i32> {
        if index >= self.len {
            return None;
        }
        // SAFETY DISCHARGE: bounds checked above, ptr valid by invariant
        Some(unsafe { unsafe_read(self.ptr, index) })
    }

    /// Safe write with bounds checking
    ///
    /// # Safety Discharge
    ///
    /// - Pointer valid: struct invariant
    /// - Bounds: explicit check before access
    /// - No aliasing: &mut self ensures exclusive access
    pub fn set(&mut self, index: usize, value: i32) -> Result<(), &'static str> {
        if index >= self.len {
            return Err("Index out of bounds");
        }
        // SAFETY DISCHARGE: bounds checked above, ptr valid, exclusive access via &mut self
        unsafe { unsafe_write(self.ptr, index, value) };
        Ok(())
    }

    /// Returns a safe slice view of the entire buffer.
    ///
    /// THE COMPELLING CASE: Returns a slice that provides safe, bounds-checked
    /// access to the underlying memory without copying.
    ///
    /// # Safety Discharge
    ///
    /// - Pointer valid: struct invariant
    /// - Length accurate: self.len matches allocation
    /// - Lifetime: returned slice borrows &self, cannot outlive buffer
    /// - Aliasing: &self ensures no concurrent mutation
    pub fn as_slice(&self) -> &[i32] {
        // SAFETY DISCHARGE: ptr valid for len elements, lifetime tied to &self
        unsafe { std::slice::from_raw_parts(self.ptr, self.len) }
    }

    /// Returns a mutable slice view of the entire buffer.
    ///
    /// # Safety Discharge
    ///
    /// - Pointer valid: struct invariant
    /// - Length accurate: self.len matches allocation
    /// - Lifetime: returned slice borrows &mut self, cannot outlive buffer
    /// - Exclusive access: &mut self ensures no aliasing
    pub fn as_mut_slice(&mut self) -> &mut [i32] {
        // SAFETY DISCHARGE: ptr valid, exclusive access via &mut self
        unsafe { std::slice::from_raw_parts_mut(self.ptr, self.len) }
    }

    /// Returns a slice over a range with bounds checking.
    ///
    /// # Safety Discharge
    ///
    /// - Returns None for invalid ranges (no panic, no UB)
    /// - Valid ranges produce valid slices (subset of valid allocation)
    pub fn get_slice(&self, start: usize, len: usize) -> Option<&[i32]> {
        if start.saturating_add(len) > self.len {
            return None;
        }
        // SAFETY DISCHARGE: bounds validated above
        Some(unsafe { std::slice::from_raw_parts(self.ptr.add(start), len) })
    }
}

impl Drop for SafeBuffer {
    fn drop(&mut self) {
        // Cross-function unsafe call, contained in Drop
        unsafe { unsafe_free(self.ptr, self.len) };
    }
}

unsafe impl Send for SafeBuffer {}
unsafe impl Sync for SafeBuffer {}

// ============================================================================
// DEMONSTRATION: Propagation chains
// ============================================================================

/// Demonstrates a CHAIN of cross-function propagation
///
/// level3 -> level2 -> level1 -> raw_alloc
///
/// At each level, we must either:
/// 1. Be `unsafe fn` (propagate), OR
/// 2. Use `unsafe {}` block (suppress)
pub mod propagation_chain {
    use super::*;

    /// Level 1: Directly calls raw unsafe function
    unsafe fn level1_unsafe() -> *mut i32 {
        raw_alloc(1)
    }

    /// Level 2: Calls level1, propagates unsafety
    unsafe fn level2_unsafe() -> *mut i32 {
        level1_unsafe()
    }

    /// Level 3: Calls level2, propagates unsafety
    /// This is PUBLIC - external code must use unsafe to call
    pub unsafe fn level3_propagate() -> *mut i32 {
        level2_unsafe()
    }

    /// Alternative Level 3: Suppresses unsafety
    /// This is PUBLIC and SAFE - external code needs no unsafe
    pub fn level3_suppress() -> *mut i32 {
        // The buck stops here - we take responsibility
        unsafe { level2_unsafe() }
    }

    /// Clean up helper
    pub unsafe fn cleanup(ptr: *mut i32) {
        raw_dealloc(ptr, 1);
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_cross_method_propagation() {
        // Within this module, we can call internal unsafe functions
        // but we STILL need unsafe blocks
        unsafe {
            let ptr = mid_level_alloc_uninit(5);
            // Must initialize before reading
            for i in 0..5 {
                ptr.add(i).write(i as i32);
            }
            assert_eq!(*ptr, 0);
            raw_dealloc(ptr, 5);
        }
    }

    #[test]
    fn test_safe_buffer() {
        // No unsafe needed - cross-module safety works
        let mut buf = SafeBuffer::new(10);
        buf.set(0, 42).unwrap();
        assert_eq!(buf.get(0), Some(42));
    }
}
