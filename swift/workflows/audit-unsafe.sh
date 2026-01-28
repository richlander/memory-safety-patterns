#!/bin/bash
# Swift Memory Safety Auditing Workflow
#
# This script demonstrates Swift 6.2's memory safety checking capabilities.
# These show the current state of Swift's auditing - notably the gaps.
#
# See: https://github.com/swiftlang/swift-evolution/blob/main/proposals/0458-strict-memory-safety.md

set -e
cd "$(dirname "$0")/.."

echo "=== Swift 6.2 Memory Safety Auditing ==="
echo ""

# 1. Check Package.swift for strictMemorySafety opt-in
echo "--- Package.swift: StrictMemorySafety opt-in ---"
echo ""
if grep -q "strictMemorySafety" Package.swift; then
    echo "✅ .strictMemorySafety() is enabled"
    grep -n "strictMemorySafety" Package.swift | sed 's/^/   /'
else
    echo "❌ .strictMemorySafety() not found - unsafe code not tracked"
fi
echo ""

# 2. Count @unsafe annotations
echo "--- @unsafe annotation count ---"
echo ""
unsafe_count=$(grep -r "@unsafe" --include="*.swift" Sources/ 2>/dev/null | wc -l | tr -d ' ')
safe_override=$(grep -r "@safe" --include="*.swift" Sources/ 2>/dev/null | wc -l | tr -d ' ')
unsafe_expr=$(grep -r "unsafe {" --include="*.swift" Sources/ 2>/dev/null | wc -l | tr -d ' ')
echo "  @unsafe annotations: $unsafe_count"
echo "  @safe overrides: $safe_override"
echo "  unsafe { } expressions: $unsafe_expr"
echo ""

# 3. Build with warnings to show what the compiler catches
echo "--- Compiler warnings (strict memory safety) ---"
echo ""
echo "Building to capture memory safety warnings..."
echo ""
swift build 2>&1 | grep -i "unsafe\|safety\|warning" | head -20 || echo "(No warnings captured)"
echo ""

# 4. Demonstrate the gap: per-module, not per-binary
echo "=== Swift 6.2 Auditing Gaps ==="
echo ""
echo "Swift 6.2's StrictMemorySafety has limitations compared to Rust:"
echo ""
echo "1. PER-MODULE OPT-IN:"
echo "   Each target must individually enable .strictMemorySafety()"
echo "   There's no project-wide or binary-wide safety assertion"
echo ""
echo "2. WARNINGS, NOT ERRORS:"
echo "   Unsafe code produces warnings, not compilation errors"
echo "   Code compiles and runs even without acknowledgment"
echo ""
echo "3. NO DEPENDENCY TREE AUDITING:"
echo "   No equivalent to cargo-geiger for Swift packages"
echo "   Can't easily answer: 'Do my dependencies use unsafe code?'"
echo ""
echo "4. NO BINARY METADATA:"
echo "   Compiled .swiftmodule doesn't record safety posture"
echo "   Can't audit a binary after the fact"
echo ""
echo "5. LIBRARY-FIRST PROBLEM:"
echo "   Consumers don't see warnings until libraries add @unsafe"
echo "   If a dependency hasn't adopted SE-0458, you get no warnings"
echo ""

echo "=== Summary ==="
echo ""
echo "Swift 6.2 StrictMemorySafety provides:"
echo "  ✅ Compiler warnings for unacknowledged unsafe code"
echo "  ✅ @unsafe/@safe annotations for API contracts"
echo "  ✅ Per-module opt-in via Package.swift"
echo ""
echo "But lacks:"
echo "  ❌ Dependency tree unsafe auditing"
echo "  ❌ Binary-level safety assertions"
echo "  ❌ Errors (only warnings)"
echo "  ❌ Ecosystem-wide enforcement"
echo ""
echo "Rating: ⚠️ Partial - good foundation, but gaps in auditing"
