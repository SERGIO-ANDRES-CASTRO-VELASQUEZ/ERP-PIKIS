using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpPikis.Domain.Entities;
using ErpPikis.Service.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ErpPikis.Api.Seguridad;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options) => _options = options.Value;

    public (string Token, DateTime ExpiraUtc) Generar(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddHours(_options.ExpiracionHoras);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Username),
            new("nombre", usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol?.Nombre ?? string.Empty)
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
