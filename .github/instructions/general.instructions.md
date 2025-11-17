---
description: General F# coding conventions
applyTo: '**/*.fs'
---

- Prefer shortened lambdas to full declaration (`_.Method()` over `fun x -> x.Method()`)
- Prefer appending new code to the end of the file
- Use `Task.FromResult()` instead of `task { return ... }`
- Omit parentheses for single-argument functions