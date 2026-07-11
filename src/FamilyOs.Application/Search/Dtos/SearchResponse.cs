namespace FamilyOs.Application.Search.Dtos;

public sealed class SearchResponse
{
    public IReadOnlyList<SearchHit> Hits { get; set; } = [];
    public int TotalCount { get; set; }
    public SearchMode ModeUsed { get; set; }
    public string? Answer { get; set; } // Q&A mode
    public string[]? AnswerSources { get; set; } // cited chunk IDs
    public double? Confidence { get; set; }
    public ToolCallProposalDto? ToolCallProposal { get; set; } // Command mode (ADR-0011)
}
