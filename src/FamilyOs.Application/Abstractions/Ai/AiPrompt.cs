namespace FamilyOs.Application.Abstractions.Ai;

public record AiPrompt(
    string SystemPrompt,
    string UserPrompt,
    string PromptId,
    string PromptVersion);
