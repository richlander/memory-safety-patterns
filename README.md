# Memory Safety Examples

This repository demonstrates memory safety patterns across three languages:

- **C#** - Current model and future directions
- **Rust** - The gold standard for memory safety
- **Swift** - Swift 6.2+ with strict memory safety

## Directory Structure

```
├── csharp/                 # Current C# model (pointer-type propagation)
├── csharpnext-keyword/     # Future C# (unsafe keyword on members)
├── csharpnext-attribute/   # Future C# alternative ([RequiresUnsafe] attribute)
├── rust/                   # Rust memory safety patterns
└── swift/                  # Swift 6.2 memory safety patterns
```

## Key Concepts Demonstrated

### Unsafe Propagation
How unsafety spreads from callee to caller:
- **C# (current)**: Via pointer types in method signatures
- **C# (future)**: Via `unsafe` modifier on methods or `[RequiresUnsafe]` attribute
- **Rust**: Via `unsafe fn` annotation
- **Swift**: Via `@unsafe` annotation

### Unsafe Suppression
How to contain unsafety and provide safe public APIs:
- Wrap unsafe internals in safe abstractions
- Document safety obligations (discharge comments)
- Provide bounds checking and proper cleanup

### Span/Slice Types
Safe, bounds-checked views into contiguous memory:
- **C#**: `Span<T>`, `ReadOnlySpan<T>`
- **Rust**: `&[T]`, `&mut [T]` (slices)
- **Swift**: `Span` (6.2+), `ArraySlice`, `UnsafeBufferPointer`

## The Compelling Case: Returning Spans

The most powerful use of Span-like types is **returning** them from methods,
allowing callers to get safe, bounds-checked views into internal state without copying:

```csharp
// C#
public Span<int> AsSpan() => _data.AsSpan();

// Rust
pub fn as_slice(&self) -> &[i32] { &self.data }

// Swift
func asSlice() -> ArraySlice<Int> { data[...] }
```

## Safety Documentation Patterns

### Where unsafety is SUPPRESSED (discharged):
```csharp
// SAFETY DISCHARGE:
// - Bounds: Explicit check `index < _length` before access
// - Valid memory: ThrowIfDisposed ensures buffer not freed
// - Initialized: Constructor zero-initializes all elements
unsafe { return ((int*)_buffer)[index]; }
```

### Where unsafety PROPAGATES (caller obligations):
```csharp
/// CALLER OBLIGATIONS:
/// - Must call Free() exactly once with the returned pointer
/// - Must not use the pointer after calling Free()
/// - Must not read/write beyond [0, count) bounds
public static unsafe int* Alloc(int count)
```

## Related Resources

### C# Memory Safety Evolution
- [Memory Safety Design PR](https://github.com/dotnet/designs/pull/362) - The design proposal PR
- [Memory Safety in .NET](https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/memory-safety.md) - Overview design document
- [Caller Unsafe Proposal](https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/caller-unsafe.md) - `unsafe` member annotations
- [SDK Memory Safety Enforcement](https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/sdk-memory-safety-enforcement.md) - SDK-level enforcement
- [dotnet/runtime #41418](https://github.com/dotnet/runtime/issues/41418) - Tracking issue for APIs that should require unsafe

### Swift Memory Safety
- [SE-0458 Strict Memory Safety](https://github.com/swiftlang/swift-evolution/blob/main/proposals/0458-strict-memory-safety.md) - Swift Evolution proposal
- [Swift 6.2 Release Notes](https://www.swift.org/blog/swift-6.2-released/) - Span and strict memory safety
- [Strict Memory Safety Diagnostics](https://docs.swift.org/compiler/documentation/diagnostics/strict-memory-safety)

### Rust Memory Safety
- [Unsafe Rust](https://doc.rust-lang.org/book/ch19-01-unsafe-rust.html) - The Rust Programming Language
- [rust-lang/rust #55607](https://github.com/rust-lang/rust/issues/55607) - Tracking issue for RFC 2585 (unsafe_block_in_unsafe_fn)

## Building and Running

### C#
```bash
cd csharp && dotnet run --project MemoryApp
```

### Rust
```bash
cd rust && cargo run
```

### Swift
```bash
cd swift && swift run
```

## Future Directions (C#)

Two models are being considered for .NET 11+. The keyword model is likely to ship
first in preview releases, with the attribute model considered afterwards.

### 1. Keyword Model (`csharpnext-keyword/`)
The `unsafe` keyword on methods propagates to callers:
```csharp
unsafe void DangerousMethod() { }  // Callers need unsafe context
```

### 2. Attribute Model (`csharpnext-attribute/`)
A new `[RequiresUnsafe]` attribute for semantic unsafety:
```csharp
[RequiresUnsafe("Reinterprets memory without type checking")]
public static T As<T>(object o) { }  // Callers need unsafe context
```

The attribute model provides:
- Better backward compatibility
- Clear separation: pointers vs semantic unsafety
- Descriptive messages explaining WHY something is unsafe
