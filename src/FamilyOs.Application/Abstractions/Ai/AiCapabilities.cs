namespace FamilyOs.Application.Abstractions.Ai;

[Flags]
public enum AiCapabilities
{
    None = 0,
    ToolUse = 1,
    JsonMode = 2,
    Streaming = 4,
}
