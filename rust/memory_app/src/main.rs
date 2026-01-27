//! Rust Memory Safety Application
//!
//! Demonstrates CROSS-MODULE propagation - how unsafety from the library
//! affects code in this separate crate.
//!
//! Key insight: Rust enforces the SAME rules for cross-module as cross-method.
//! If a function is `unsafe fn`, callers MUST use `unsafe` - no exceptions.

use memory_lib::{
    unsafe_alloc, unsafe_free, unsafe_read, unsafe_write,
    SafeBuffer,
    propagation_chain,
};

fn main() {
    println!("=== Rust Memory Safety Demo ===\n");

    demonstrate_cross_module_propagation();
    demonstrate_cross_module_suppression();
    demonstrate_propagation_chain();
    print_summary();
}

/// Demonstrates CROSS-MODULE PROPAGATION
///
/// When calling `unsafe fn` from another crate, we MUST use `unsafe`.
/// The compiler enforces this at the crate boundary.
fn demonstrate_cross_module_propagation() {
    println!("--- Cross-Module Propagation ---");
    println!("Calling unsafe functions from library requires `unsafe` blocks.\n");

    // This would NOT compile without unsafe:
    // let ptr = unsafe_alloc(5);  // ERROR: call to unsafe function

    // We MUST acknowledge the unsafety
    unsafe {
        let ptr = unsafe_alloc(5);

        unsafe_write(ptr, 0, 100);
        unsafe_write(ptr, 1, 200);
        unsafe_write(ptr, 2, 300);

        println!("unsafe_read(ptr, 0) = {}", unsafe_read(ptr, 0));
        println!("unsafe_read(ptr, 1) = {}", unsafe_read(ptr, 1));
        println!("unsafe_read(ptr, 2) = {}", unsafe_read(ptr, 2));

        unsafe_free(ptr, 5);
    }

    println!();
}

/// Demonstrates CROSS-MODULE SUPPRESSION
///
/// SafeBuffer suppresses unsafety internally. We can use it without
/// any `unsafe` blocks - the library took responsibility.
fn demonstrate_cross_module_suppression() {
    println!("--- Cross-Module Suppression ---");
    println!("SafeBuffer contains unsafety - no `unsafe` needed here.\n");

    // No unsafe blocks anywhere in this function!
    let mut buffer = SafeBuffer::new(5);

    buffer.set(0, 100).unwrap();
    buffer.set(1, 200).unwrap();
    buffer.set(2, 300).unwrap();

    println!("buffer.get(0) = {:?}", buffer.get(0));
    println!("buffer.get(1) = {:?}", buffer.get(1));
    println!("buffer.get(2) = {:?}", buffer.get(2));

    // Safe error handling instead of undefined behavior
    println!("buffer.get(100) = {:?}", buffer.get(100));
    println!("buffer.set(100, 1) = {:?}", buffer.set(100, 1));

    println!();
}

/// Demonstrates PROPAGATION CHAINS across module boundaries
fn demonstrate_propagation_chain() {
    println!("--- Propagation Chain (Cross-Module) ---");
    println!("Shows how unsafety chains through multiple levels.\n");

    // level3_propagate is unsafe - we must acknowledge
    unsafe {
        let ptr = propagation_chain::level3_propagate();
        println!("level3_propagate() returned pointer (unsafe call)");
        propagation_chain::cleanup(ptr);
    }

    // level3_suppress is safe - library contained the unsafety
    let ptr = propagation_chain::level3_suppress();
    println!("level3_suppress() returned pointer (safe call)");
    // Note: we'd need unsafe to clean this up properly, which shows
    // the tension between propagation and suppression
    unsafe { propagation_chain::cleanup(ptr); }

    println!();
}

fn print_summary() {
    println!("--- Summary: Rust Propagation ---");
    println!("CROSS-FUNCTION: Within a crate, `unsafe fn` calls require `unsafe`.");
    println!("CROSS-MODULE: Across crates, `unsafe fn` calls require `unsafe`.");
    println!("IDENTICAL RULES: Rust enforces the same for both!");
    println!("NO HIDING: You cannot call unsafe fn without acknowledgment.");
    println!("SUPPRESSION: Use `unsafe {{}}` blocks to contain unsafety.");
}
