// Usos de paquetes y namespaces necesarios
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WeeCompanyAPI.Models;
using System.Security.Claims;
using WeeCompanyAPI.DTOs;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =======================================
// CONFIGURACIÓN DE SERVICIOS (DEPENDENCY INJECTION)
// =======================================

// Inyección del DbContext para usar Entity Framework con SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Inyectamos el servicio para generar tokens JWT
builder.Services.AddSingleton<JwtService>();

// Configuramos la autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Leemos la clave desde appsettings.json y configuramos el validador del token
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

// Activamos la autorización para usar [Authorize] o RequireAuthorization
builder.Services.AddAuthorization();

// =======================================
// CONFIGURAMOS SWAGGER (Documentación de la API)
// =======================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WeeCompany API", Version = "v1" });

    // Permitimos probar endpoints protegidos con JWT en Swagger
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

// =======================================
// CONFIGURACIÓN DEL PIPELINE HTTP
// =======================================

// En entorno de desarrollo mostramos Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();     // Redirección a HTTPS
app.UseRouting();              // Habilita rutas (¡importante!)
app.UseAuthentication();       // Activamos autenticación (JWT)
app.UseAuthorization();        // Activamos autorización (Roles, Claims)

// =======================================
// ENDPOINTS DE AUTENTICACIÓN
// =======================================

// Registro de usuario (POST /auth/registro)
app.MapPost("/auth/registro", async (RegistroDTO dto, ApplicationDbContext db) =>
{
    // Validar si el correo ya está registrado
    if (await db.Usuarios.AnyAsync(u => u.Correo == dto.Correo))
        return Results.BadRequest("El correo ya está registrado.");

    // Crear nuevo usuario con contraseña encriptada
    var usuario = new Usuario
    {
        Nombre = dto.Nombre,
        Correo = dto.Correo,
        Contraseña = BCrypt.Net.BCrypt.HashPassword(dto.Contraseña),
        Rol = "Cliente" // Rol por defecto
    };

    db.Usuarios.Add(usuario);
    await db.SaveChangesAsync();
    return Results.Ok("Usuario registrado exitosamente.");
}).Accepts<RegistroDTO>("application/json")
  .Produces<string>(200)
  .Produces(400);

// Login de usuario (POST /auth/login)
app.MapPost("/auth/login", async (LoginDTO dto, ApplicationDbContext db, JwtService jwt) =>
{
    // Buscar usuario por correo
    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == dto.Correo);

    // Verificar contraseña
    if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Contraseña, usuario.Contraseña))
        return Results.Unauthorized();

    // Generar token JWT y devolverlo
    var token = jwt.GenerateToken(usuario.Id, usuario.Correo, usuario.Rol);
    return Results.Ok(new { token });
}).Accepts<LoginDTO>("application/json")
  .Produces(200)
  .Produces(401);

// =======================================
// ENDPOINTS DE RESERVAS (Protegidos por JWT)
// =======================================

// Obtener todas las reservas (solo para Admins)
app.MapGet("/reservas", async (ClaimsPrincipal user, ApplicationDbContext db) =>
{
    // Validar que el usuario sea Admin
    if (!user.IsInRole("Admin"))
        return Results.Forbid();

    // Incluir datos del usuario con cada reserva
    var reservas = await db.Reservas.Include(r => r.Usuario).ToListAsync();
    return Results.Ok(reservas);
}).RequireAuthorization()
  .Produces(403)
  .Produces<List<Reserva>>(200);

// Obtener reservas propias (clientes autenticados)
app.MapGet("/reservas/mis-reservas", async (ClaimsPrincipal user, ApplicationDbContext db) =>
{
    // Obtener el ID del usuario autenticado
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Filtrar reservas por el usuario
    var reservas = await db.Reservas.Where(r => r.UsuarioId == userId).ToListAsync();
    return Results.Ok(reservas);
}).RequireAuthorization()
  .Produces<List<Reserva>>(200);

// Crear una nueva reserva (clientes autenticados)
app.MapPost("/reservas", async (ReservaDTO dto, ClaimsPrincipal user, ApplicationDbContext db) =>
{
    // Obtener el ID del usuario autenticado
    var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Crear la reserva con los datos del cliente
    var reserva = new Reserva
    {
        UsuarioId = userId,
        FechaHora = dto.FechaHora,
        CantidadPersonas = dto.CantidadPersonas,
    };

    db.Reservas.Add(reserva);
    await db.SaveChangesAsync();
    return Results.Ok("Reserva creada correctamente.");
}).RequireAuthorization()
  .Accepts<ReservaDTO>("application/json")
  .Produces<string>(200);

// =======================================
// ARRANQUE DE LA APLICACIÓN
// =======================================
app.Run();
