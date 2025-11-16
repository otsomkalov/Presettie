---
description: Instructions for unit tests
applyTo: '**/*.Tests/*.fs'
---

- Prefer class `type` instead of module for unit tests
- Do not add `Tests` suffix to types with unit tests
- Use `Moq` library to mock interfaces
- Declare and initialize mocks as `type` fields
- Use assertion checks from `FsUnit` and `FsUnit.xUnit` libraries
- Setup mocks before entering `task` computational expression
- Prefer `VerifyAll` and `VerifyNoOtherCalls` methods call on `IMock` instead of `Verify` method
- Do not add `Mock` suffix to mock variable names
- Prefer `ReturnsAsync` method for setting up mock returns