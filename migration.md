# Migration Strategies for Memory Safety

This document compares migration paths for strengthening memory safety across Rust, Swift, and C#. The difficulty of migration correlates inversely with the strength of the original baseline.

## Overview

| Language | Baseline | Migration Scope | Compiler Aid |
|----------|----------|-----------------|--------------|
| **Rust** | Strong (unsafe fn requires unsafe to call) | Minor (explicit blocks inside unsafe fn) | Incremental lint with `allow` |
| **Swift** | Weak (no enforcement) | Significant (add @unsafe annotations) | Warnings only, opt-in per module |
| **C#** | Weak (pointer types only) | Significant (annotate semantic unsafety) | Analyzers/fixers opportunity |

---

## Rust: Minor Migration

### The Change (RFC 2585)

Before Rust 1.52, the body of an `unsafe fn` was implicitly an unsafe block:

```rust
// Old: entire body is implicitly unsafe
unsafe fn old_style(ptr: *mut i32) {
    *ptr = 42;  // No explicit unsafe block needed
}
```

After enabling `unsafe_op_in_unsafe_fn`, explicit blocks are required:

```rust
// New: must explicitly acknowledge each unsafe operation
unsafe fn new_style(ptr: *mut i32) {
    // SAFETY: caller guarantees ptr is valid and aligned
    unsafe { *ptr = 42; }
}
```

### Why This Was Minor

1. **Strong baseline**: Rust already required `unsafe` to *call* unsafe functions
2. **No new annotations needed**: existing `unsafe fn` signatures unchanged
3. **Internal only**: changes are within function bodies, not at API boundaries
4. **Incremental adoption**: lint can be enabled per-crate or per-function

### Compiler-Aided Migration

```rust
// Step 1: Enable the lint as a warning (Rust 1.52+)
#![warn(unsafe_op_in_unsafe_fn)]

// Step 2: Compiler points to exact locations needing unsafe blocks
// warning: unnecessary `unsafe` block
//   --> src/lib.rs:42:5
//    |
// 42 |     *ptr = value;
//    |     ^^^^^^^^^^^^ this operation requires unsafe

// Step 3: Add blocks incrementally, with SAFETY comments
// Step 4: Promote to deny when ready
#![deny(unsafe_op_in_unsafe_fn)]
```

### Migration Effort: Low

- Compiler identifies every location
- Changes are mechanical (wrap in `unsafe {}`)
- No cross-crate coordination needed
- Can migrate one function at a time

---

## Swift: Significant Migration

### The Change (SE-0458)

Swift 6.2 introduces `StrictMemorySafety` mode with `@unsafe` and `@safe` annotations:

```swift
// Must now be marked @unsafe
@unsafe
public func dangerousOperation(_ ptr: UnsafeMutablePointer<Int>) {
    ptr.pointee = 42
}

// Callers must acknowledge with unsafe expression
let result = unsafe dangerousOperation(ptr)
```

### Why This Is Significant

1. **Weak baseline**: Previously no compile-time enforcement of unsafe propagation
2. **Annotation burden**: Every unsafe API needs `@unsafe` annotation
3. **Ecosystem coordination**: Libraries must update before consumers benefit
4. **Warnings only**: Not errors, so can be ignored

### Compiler-Aided Migration

```swift
// Step 1: Enable StrictMemorySafety in Package.swift
swiftSettings: [
    .enableExperimentalFeature("StrictMemorySafety")
]

// Step 2: Compiler warns about unacknowledged unsafe calls
// warning: expression uses unsafe constructs but is not marked with 'unsafe'
//   --> Sources/MyLib/File.swift:42:5
//    |
// 42 |     ptr.pointee = value
//    |     ^^^^^^^^^^^^^^^^^^

// Step 3: Either add @unsafe to propagate, or unsafe to suppress
@unsafe  // Propagate to callers
func wrapper(_ ptr: UnsafeMutablePointer<Int>) {
    ptr.pointee = 42
}

// OR

@safe  // Suppress - we take responsibility
func safeWrapper(_ ptr: UnsafeMutablePointer<Int>) {
    unsafe { ptr.pointee = 42 }
}
```

### Migration Challenges

1. **Library-first problem**: Consumers don't see warnings until libraries adopt `@unsafe`
2. **Hidden unsafety**: The "sneaky" pattern (calling @unsafe without acknowledgment) still compiles
3. **No automatic fixers**: Must manually audit code for unsafe operations
4. **Semantic judgment**: Deciding what "should" be @unsafe requires understanding, not just pattern matching

### Migration Effort: Medium-High

- Requires semantic understanding of what's unsafe
- Libraries must migrate before consumers benefit
- Warnings can be ignored indefinitely
- No ecosystem-wide enforcement mechanism

---

## C#: Significant Migration with Tooling Opportunity

### The Proposed Changes

Two models under consideration for .NET 11+:

**Model 1: Keyword-based** (`unsafe` modifier on members)
```csharp
// unsafe modifier propagates to callers
public unsafe void DangerousMethod() { }

// Callers must be in unsafe context
unsafe { DangerousMethod(); }
```

