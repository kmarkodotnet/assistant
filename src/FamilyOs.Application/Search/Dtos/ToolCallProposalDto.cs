namespace FamilyOs.Application.Search.Dtos;

/// <summary>api-design.md §16.1 — Command mode response payload.</summary>
public sealed record ToolCallProposalDto(
    string ProposalToken,
    string ToolName,
    string Summary,
    IReadOnlyList<ToolCallParameterDto> Parameters,
    IReadOnlyList<string> Warnings,
    DateTime ExpiresUtc);

public sealed record ToolCallParameterDto(string Label, string Value);
