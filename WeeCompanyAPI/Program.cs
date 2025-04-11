using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WeeCompanyAPI.Models;
using System.Security.Claims;
using WeeCompanyAPI.DTOs;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT Authentication
builder.Services.AddSingleton<JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Add Authorization Services
builder.Services.AddAuthorization();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WeeCompany API", Version = "v1" });

    // Configure JWT in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting(); // <= ¡IMPORTANTE! Debe ir antes de UseAuthentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// Auth Endpoints
app.MapPost("/auth/registro", async (RegistroDTO dto, ApplicationDbContext db) =>
{
    if (await db.Usuarios.AnyAsync(u => u.Correo == dto.Correo))
        return Results.BadRequest("El correo ya está registrado.");

    var usuario = new Usuario
    {
        Nombre = dto.Nombre,
        Correo = dto.Correo,
        Contraseña = BCrypt.Net.BCrypt.HashPassword(dto.Contraseña),
        Rol = "Cliente"
    };

    db.Usuarios.Add(usuario);
    await db.SaveChangesAsync();
    return Results.Ok("Usuario registrado exitosamente.");
}).Accepts<RegistroDTO>("application/json").Produces<string>(200).Produces(400);

app.MapPost("/auth/login", async (LoginDTO dto, ApplicationDbContext db, JwtService jwt) =>
{
    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == dto.Correo);
    if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Contraseña, usuario.Contraseña))
        return Results.Unauthorized();

    var token = jwt.GenerateToken(usuario.Id, usuario.Correo, usuario.Rol);
    return Results.Ok(new { token });
}).Accepts<LoginDTO>("application/json").Produces(200).Produces(401);

// Reservas Endpoints
app.MapGet("/reservas", async (ClaimsPrincipal user, ApplicationDbContext db) =>
{
    if (!user.IsInRole("Admin"))
        return Results.Forbid();

    var reservas = await db.Reservas.Include(r => r.Usuario).ToListAsync();
    return Results.Ok(reservas);
}).RequireAuthorization().Produces(403).Produces<List<Reserva>>(200);

app.MapGet("/reservas/mis-reservas", async (ClaimsPrincipal user, ApplicationDbContext db) =>
{
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var reservas = await db.Reservas.Where(r => r.UsuarioId == userId).ToListAsync();
    return Results.Ok(reservas);
}).RequireAuthorization().Produces<List<Reserva>>(200);

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
}).RequireAuthorization().Accepts<ReservaDTO>("application/json").Produces<string>(200);

app.Run();