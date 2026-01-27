//! Rust Memory Safety Library - Slice Examples
//!
//! Rust slices (`&[T]` and `&mut [T]`) are the equivalent of C#'s Span<T>
//! and Swift's Span. They provide:
//!
//! - Safe, bounds-checked access to contiguous memory
//! - Non-owning views (borrowed references)
//! - Zero-copy slicing
//! - Compile-time lifetime guarantees
//!
//! Key difference from C#/Swift: Rust enforces lifetimes at COMPILE TIME,
//! so use-after-free is impossible, not just detected at runtime.

/// Demonstrates basic slice creation from arrays and vectors.
pub fn demonstrate_basic_slices() {
    println!("--- Slices from Arrays and Vectors ---");

    // Array on stack
    let array: [i32; 10] = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    // Create a slice over the entire array - zero copy
    let full_slice: &[i32] = &array;
    println!("Full slice length: {}", full_slice.len());

    // Create a slice over a range - still zero copy
    let slice = &array[2..7]; // elements 3,4,5,6,7
    println!("Slice [2..7]: {:?}", slice);

    // Vector on heap
    let vec = vec![10, 20, 30, 40, 50];
    let vec_slice: &[i32] = &vec;
    println!("Vector as slice: {:?}", vec_slice);

    // Bounds checking - this would panic, not cause UB:
    // let _ = array[100]; // Runtime panic with clear message

    println!();
}

/// Demonstrates mutable slices.
pub fn demonstrate_mutable_slices() {
    println!("--- Mutable Slices ---");

    let mut array = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    println!("Original: {:?}", array);

    // Mutable slice allows modification
    let slice: &mut [i32] = &mut array[2..7];
    slice[0] = 100;
    println!("After slice[0] = 100, array: {:?}", array);

    // Fill a region
    let fill_slice = &mut array[0..3];
    fill_slice.fill(42);
    println!("After filling [0..3] with 42: {:?}", array);

    println!();
}

/// Demonstrates zero-copy slicing operations.
pub fn demonstrate_slicing() {
    println!("--- Zero-Copy Slicing ---");

    let data = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    // Various slice syntaxes - all zero copy
    let first = &data[..3];     // [0, 1, 2]
    let middle = &data[3..7];   // [3, 4, 5, 6]
    let last = &data[7..];      // [7, 8, 9]

    println!("First:  {:?}", first);
    println!("Middle: {:?}", middle);
    println!("Last:   {:?}", last);

    // Split operations
    let (left, right) = data.split_at(5);
    println!("Split at 5 - Left: {:?}, Right: {:?}", left, right);

    // Chunks
    println!("Chunks of 3:");
    for (i, chunk) in data.chunks(3).enumerate() {
        println!("  Chunk {}: {:?}", i, chunk);
    }

    println!();
}

/// Demonstrates slices as function parameters.
pub fn demonstrate_function_parameters() {
    println!("--- Slices as Function Parameters ---");

    let data = [5, 3, 8, 1, 9, 2, 7, 4, 6, 0];
    println!("Original: {:?}", data);
    println!("Sum: {}", sum(&data));

    // Sort a mutable slice
    let mut sortable = [5, 3, 8, 1, 9, 2, 7, 4, 6, 0];
    sort_slice(&mut sortable[2..7]);
    println!("After sorting [2..7]: {:?}", sortable);

    // Process with a closure
    let doubled: Vec<i32> = map_slice(&data, |x| x * 2);
    println!("Doubled: {:?}", doubled);

    println!();
}

/// Demonstrates compile-time lifetime safety.
pub fn demonstrate_lifetime_safety() {
    println!("--- Compile-Time Lifetime Safety ---");

    // This is SAFE - slice lives as long as the data
    let data = vec![1, 2, 3];
    let slice = &data[..];
    println!("Slice of vec: {:?}", slice);

    // This would NOT compile - Rust prevents use-after-free at compile time:
    // let dangling: &[i32];
    // {
    //     let temp = vec![1, 2, 3];
    //     dangling = &temp[..]; // ERROR: `temp` does not live long enough
    // }
    // println!("{:?}", dangling); // Would be use-after-free

    println!("Rust prevents dangling slices at compile time!");
    println!("Unlike C#/Swift Span, no runtime checks needed for lifetime safety.");

    println!();
}