**Model 2: Attribute-based** (`[RequiresUnsafe]`)
```csharp
[RequiresUnsafe("Reinterprets memory without type checking")]
public static T As<T>(object o) { ... }

// Callers must acknowledge
unsafe { var x = Unsafe.As<Foo>(obj); }
```

### Why This Is Significant

1. **Weak baseline**: Only pointer types currently propagate unsafety
2. **Semantic gap**: `Unsafe.As`, `Marshal.*`, etc. don't require unsafe today
3. **BCL changes**: Many existing APIs need annotation
4. **Breaking change**: Code that compiled safely now requires unsafe

### Compiler and Tooling Opportunities

C#'s Roslyn infrastructure provides exceptional opportunities for automated migration:

#### 1. Detection Analyzers

```csharp
// Analyzer: Detect calls to known-unsafe APIs outside unsafe context
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnsafeApiCallAnalyzer : DiagnosticAnalyzer
{
    // Flags: Unsafe.As, Unsafe.Add, Marshal.*, etc.
    // when called outside unsafe block/method
}

// Analyzer: Detect methods that SHOULD be marked unsafe
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShouldBeUnsafeAnalyzer : DiagnosticAnalyzer
{
    // Flags methods that:
    // - Call [RequiresUnsafe] methods without suppressing
    // - Return pointers obtained from unsafe operations
    // - Have unsafe semantics (type punning, etc.)
}
```

#### 2. Code Fixers

```csharp
// Fixer: Wrap call in unsafe block (suppress)
// Before:
var x = Unsafe.As<int, float>(ref value);

// After (fixer applied):
var x = unsafe { return Unsafe.As<int, float>(ref value); };
// Note: C# doesn't have unsafe expressions yet, so this would be:
float x;
unsafe { x = Unsafe.As<int, float>(ref value); }
```

```csharp
// Fixer: Add [RequiresUnsafe] to method (propagate)
// Before:
public T ReinterpretAs<T>(object o) {
    return Unsafe.As<T>(o);
}

// After (fixer applied):
[RequiresUnsafe("Delegates to Unsafe.As which bypasses type safety")]
public T ReinterpretAs<T>(object o) {
    return Unsafe.As<T>(o);
}
```

#### 3. Bulk Migration Tools

```bash
# Hypothetical dotnet tool for migration
dotnet memory-safety analyze MyProject.csproj
# Output:
# Found 47 calls to unsafe APIs outside unsafe context:
#   - Unsafe.As: 23 calls in 12 files
#   - Marshal.PtrToStructure: 15 calls in 8 files
#   - Unsafe.Add: 9 calls in 5 files
#
# Suggested actions:
#   - 31 calls should be wrapped in unsafe blocks (suppression)
#   - 12 methods should be marked [RequiresUnsafe] (propagation)
#   - 4 calls need manual review

dotnet memory-safety fix MyProject.csproj --strategy=suppress
# Automatically wraps calls in unsafe blocks

dotnet memory-safety fix MyProject.csproj --strategy=propagate
# Automatically adds [RequiresUnsafe] to calling methods
```

#### 4. IDE Integration

| Feature | Description |
|---------|-------------|
| **Lightbulb suggestions** | "This call requires unsafe context" with fix options |
| **Bulk actions** | "Add unsafe to all calls in file/project" |
| **Propagation visualization** | Show call graph of unsafe propagation |
| **Safety audit report** | List all unsafe entry points in assembly |

### Analyzer Rules (Proposed IDs)

| Rule ID | Description | Default |
|---------|-------------|---------|
| `MEM001` | Call to [RequiresUnsafe] method outside unsafe context | Error |
| `MEM002` | Method calls unsafe API but isn't marked [RequiresUnsafe] | Warning |
| `MEM003` | Unsafe block contains no unsafe operations | Info |
| `MEM004` | [RequiresUnsafe] method has no unsafe operations | Warning |
| `MEM005` | Public method returns pointer without [RequiresUnsafe] | Warning |
| `MEM006` | Consider using safe alternative (e.g., Span instead of pointer) | Suggestion |

### Migration Strategies for C#

#### Strategy 1: Gradual Opt-In (Recommended)

```xml
<!-- Project opts into new rules -->
<PropertyGroup>
  <MemorySafetyRules>true</MemorySafetyRules>
  <TreatMemorySafetyWarningsAsErrors>false</TreatMemorySafetyWarningsAsErrors>
</PropertyGroup>
```

1. Enable rules as warnings
2. Run analyzers to identify scope
3. Fix high-priority items (public APIs)
4. Gradually increase strictness
5. Eventually treat as errors

#### Strategy 2: Outside-In

1. Annotate public API surface first (`[RequiresUnsafe]`)
2. Internal implementation can remain unannotated initially
3. Consumers see correct contracts immediately
4. Clean up internals over time

#### Strategy 3: Inside-Out

1. Start with leaf functions (lowest level)
2. Propagate annotations upward through call graph
3. Tools can automate much of this
4. Results in minimal unsafe surface area

### Who Needs to React?

The migration burden falls primarily on **library developers**, not application developers:

