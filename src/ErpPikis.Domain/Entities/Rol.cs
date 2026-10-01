namespace ErpPikis.Domain.Entities;

public class Rol
{
    public const string Administrador = "Administrador";
    public const string Cajero = "Cajero";

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