/// Demonstrates safe iteration patterns.
pub fn demonstrate_iteration() {
    println!("--- Safe Iteration ---");

    let data = [10, 20, 30, 40, 50];

    // Immutable iteration
    print!("Values: ");
    for value in &data {
        print!("{} ", value);
    }
    println!();

    // Mutable iteration
    let mut mutable_data = [1, 2, 3, 4, 5];
    for value in &mut mutable_data {
        *value *= 10;
    }
    println!("After multiplying by 10: {:?}", mutable_data);

    // Enumerated iteration
    print!("With indices: ");
    for (i, value) in data.iter().enumerate() {
        print!("[{}]={} ", i, value);
    }
    println!();

    // Windows and overlapping iteration
    println!("Windows of 3:");
    for window in data.windows(3) {
        println!("  {:?}", window);
    }

    println!();
}

/// Demonstrates contrast with unsafe raw pointers.
pub fn demonstrate_contrast_with_pointers() {
    println!("--- Contrast: Slices vs Raw Pointers ---");

    let data = [10, 20, 30, 40, 50];

    // UNSAFE: Raw pointer approach - no bounds checking
    unsafe {
        let ptr = data.as_ptr();
        println!("Pointer access: {}", *ptr.add(2));
        // *ptr.add(100) would compile - undefined behavior!
    }

    // SAFE: Slice approach - bounds checked
    let slice = &data[..];
    println!("Slice access: {}", slice[2]);
    // slice[100] would panic with clear error message

    println!("Raw pointers: No bounds checking, UB possible");
    println!("Slices: Bounds checked, panics on invalid access");
    println!("Slices also have COMPILE-TIME lifetime checking!");

    println!();
}

/// Demonstrates `get` for non-panicking access.
pub fn demonstrate_safe_access() {
    println!("--- Non-Panicking Access with get() ---");

    let data = [1, 2, 3, 4, 5];

    // get() returns Option - no panic on out of bounds
    match data.get(2) {
        Some(value) => println!("data.get(2) = Some({})", value),
        None => println!("data.get(2) = None"),
    }

    match data.get(100) {
        Some(value) => println!("data.get(100) = Some({})", value),
        None => println!("data.get(100) = None (safe, no panic)"),
    }

    // get_mut for mutable access
    let mut mutable = [1, 2, 3];
    if let Some(value) = mutable.get_mut(1) {
        *value = 100;
    }
    println!("After get_mut modification: {:?}", mutable);

    println!();
}

