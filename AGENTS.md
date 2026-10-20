# Project conventions

- Follow the formatting, naming, and code style rules defined in `.editorconfig`. Treat `.editorconfig` as the source of truth.
- Use `camelCase` for private instance fields without an underscore (`_`) prefix.
- Follow standard C# naming conventions for constants, static fields, types, methods, properties, and parameters, unless overridden by `.editorconfig`.
- Do not use Hungarian notation or prefixes that encode a variable's type or scope.
- Preserve established .NET naming conventions, including the `I` prefix for interfaces (e.g., `IService`).
- Prefer expression-bodied members for simple, single-expression implementations when consistent with `.editorconfig`.
- Insert a blank line before `return` statements, except when `return` is the first statement in a block.
- Insert a blank line after a group of local variable declarations before the next statement.
- Separate control-flow statements and blocks (`if`, `for`, `foreach`, `while`, `do`, `switch`, `try`, etc.) from surrounding statements with blank lines. Do not insert blank lines between related clauses such as `if`/`else`, `try`/`catch`/`finally`, or `do`/`while`.
