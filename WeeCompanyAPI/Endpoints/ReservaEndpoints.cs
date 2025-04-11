using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using WeeCompanyAPI.DTOs;
using WeeCompanyAPI.Models;

public static class ReservaEndpoints
{
    public static void MapReservaEndpoints(this WebApplication app)
    {
        app.MapGet("/reservas", async (ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            if (!user.IsInRole("Admin"))
                return Results.Forbid();

            var reservas = await db.Reservas.Include(r => r.Usuario).ToListAsync();
            return Results.Ok(reservas);
        }).RequireAuthorization();

        app.MapGet("/reservas/mis-reservas", async (ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var reservas = await db.Reservas.Where(r => r.UsuarioId == userId).ToListAsync();
            return Results.Ok(reservas);
        }).RequireAuthorization();

        app.MapPost("/reservas", async (ReservaDTO dto, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var reserva = new Reserva
            {
                UsuarioId = userId,
                FechaHora = dto.FechaHora,
                CantidadPersonas = dto.CantidadPersonas,
            };
            db.Reservas.Add(reserva);
            await db.SaveChangesAsync();
            return Results.Ok("Reserva creada correctamente.");
        }).RequireAuthorization();

        app.MapPut("/reservas/{id}", async (int id, ReservaDTO dto, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var rol = user.FindFirstValue(ClaimTypes.Role);

            var reserva = await db.Reservas.FindAsync(id);
            if (reserva is null)
                return Results.NotFound();

            if (reserva.UsuarioId != userId && rol != "Admin")
                return Results.Forbid();

            reserva.FechaHora = dto.FechaHora;
            reserva.CantidadPersonas = dto.CantidadPersonas;
            await db.SaveChangesAsync();
            return Results.Ok("Reserva actualizada.");
        }).RequireAuthorization();

        app.MapDelete("/reservas/{id}", async (int id, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var rol = user.FindFirstValue(ClaimTypes.Role);

            var reserva = await db.Reservas.FindAsync(id);
            if (reserva is null)
                return Results.NotFound();

            if (reserva.UsuarioId != userId && rol != "Admin")
                return Results.Forbid();

            db.Reservas.Remove(reserva);
            await db.SaveChangesAsync();
            return Results.Ok("Reserva eliminada.");
        }).RequireAuthorization();
    }
}

