using FamilyOs.Application.Family.Commands;
using FamilyOs.Application.Family.Queries;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Api.Endpoints;

public static class FamilyModule
{
    public static void MapFamilyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/family-members")
            .RequireAuthorization("RequireAuthenticated");

        group.MapGet("/", async (Relation? relation, ISender sender) =>
        {
            var result = await sender.Send(new GetFamilyMembersQuery(relation));
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetFamilyMemberByIdQuery(id));
            return Results.Ok(result);
        });

        group.MapPost("/", async (CreateFamilyMemberRequest req, ISender sender) =>
        {
            var result = await sender.Send(new CreateFamilyMemberCommand(
                req.DisplayName, req.Relation, req.FullName, req.BirthDate, req.Notes));
            return Results.Created($"/api/v1/family-members/{result.Id}", result);
        }).RequireAuthorization("RequireAdmin");

        group.MapMethods("/{id:guid}", ["PATCH"], async (Guid id, UpdateFamilyMemberRequest req, ISender sender) =>
        {
            var result = await sender.Send(new UpdateFamilyMemberCommand(
                id, req.DisplayName, req.Relation, req.FullName, req.BirthDate, req.Notes, req.RowVersion));
            return Results.Ok(result);
        }).RequireAuthorization("RequireAdmin");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            await sender.Send(new DeleteFamilyMemberCommand(id));
            return Results.NoContent();
        }).RequireAuthorization("RequireAdmin");
    }
}

public record CreateFamilyMemberRequest(
    string DisplayName,
    Relation Relation,
    string? FullName,
    DateOnly? BirthDate,
    string? Notes);

public record UpdateFamilyMemberRequest(
    string DisplayName,
    Relation Relation,
    string? FullName,
    DateOnly? BirthDate,
    string? Notes,
    string RowVersion);
