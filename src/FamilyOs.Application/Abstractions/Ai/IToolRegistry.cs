using System.Diagnostics.CodeAnalysis;

namespace FamilyOs.Application.Abstractions.Ai;

public interface IToolRegistry
{
    /// <summary>All registered (whitelisted) tools.</summary>
    IReadOnlyList<ITool> All { get; }

    // [MaybeNullWhen(false)] mirrors Dictionary<TKey,TValue>.TryGetValue's own annotation —
    // keeps the "out ITool tool" shape from ai-pipeline.md §11.1 while satisfying this
    // project's nullable-warnings-as-errors build gate (Directory.Build.props).
    bool TryGet(string name, [MaybeNullWhen(false)] out ITool tool);
}