/// THE COMPELLING CASE: Methods that RETURN slices.
///
/// Returning a slice allows callers to get a safe, bounds-checked view
/// into internal state. Rust's lifetime system ensures the slice cannot
/// outlive the data it references - enforced at COMPILE TIME.
pub fn demonstrate_returning_slices() {
    println!("--- Returning Slices (The Compelling Case) ---");

    let container = DataContainer::new(vec![1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

    // Get safe views into the container's internal data
    // The lifetime system ensures these can't outlive `container`
    let first_half = container.first_half();
    let last_half = container.last_half();

    println!("First half: {:?}", first_half);
    println!("Last half: {:?}", last_half);

    // Mutable access - Rust ensures exclusive access at compile time
    let mut mutable_container = DataContainer::new(vec![1, 2, 3, 4, 5]);
    {
        let slice = mutable_container.first_half_mut();
        slice[0] = 100;
    }
    println!("After mutation: {:?}", mutable_container.as_slice());

    // Subslicing with bounds checking
    if let Some(range) = container.get_range(2, 6) {
        println!("Range [2..6]: {:?}", range);
    }

    // The following would NOT compile - Rust prevents use-after-free:
    // let dangling: &[i32];
    // {
    //     let temp_container = DataContainer::new(vec![1, 2, 3]);
    //     dangling = temp_container.as_slice();
    // } // temp_container dropped here
    // println!("{:?}", dangling); // ERROR: borrowed value does not live long enough

    println!("Rust's lifetime system prevents returning dangling slices!");
    println!();
}

// Helper functions that work with slices

fn sum(slice: &[i32]) -> i32 {
    slice.iter().sum()
}

fn sort_slice(slice: &mut [i32]) {
    slice.sort();
}

fn map_slice<F>(slice: &[i32], f: F) -> Vec<i32>
where
    F: Fn(i32) -> i32,
{
    slice.iter().map(|&x| f(x)).collect()
}

/// Runs all slice demonstrations.
pub fn run_all_demonstrations() {
    println!("=== Rust Slice Examples ===");
    println!("Slices provide safe, bounds-checked access to contiguous memory.\n");

    demonstrate_basic_slices();
    demonstrate_mutable_slices();
    demonstrate_slicing();
    demonstrate_function_parameters();
    demonstrate_lifetime_safety();
    demonstrate_iteration();
    demonstrate_safe_access();
    demonstrate_returning_slices();
    demonstrate_contrast_with_pointers();

    println!("--- Summary ---");
    println!("Rust slices (&[T], &mut [T]) provide:");
    println!("- Safe, bounds-checked memory access");
    println!("- Zero-copy slicing and views");
    println!("- COMPILE-TIME lifetime guarantees (unique to Rust)");
    println!("- Can be RETURNED from methods - lifetime-checked!");
    println!("- Rich iteration and transformation APIs");
    println!("- The safe alternative to raw pointer manipulation");
}

/// Example struct that RETURNS slices into its internal data.
///
/// This demonstrates the compelling use case: exposing internal state
/// safely without copying. Rust's lifetime annotations ensure the
/// returned slices cannot outlive the container.
pub struct DataContainer {
    data: Vec<i32>,
}

impl DataContainer {
    pub fn new(data: Vec<i32>) -> Self {
        Self { data }
    }

    pub fn len(&self) -> usize {
        self.data.len()
    }

    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.data.is_empty()
    }

    /// Returns a slice over the entire data.
    ///
    /// SAFETY DISCHARGE: This is safe because:
    /// - The lifetime `'_` ties the returned slice to `&self`
    /// - Rust's borrow checker ensures the slice cannot outlive self
    /// - No unsafe code involved
    pub fn as_slice(&self) -> &[i32] {
        &self.data
    }

    /// Returns a slice over the first half.
    ///
    /// SAFETY DISCHARGE:
    /// - Bounds computed from self.data.len(), always valid
    /// - Lifetime tied to &self by return type
    pub fn first_half(&self) -> &[i32] {
        let mid = self.data.len() / 2;
        &self.data[..mid]
    }

    /// Returns a slice over the last half.
    ///
    /// SAFETY DISCHARGE: Same as first_half.
    pub fn last_half(&self) -> &[i32] {
        let mid = self.data.len() / 2;
        &self.data[mid..]
    }

    /// Returns a mutable slice over the first half.
    ///
    /// SAFETY DISCHARGE:
    /// - &mut self ensures exclusive access (no aliasing)
    /// - Lifetime tied to &mut self
    /// - Bounds are valid
    pub fn first_half_mut(&mut self) -> &mut [i32] {
        let mid = self.data.len() / 2;
        &mut self.data[..mid]
    }

    /// Returns a slice over a specified range, with bounds checking.
    ///
    /// SAFETY DISCHARGE:
    /// - Returns None for invalid ranges (no panic, no UB)
    /// - Uses get() which is bounds-checked
    pub fn get_range(&self, start: usize, end: usize) -> Option<&[i32]> {
        self.data.get(start..end)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_sum() {
        assert_eq!(sum(&[1, 2, 3, 4, 5]), 15);
    }

    #[test]
    fn test_sort_slice() {
        let mut data = [5, 3, 1, 4, 2];
        sort_slice(&mut data);
        assert_eq!(data, [1, 2, 3, 4, 5]);
    }

    #[test]
    fn test_bounds_checking() {
        let data = [1, 2, 3];
        assert_eq!(data.get(0), Some(&1));
        assert_eq!(data.get(10), None);
    }
}
