// C# Memory Safety - Attribute-Based Model
//
// ALTERNATIVE DESIGN: Use attributes for propagation instead of
// changing the meaning of the `unsafe` keyword.
//
// In this model:
// - `unsafe` keyword keeps its current meaning (pointer types only)
// - [RequiresUnsafe] marks methods that are semantically unsafe
// - [SuppressUnsafe] explicitly acknowledges and contains unsafety
//
// Benefits:
// - More backward compatible
// - Clear separation between pointer unsafety and semantic unsafety
// - Existing code using `unsafe` for pointers doesn't change
// - More explicit about WHY something is unsafe (via attribute message)

using System;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Indicates that a member is semantically unsafe and requires
/// callers to acknowledge the unsafety.
///
/// <para>
/// When MemorySafetyRules is enabled, calling a method marked with
/// this attribute produces a warning/error unless the caller:
/// <list type="bullet">
/// <item>Is in an <c>unsafe</c> block</item>
/// <item>Is marked with <c>[RequiresUnsafe]</c> (propagation)</item>
/// <item>Uses <c>[SuppressUnsafe]</c> (explicit suppression)</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Key difference from `unsafe` keyword:</b>
/// The `unsafe` keyword applies to POINTER TYPES.
/// This attribute applies to SEMANTIC UNSAFETY (memory safety violations
/// that don't necessarily involve pointers, like Unsafe.As).
/// </para>
/// </summary>
/// <example>
/// <code>
/// // Library code - marks method as requiring unsafe acknowledgment
/// [RequiresUnsafe("Reinterprets memory without type checking")]
/// public static T As&lt;T&gt;(object o) { ... }
///
/// // Consumer - must acknowledge
/// void Caller()
/// {
///     unsafe { var x = Unsafe.As&lt;int&gt;(obj); }  // OK: unsafe block
/// }
///
/// [RequiresUnsafe]
/// void CallerPropagate()
/// {
///     var x = Unsafe.As&lt;int&gt;(obj);  // OK: propagates to callers
/// }
/// </code>
/// </example>
[AttributeUsage(
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Field |
    AttributeTargets.Constructor,
    Inherited = false,
    AllowMultiple = false)]
public sealed class RequiresUnsafeAttribute : Attribute
{
    /// <summary>
    /// Creates a new RequiresUnsafeAttribute.
    /// </summary>
    public RequiresUnsafeAttribute() { }

    /// <summary>
    /// Creates a new RequiresUnsafeAttribute with a message.
    /// </summary>
    /// <param name="message">Describes why this member is unsafe.</param>
    public RequiresUnsafeAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets or sets a message describing why this member is unsafe
    /// and what callers must ensure.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets a URL with more information about the safety requirements.
    /// </summary>
    public string? Url { get; set; }
}

/// <summary>
/// Explicitly suppresses the unsafe requirement at this call site.
///
/// <para>
/// Use this when you want to contain unsafety without propagating it
/// and without using a full <c>unsafe</c> block.
/// </para>
///
/// <para>
/// This is similar to <c>unsafe { }</c> blocks but:
/// <list type="bullet">
/// <item>Can be applied to a single statement/expression</item>
/// <item>Doesn't enable pointer operations</item>
/// <item>More explicit about intent (suppressing attribute, not enabling pointers)</item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// <code>
/// void SafeWrapper()
/// {
///     // Suppress just for this call - we've verified it's safe
///     [SuppressUnsafe("Array types are compatible")]
///     var result = Unsafe.As&lt;string[], object[]&gt;(strings);
/// }
/// </code>
/// </example>
/// <remarks>
/// NOTE: C# doesn't currently support attributes on statements/expressions.
/// This would require language support. For now, use unsafe blocks.
/// This attribute is shown here to illustrate the design concept.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Method |
    AttributeTargets.Property,
    Inherited = false,
    AllowMultiple = false)]
public sealed class SuppressUnsafeAttribute : Attribute
{
    /// <summary>
    /// Creates a new SuppressUnsafeAttribute.
    /// </summary>
    public SuppressUnsafeAttribute() { }

    /// <summary>
    /// Creates a new SuppressUnsafeAttribute with a justification.
    /// </summary>
    /// <param name="justification">Explains why the suppression is safe.</param>
    public SuppressUnsafeAttribute(string justification)
    {
        Justification = justification;
    }

    /// <summary>
    /// Gets or sets the justification for why this suppression is safe.
    /// </summary>
    public string? Justification { get; set; }
}
