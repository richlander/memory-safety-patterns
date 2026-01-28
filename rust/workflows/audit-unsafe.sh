#!/bin/bash
# Rust Memory Safety Auditing Workflow
#
# This script demonstrates Rust's strong auditing capabilities for unsafe code.
# These tools provide evidence for the "Strong auditing" characteristic.
#
# Prerequisites:
#   cargo install cargo-geiger
#   cargo install cargo-audit
#
# See: https://github.com/rust-secure-code/cargo-geiger
# See: https://github.com/rustsec/rustsec/tree/main/cargo-audit

set -e
cd "$(dirname "$0")/.."

echo "=== Rust Unsafe Code Auditing ==="
echo ""

# 1. cargo-geiger: Count unsafe usage in dependency tree
echo "--- cargo-geiger: Unsafe code metrics ---"
echo "Shows unsafe usage in your code AND all dependencies."
echo ""
if command -v cargo-geiger &> /dev/null; then
    cargo geiger --quiet 2>/dev/null || cargo geiger
else
    echo "Install with: cargo install cargo-geiger"
    echo ""
    echo "Example output:"
    echo "    Metric output format: x/y"
    echo "    x = unsafe code used by build"
    echo "    y = total unsafe code found"
    echo ""
    echo "    Functions  Expressions  Impls  Traits  Methods  Dependency"
    echo "    0/0        0/0          0/0    0/0     0/0      memory_lib"
    echo "    0/0        4/4          0/0    0/0     0/0      └── libc 0.2.x"
fi
echo ""

# 2. Check for #![forbid(unsafe_code)] in crate roots
echo "--- Crate-level unsafe policy ---"
echo "Crates can declare their safety posture in lib.rs/main.rs:"
echo ""
for lib in memory_lib/src/lib.rs memory_app/src/main.rs; do
    if [ -f "$lib" ]; then
        echo "  $lib:"
        if grep -q "forbid(unsafe_code)" "$lib"; then
            echo "    #![forbid(unsafe_code)] - NO unsafe allowed"
        elif grep -q "deny(unsafe_code)" "$lib"; then
            echo "    #![deny(unsafe_code)] - unsafe is error (can be overridden)"
        elif grep -q "warn(unsafe_code)" "$lib"; then
            echo "    #![warn(unsafe_code)] - unsafe triggers warning"
        else
            echo "    (no unsafe policy declared - unsafe allowed)"
        fi
    fi
done
echo ""

# 3. Count unsafe blocks in the codebase
echo "--- Unsafe block count ---"
echo "Direct search for unsafe usage in this workspace:"
echo ""
unsafe_count=$(grep -r "unsafe\s*{" --include="*.rs" . 2>/dev/null | wc -l | tr -d ' ')
unsafe_fn_count=$(grep -r "unsafe\s*fn" --include="*.rs" . 2>/dev/null | wc -l | tr -d ' ')
echo "  unsafe { } blocks: $unsafe_count"
echo "  unsafe fn declarations: $unsafe_fn_count"
echo ""

# 4. cargo-audit: Check for known vulnerabilities
echo "--- cargo-audit: Security vulnerabilities ---"
echo "Checks dependencies against RustSec Advisory Database."
echo ""
if command -v cargo-audit &> /dev/null; then
    cargo audit --quiet 2>/dev/null || cargo audit || true
else
    echo "Install with: cargo install cargo-audit"
    echo ""
    echo "Example output:"
    echo "    Fetching advisory database from 'https://github.com/RustSec/advisory-db'"
    echo "    Scanning Cargo.lock for vulnerabilities (X crate dependencies)"
    echo "    No vulnerable packages found."
fi
echo ""

echo "=== Summary ==="
echo ""
echo "Rust provides strong auditing through:"
echo "  1. cargo-geiger: Quantifies unsafe in entire dependency tree"
echo "  2. Crate attributes: #![forbid(unsafe_code)] enforced at compile time"
echo "  3. Crate metadata: Safety posture visible in Cargo.toml and docs"
echo "  4. cargo-audit: CVE tracking for dependencies"
echo ""
echo "This enables confident answers to: 'Does this binary use unsafe code?'"
