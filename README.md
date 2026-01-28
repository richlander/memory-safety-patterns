# Memory Safety Examples

This repository demonstrates memory safety patterns across three languages:

- **[C#](csharp/)** - Current model ([csharpnext-keyword/](csharpnext-keyword/) and [csharpnext-attribute/](csharpnext-attribute/) show future directions)
- **[Rust](rust/)** - The gold standard for memory safety
- **[Swift](swift/)** - Swift 6.2+ with strict memory safety

## North Star

Memory safety mechanics exist to serve developer productivity and confidence. See [north-star.md](north-star.md) for the full discussion. When evaluating patterns and language features, we care about:

| Characteristic | What it means |
|----------------|---------------|
| **Consistent scheme** | Safety rules are clearly defined; all APIs are marked correctly throughout |
| **Strong migration** | Transitions between models are lossless, particularly across commits; tools capture state so implicit decisions become explicit |
| **Strong propagation** | Unresolved unsafe usage bubbles up until discharged; if it compiles clean, obligations are met |
| **Strong auditing** | Easy to audit assets for their safety posture; markings persist in compiled artifacts |

## Analysis

Evaluating each language against the [north star](north-star.md) characteristics:

| Characteristic | C# (current) | C# (future) | Rust | Swift 6.2 |
|----------------|--------------|-------------|------|-----------|
| **Consistent scheme** | ⚠️ Partial | ✅ Strong | ✅ Strong | ⚠️ Partial |
| **Strong migration** | N/A | ✅ Planned | ✅ Incremental lint | ⚠️ Warnings only |
| **Strong propagation** | ❌ Pointer types only | ✅ Member annotations | ✅ `unsafe fn` | ✅ `@unsafe` |
| **Strong auditing** | ⚠️ AllowUnsafeBlocks | 🔮 TBD | ✅ Crate metadata | ⚠️ Per-module |

**Rust:** The `unsafe` keyword has a clear, comprehensive definition covering all memory-unsafe operations. The borrow checker enforces ownership and lifetimes at compile time, eliminating use-after-free and data races. Crate-level metadata makes auditing straightforward. Tools like [cargo-geiger](https://github.com/geiger-rs/cargo-geiger) quantify unsafe usage across the entire dependency tree.

**Key gaps in Swift 6.2:** Warnings-only enforcement can result in a false sense of security if warnings are missed or disabled. More significantly, there is no migration tooling to capture state transitions. When enabling strict memory safety mode, developers add `unsafe` keywords to silence warnings, but there's no distinction between "never audited" and "intentionally suppressed after review." Major Apple frameworks like Combine are [unlikely to receive `@unsafe` annotations](https://forums.swift.org/t/se-0458-opt-in-strict-memory-safety-checking/77274), meaning consumers get no warnings when calling unsafe APIs from those libraries. Additionally, precompiled frameworks (how Apple ships Foundation, UIKit, etc.) lose all warning information—you can't audit what you don't compile.

**Key gaps in C# (current):** `Unsafe.As`, `Marshal.*`, and similar APIs are semantically unsafe but don't require `unsafe` blocks. The compiler can't track this unsafety. This is in part because the APIs are not marked correctly and because the definition of `unsafe` is too narrow, focused on pointer operations as opposed to possibility of memory corruption.

**Planned improvements with C# vNext:** The definition of unsafe expands to match Rust and all platform APIs are marked correctly—similar to how nullable annotations were applied pervasively across .NET. Migration tooling ensures lossless state transitions: code that was "memory safety oblivious" (compiled without unsafe acknowledgment) is explicitly marked for audit, preserving intent across commits. See [csharp-migration.md](csharp-migration.md) for details.

**Potential gaps in C# vNext:** Reflection is likely to be unaddressed. The lack of a lifetime/ownership model means the compiler is unable to detect use-after-free issues with ArrayPool. Ownership may be addressed after vNext `unsafe` is delivered.

### Tools examples

**Rust auditing**: See [`rust/workflows/audit-unsafe.sh`](rust/workflows/audit-unsafe.sh) for examples of `cargo-geiger`, `cargo-audit`, and crate-level `#![forbid(unsafe_code)]` policies.

**Swift auditing**: See [`swift/workflows/audit-unsafe.sh`](swift/workflows/audit-unsafe.sh) for details on per-module opt-in, warnings-only enforcement, and lack of dependency tree auditing.

**C# (future) opportunity**: The `// TODO (MSv2)` pattern in `csharpnext-*` shows how migration tooling could mark "memory safety oblivious" code for explicit audit decisions.

### Concepts Demonstrated

The examples in this repo provide evidence for the analysis above:

#### Unsafe Propagation

How unsafety spreads from callee to caller:

- **C# (current)**: Via pointer types in method signatures
- **C# (future)**: Via `unsafe` modifier on methods or `[RequiresUnsafe]` attribute
- **Rust**: Via `unsafe fn` annotation
- **Swift**: Via `@unsafe` annotation

#### Unsafe Suppression

How to contain unsafety and provide safe public APIs:

- Wrap unsafe internals in safe abstractions
- Document safety obligations (discharge comments)
- Provide bounds checking and proper cleanup

#### Span/Slice Types

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

## Unsafe Propagation

When a method has unsafe obligations that **callers must fulfill**, document them clearly:

```csharp
/// CALLER OBLIGATIONS:
/// - Must call Free() exactly once with the returned pointer
/// - Must not use the pointer after calling Free()
/// - Must not read/write beyond [0, count) bounds
public static unsafe int* Alloc(int count)
```

## Unsafe Suppression

When a method **contains and discharges** unsafe obligations internally, document the safety justification:

```csharp
// SAFETY DISCHARGE:
// - Bounds: Explicit check `index < _length` before access
// - Valid memory: ThrowIfDisposed ensures buffer not freed
// - Initialized: Constructor zero-initializes all elements
unsafe { return ((int*)_buffer)[index]; }
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

### C #

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
