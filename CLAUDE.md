# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Code Style

### Philosophy

Be direct and clear. Simple, readable code is the goal. Do not over-engineer, introduce unnecessary abstractions, or write obtuse solutions. If a straightforward implementation exists, use it.

### C# conventions (enforced by `.editorconfig`)

- **Indentation**: tabs, width 4
- **Braces**: always required for `if`/`else`/loops — enforced as error (IDE0011)
- **Namespaces**: file-scoped (`namespace Foo;`) — enforced as warning
- **Naming**:
  - Public types and members: `PascalCase`
  - Private/internal methods: `camelCase`
  - Private fields: `_camelCase` (underscore prefix)
  - Constants: `UPPER_CASE_WITH_UNDERSCORES`
  - Interfaces: `IPascalCase`
  - Locals and parameters: `camelCase`
- **`var`**: use when the type is apparent from the right-hand side
- **Null handling**: use `?.`, `??`, pattern matching — null reference violations are treated as errors
- **Unused values**: discard explicitly with `_ =`; unused parameters are warnings
- **No `this.`** qualifier — omit it

### Preferred patterns

- **Static methods** over instance methods where state is not needed
- **Functional style** — prefer expressions, LINQ, pattern matching, and lambdas over imperative loops where it reads clearly
- **Expression-bodied properties and accessors** where they fit naturally
- **Primary constructors** and collection expressions where they simplify code
- **Switch expressions** over switch statements

### What to avoid

- Over-abstraction: don't create interfaces, base classes, or layers for things that only have one implementation
- Premature generalisation: solve the problem at hand, not hypothetical future problems
- Deeply nested logic: flatten it with early returns or extracted methods
- Verbose patterns where concise ones exist (e.g. don't write `if (x == null)` when `x is null` or `x?.` works)
