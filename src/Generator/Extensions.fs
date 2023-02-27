﻿namespace Generator.Extensions

open System.Runtime.CompilerServices

module ServiceCollection =
  open Microsoft.Extensions.DependencyInjection

  [<Extension>]
  type IServiceCollection =
    [<Extension>]
    static member AddSingletonFunc<'TFunc, 'TDep when 'TFunc: not struct>
      (
        sc: Microsoft.Extensions.DependencyInjection.IServiceCollection,
        factory
      ) =
      sc.AddSingleton<'TFunc>(fun sp ->
        let service = sp.GetRequiredService<'TDep>()

        factory service)

    [<Extension>]
    static member AddSingletonFunc<'TFunc, 'TDep1, 'TDep2 when 'TFunc: not struct>
      (
        sc: Microsoft.Extensions.DependencyInjection.IServiceCollection,
        factory
      ) =
      sc.AddSingleton<'TFunc>(fun sp ->
        let service1 = sp.GetRequiredService<'TDep1>()
        let service2 = sp.GetRequiredService<'TDep2>()

        factory service1 service2)

    [<Extension>]
    static member AddSingletonFunc<'TFunc, 'TDep1, 'TDep2, 'TDep3 when 'TFunc: not struct>
      (
        sc: Microsoft.Extensions.DependencyInjection.IServiceCollection,
        factory
      ) =
      sc.AddSingleton<'TFunc>(fun sp ->
        let service1 = sp.GetRequiredService<'TDep1>()
        let service2 = sp.GetRequiredService<'TDep2>()
        let service3 = sp.GetRequiredService<'TDep3>()

        factory service1 service2 service3)
