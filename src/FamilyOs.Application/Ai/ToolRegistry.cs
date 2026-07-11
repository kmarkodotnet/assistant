using System.Diagnostics.CodeAnalysis;
using FamilyOs.Application.Abstractions.Ai;

namespace FamilyOs.Application.Ai;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _byName;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        All = tools.ToList();
        _byName = All.ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<ITool> All { get; }

    public bool TryGet(string name, [MaybeNullWhen(false)] out ITool tool) => _byName.TryGetValue(name, out tool);
}
