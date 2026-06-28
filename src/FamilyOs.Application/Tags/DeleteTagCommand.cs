using MediatR;

namespace FamilyOs.Application.Tags;

public sealed record DeleteTagCommand(Guid Id, bool Force) : IRequest;
