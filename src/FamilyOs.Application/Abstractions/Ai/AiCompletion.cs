namespace FamilyOs.Application.Abstractions.Ai;

public record AiCompletion(
    string Content,
    int InputTokens,
    int OutputTokens,
    string ModelName);
