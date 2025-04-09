namespace WeeCompanyAPI.Models
{
    //Modelo de Usuario
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;

        //En un sistema real, nunca guardar contraseñas en texto plano.
        //Usarías técnicas como hashing + salt
        public string Contraseña { get; set; } = string.Empty;
        
        //Usar un enum para roles definidos
        public enum RolUsuario { Cliente, Admin }
        public RolUsuario Rol { get; set; } = RolUsuario.Cliente;
    }

}