1. **Library developers** will need to:
   - Audit their APIs for unsafe operations
   - Decide whether to propagate (`[RequiresUnsafe]`) or suppress (safe wrapper)
   - Most will be biased toward **exposing safe APIs** when reasonably possible
   - This is the desired outcome: unsafe code concentrated in well-audited libraries

2. **Application developers** will largely benefit without action:
   - Libraries they consume will expose safe APIs
   - Compiler will warn/error if they accidentally use unsafe APIs
   - Only need to react if they have direct unsafe code (uncommon)

This mirrors the Rust ecosystem where most application code never touches `unsafe`,
relying instead on safe abstractions provided by libraries.

### The Keyword vs Attribute Tension

A key difference between the two models is how they handle existing code:

**Keyword model (stronger)**

The `unsafe` keyword on a method signature becomes the propagation choice:
- `unsafe void Foo() { ... }` → propagates to callers
- `void Foo() { unsafe { ... } }` → suppresses (blocks inside, not on signature)

This **forces a decision** on every existing `unsafe` method signature:
1. Keep `unsafe` on signature → now propagates (breaking change to callers)
2. Remove `unsafe` from signature, keep internal blocks → suppresses (asserting safety)

No existing `unsafe` method passes through unexamined. The migration is more disruptive
but leaves no ambiguity.

**Attribute model (gentler)**

Existing `unsafe` keyword meaning unchanged. Propagation is opt-in via `[RequiresUnsafe]`.
Suppression remains implicit—existing code continues to compile without changes.

This is easier to migrate but weaker: you can ignore the new attributes entirely.

**The explicit choice problem**

Neither model perfectly solves the "how does the compiler know it's been analyzed?" question.
The keyword model forces review of `unsafe` signatures, but methods with only internal
`unsafe` blocks (the suppression case) still pass through silently.

The surefire approach would be an attribute that requires an explicit boolean choice:

```csharp
// Explicit propagation
[Unsafe(propagates: true, Reason = "Caller must ensure pointer validity")]
public void DangerousMethod() { ... }

// Explicit suppression - author asserts obligations are discharged
[Unsafe(propagates: false, Reason = "Bounds checked, memory owned by this class")]
public void SafeWrapper() { unsafe { ... } }

// No attribute on method with unsafe blocks → compiler warning
// "Method contains unsafe code but lacks [Unsafe] annotation.
//  Specify whether this propagates or suppresses unsafety."
```

This forces every method touching unsafe code to make an explicit, documented choice.
Whether such an attribute is adopted remains an open question—it's more verbose but
eliminates ambiguity about author intent.

### Rollout Strategy

The most likely rollout sequence:

1. **Preview releases**: Keyword model (`unsafe` on members) ships first
2. **Feedback period**: Gather real-world migration experience
3. **Attribute consideration**: `[RequiresUnsafe]` evaluated for semantic unsafety cases
4. **BCL annotation**: Standard library APIs annotated (required for ecosystem benefit)

### Migration Effort: Medium (with tooling)

- Roslyn analyzers can identify 90%+ of issues automatically
- Code fixers can apply mechanical fixes
- Semantic decisions still require human judgment
- BCL must ship with annotations for ecosystem to benefit
- **Library authors bear most of the burden; app developers mostly benefit**

---

## Comparison Summary

| Aspect | Rust | Swift | C# |
|--------|------|-------|-----|
| **Baseline strength** | Strong | Weak | Weak |
| **Annotation needed** | None (internal only) | @unsafe/@safe | [RequiresUnsafe] or unsafe modifier |
| **Enforcement** | Error | Warning | Error (proposed) |
| **Tooling support** | Compiler lint | Compiler warnings | Analyzers + fixers |
| **Incremental adoption** | Per-function | Per-module | Per-project |
| **Breaking changes** | None to API | None (additive) | Potentially breaking |
| **Ecosystem coordination** | Not needed | Libraries first | BCL + libraries |

---

## Recommendations

### For Language Designers

1. **Provide incremental adoption paths**: Allow projects to opt-in gradually
2. **Ship analyzers with the feature**: Don't rely on third-party tooling
3. **Annotate standard libraries first**: Ecosystem can't migrate without this
4. **Consider warning-to-error progression**: Start permissive, tighten over time

### For Library Authors

1. **Audit unsafe usage early**: Don't wait for enforcement
2. **Document safety contracts**: Even without compiler enforcement
3. **Prefer safe abstractions**: Minimize exposed unsafe surface
4. **Test with strict modes enabled**: Catch issues before consumers do

### For Application Developers

1. **Enable strict modes in CI**: Catch regressions
2. **Prefer suppression over propagation**: Contain unsafety at boundaries
3. **Use analyzers proactively**: Don't wait for language enforcement
4. **Track unsafe usage**: Know where your risks are

---

## References

- [Rust RFC 2585: unsafe_op_in_unsafe_fn](https://github.com/rust-lang/rust/issues/55607)
- [Swift SE-0458: Strict Memory Safety](https://github.com/swiftlang/swift-evolution/blob/main/proposals/0458-strict-memory-safety.md)
- [C# Memory Safety Design](https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/memory-safety.md)
- [C# Caller Unsafe Proposal](https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/caller-unsafe.md)
