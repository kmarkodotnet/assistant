using MediatR;

namespace FamilyOs.Application.Admin.AiProviders;

public sealed record ListAiProvidersQuery : IRequest<IReadOnlyList<AiProviderDto>>;
