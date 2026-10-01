namespace ErpPikis.Api.Seguridad;

public class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Issuer { get; set; } = "ErpPikis";
    public string Audience { get; set; } = "ErpPikis";
    public string Key { get; set; } = string.Empty;
    public int ExpiracionHoras { get; set; } = 12;
}
