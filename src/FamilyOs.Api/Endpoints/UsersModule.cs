using FamilyOs.Application.Users.Commands;
using FamilyOs.Application.Users.Queries;
using MediatR;

namespace FamilyOs.Api.Endpoints;

public static class UsersModule
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .RequireAuthorization("RequireAdmin");

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetUserAccountsQuery());
            return Results.Ok(result);
        });

        group.MapPost("/invite", async (InviteUserRequest req, ISender sender) =>
        {
            await sender.Send(new InviteUserCommand(req.Email, req.FamilyMemberId, req.Role));
            return Results.Accepted();
        });

        group.MapMethods("/{id:guid}", ["PATCH"], async (Guid id, PatchUserRequest req, ISender sender) =>
        {
            await sender.Send(new PatchUserAccountCommand(id, req.Role, req.IsActive));
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            await sender.Send(new DeleteUserAccountCommand(id));
            return Results.NoContent();
        });
    }
}

public record InviteUserRequest(string Email, Guid FamilyMemberId, string Role);
public record PatchUserRequest(string? Role, bool? IsActive);
