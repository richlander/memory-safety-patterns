// C# Memory Safety - RequiresUnsafeAttribute (Future)
//
// This attribute is planned for .NET 11+ to mark members that require
// callers to use an unsafe context.
//
// From the design doc:
// "When the compilation property 'EnableRequiresUnsafe' is set to true,
// the `unsafe` keyword on C# members would require that their uses
// appear in an unsafe context."
//
// In actual .NET 11+, you would use the `unsafe` keyword on members
// and the compiler would enforce this. This attribute is the metadata
// representation that the compiler emits.
//
// See: https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/caller-unsafe.md

using System;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Indicates that a member requires callers to use an unsafe context.
///
/// This attribute is the metadata representation of the <c>unsafe</c>
/// modifier on members when MemorySafetyRules is enabled.
///
/// <para>
/// <b>Global Invariants:</b> Members marked with this attribute may violate:
/// <list type="bullet">
/// <item>Memory safety - accessing memory not managed by the runtime</item>
/// <item>No access to uninitialized memory</item>
/// </list>
/// Callers must ensure these properties are preserved.
/// </para>
/// </summary>
/// <remarks>
/// It is an error to use this attribute directly in C#.
/// Instead, use the <c>unsafe</c> keyword on member declarations.
/// This type exists for cross-language interop and reflection.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Field |
    AttributeTargets.Constructor,
    Inherited = false)]
public sealed class RequiresUnsafeAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an optional message describing the safety requirements.
    /// </summary>
    public string? Message { get; set; }
}
