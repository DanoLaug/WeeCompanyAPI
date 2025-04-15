// Librerías necesarias para JWT, claims y encriptación
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

// Servicio encargado de generar tokens JWT
public class JwtService
{
    // Inyectamos IConfiguration para poder leer valores del archivo appsettings.json
    private readonly IConfiguration _config;

    // Constructor que recibe la configuración
    public JwtService(IConfiguration config) => _config = config;

    // Método público que genera y retorna un token JWT
    public string GenerateToken(int userId, string email, string rol)
    {
        // ==============================================
        // 1. DEFINIMOS LOS CLAIMS DEL USUARIO
        // Son afirmaciones sobre la identidad del usuario que estarán dentro del token
        // Ej: ID, correo, y rol (Admin o Cliente)
        // ==============================================
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // Identificador del usuario
            new Claim(ClaimTypes.Email, email),                      // Correo del usuario
            new Claim(ClaimTypes.Role, rol)                          // Rol: Admin o Cliente
        };

        // ==============================================
        // 2. CLAVE SIMÉTRICA Y CREDENCIALES DE FIRMA
        // Usamos la clave secreta desde appsettings.json
        // con algoritmo HMAC SHA256 para firmar el token
        // ==============================================
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // ==============================================
        // 3. CREAR EL TOKEN JWT
        // Incluye: emisor, audiencia, claims, expiración y firma
        // ==============================================
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],               // Emisor del token
            audience: _config["Jwt:Audience"],           // Audiencia (quién debe aceptar el token)
            claims: claims,                              // Datos del usuario
            expires: DateTime.Now.AddHours(2),           // Tiempo de expiración (2 horas)
            signingCredentials: creds                    // Firma con clave secreta
        );

        // ==============================================
        // 4. SERIALIZAMOS EL TOKEN
        // Convertimos el objeto token a string en formato JWT (base64)
        // ==============================================
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
