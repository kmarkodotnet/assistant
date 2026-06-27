using FamilyOs.Application.Common.Errors;

namespace FamilyOs.Infrastructure.Ai.Providers;

public sealed class AiProviderNotAllowedException : DomainException
{
    public AiProviderNotAllowedException()
        : base(
            message: "AI provider not allowed in current privacy mode.",
            userMessage: "Az AI szolgáltató nem engedélyezett az aktuális adatvédelmi módban.")
    {
    }
}
