module Domain.Tests.Extensions

open Xunit

let inline equivalent expected actual = Assert.Equivalent(expected, actual)