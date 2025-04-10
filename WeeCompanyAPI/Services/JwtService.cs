using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtService
{
    //Inyeccion IConfiguration para leer valores
    private readonly IConfiguration _config;
    public JwtService(IConfiguration config) => _config = config;

    //Generar token JWT
    public string GenerateToken(int userId, string email, string rol)
    {
        //Claims son afirmaciones sobre el usuario
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, rol)
        };

        //Clave y credenciales para firmar el token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //Crear el token
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds
        );

        //Serializar el token = convertir a string "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." 
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
