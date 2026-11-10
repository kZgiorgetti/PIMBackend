using System.Security.Claims;
using Pim.Application.Interfaces;
using Pim.Domain.Enums;

namespace Pim.Api.Servicos;

public class UsuarioAtual : IUsuarioAtual
{
    private readonly ClaimsPrincipal? _user;
    public UsuarioAtual(IHttpContextAccessor http) => _user = http.HttpContext?.User;

    public int? Id => Int("sub");
    public string? Nome => _user?.FindFirst("name")?.Value;
    public int? ColaboradorId => Int("colaboradorId");
    public PerfilUsuario? Perfil => Enum.TryParse<PerfilUsuario>(_user?.FindFirst("role")?.Value, out var p) ? p : null;
    public bool EhGestorOuAdmin => Perfil is PerfilUsuario.Admin or PerfilUsuario.Gestor;

    private int? Int(string tipo) => int.TryParse(_user?.FindFirst(tipo)?.Value, out var v) ? v : null;
}
