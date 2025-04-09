namespace WeeCompanyAPI.Models
{
    public class Reserva
    {
        public int Id { get; set; }
        
        //Foreign Key de Usuario
        public int UsuarioId { get; set; }
        public DateTime FechaHora { get; set; }
        public int CantidadPersonas { get; set; }
        
        //Propiedad de navegación en Entity Framework
        public Usuario? Usuario { get; set; }
    }

}
