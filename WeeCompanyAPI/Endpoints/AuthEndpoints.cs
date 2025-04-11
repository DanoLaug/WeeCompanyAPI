using Microsoft.EntityFrameworkCore;
using WeeCompanyAPI.DTOs;
using WeeCompanyAPI.Models;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Registro de usuarios
        app.MapPost("/auth/registro", async (RegistroDTO dto, ApplicationDbContext db) =>
        {
            if (await db.Usuarios.AnyAsync(u => u.Correo == dto.Correo))
                return Results.BadRequest("El correo ya está registrado.");

            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Correo = dto.Correo,
                Contraseña = BCrypt.Net.BCrypt.HashPassword(dto.Contraseña),
                Rol = "Cliente" // Por defecto todos se registran como cliente
            };

            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
            return Results.Ok("Usuario registrado exitosamente.");
        });

        // Login de usuarios
        app.MapPost("/auth/login", async (LoginDTO dto, ApplicationDbContext db, JwtService jwt) =>
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == dto.Correo);
            if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Contraseña, usuario.Contraseña))
                return Results.Unauthorized();

            var token = jwt.GenerateToken(usuario.Id, usuario.Correo, usuario.Rol);
            return Results.Ok(new { token });
        });
    }
}

