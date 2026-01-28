# Memory Safety North Star

Memory safety implementations are inherently based on a set of mechanics to drive behavior and generate confidence in large and layered code bases (including external dependencies). It is easy to miss the key characteristics to enable and uphold within the mechanics. The mechanics are in service of those characteristics, which are in service of developer productivity and confidence.

Key characteristics:

- **Consistent well-defined scheme** -- Safety rules are clearly defined and all APIs are marked correctly throughput, particularly in the platform. This requirement equally applies to auto-generated code.
- **Strong migration moment** -- Transitions from one model to another are lossless. Tools fully capture the state transition so that correct auditing can be done and that the safety nature of the project is consistent across commits. For example, a method with unsafe blocks with no unsafe method signature is seen as the suppression case in the new C# model, however, no one made that decision. It's the most dangerous kind of implicit. Tools mark that method as "needs analysis" as the required bookkeeping, maintaining the nature of that method across model transitions.
- **Strong propagation** -- Unresolved unsafe code usage propagates upwards until the point that a safe API is exposed. This is similar to how exceptions are handled, however that's a runtime concern in C#; this is a compile-time concern. If an app compiles doesn't without unsafe code compiler errors, then all obligations have been discharged and you can make strong claims about your app.
- **Strong auditing** -- It is easy to audit assets to determine their safety nature (compiled with safety scheme x and uses unsafe code or not). This includes recording markings in persistent assets, for C# that's `.dll` and `.nupkg` files. This style of marking is already common in the .NET-related assets.

Note: This system is based on trust that safety concerns will be handled with care by the maintainers of your dependencies. It is unwise to take a dependency on components -- even if they only use safe code -- for maintainers that you do not trust. The introduction of stricter safety models doesn't significantly change that.
