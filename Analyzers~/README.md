# EcsTask Analyzers

Roslyn analyzers that catch wrong use of `EcsTask` while the code compiles. They need only `EcsTask`,
so they run in every assembly that references it. Compile time only: no runtime cost.

Build (from the Unity project root; copies the DLL to `Analyzers/`):
`dotnet build Packages/ecs-task/Analyzers~/LeoECSLiteExtensions.EcsTask.Analyzers -c Release`

## Rules

| Id | Severity | What it catches |
|---|---|---|
| LEOECS103 | Warning | Awaited call in `EcsTask` code that could take the `CancellationToken` but does not |
| LEOECS105 | Error | `EcsTask` call used as a statement, without `await` |

```csharp
// LEOECS103: without the token, the owner of the task cannot stop the wait.
await UniTask.Delay(1000);   // ❌ pass cancellationToken: ct

// LEOECS105: exceptions are lost and the code below does not wait.
MoveToCover(ct);       // ❌
await MoveToCover(ct); // ✅
_ = MoveToCover(ct);   // ✅ explicit "fire and forget"
```

`EcsTask<T>` counts as `EcsTask`. A token parameter named `_` means "not needed", so LEOECS103 stays quiet.
To turn a rule off for one case, use `#pragma warning disable LEOECS103` or
`[SuppressMessage("LeoECS.Async", "LEOECS103")]`.

## Directory layout

- `Analyzers~/LeoECSLiteExtensions.EcsTask.Analyzers/`: the sources. Unity ignores folders ending in `~`.
  - `Rules.cs`: ids, messages and severities.
  - `EcsTaskSymbols.cs`: the EcsTask and CancellationToken types.
  - `AsyncCode.cs`: finds awaits and async functions.
  - `*Analyzer.cs`: one file per rule.
- `Analyzers~/LeoECSLiteExtensions.EcsTask.Analyzers.Tests/`: NUnit tests. The stubs have no ECS types on purpose.
- `Analyzers/LeoECSLiteExtensions.EcsTask.Analyzers.dll`: the compiled analyzer that Unity loads. Its `.meta`
  has the `RoslynAnalyzer` label and is disabled for every platform. Do not replace or regenerate that `.meta` file.

## Build and test

Run from the Unity project root:

```
dotnet test Packages/ecs-task/Analyzers~/LeoECSLiteExtensions.EcsTask.Analyzers.Tests
dotnet build Packages/ecs-task/Analyzers~/LeoECSLiteExtensions.EcsTask.Analyzers -c Release
```

The project targets `netstandard2.0` and references `Microsoft.CodeAnalysis.CSharp` 4.3.1 to stay compatible
with Unity's analyzer loading.
