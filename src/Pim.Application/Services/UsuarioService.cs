using Microsoft.Extensions.Logging;
using Pim.Application.Dtos;
using Pim.Application.Excecoes;
using Pim.Application.Interfaces;
using Pim.Application.Mapeamentos;
using Pim.Domain.Entidades;
using Pim.Domain.Enums;

namespace Pim.Application.Services;

public class UsuarioService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IColaboradorRepository _colaboradores;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IUsuarioAtual _atual;
    private readonly ILogger<UsuarioService> _log;

    public UsuarioService(IUsuarioRepository usuarios, IColaboradorRepository colaboradores, IPasswordHasher hasher,
        IUnitOfWork uow, IUsuarioAtual atual, ILogger<UsuarioService> log)
    {
        _usuarios = usuarios; _colaboradores = colaboradores; _hasher = hasher; _uow = uow; _atual = atual; _log = log;
    }

    public async Task<IReadOnlyList<UsuarioResponse>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await _usuarios.ListarAsync(ct);
        var mapa = await _colaboradores.MapaUsuarioColaboradorAsync(ct);
        return lista.Select(u => u.ParaResponse(mapa.TryGetValue(u.Id, out var c) ? c : null)).ToList();
    }

    public async Task<UsuarioResponse> ObterAsync(int id, CancellationToken ct = default)
    {
        var u = await Carregar(id, ct);
        return u.ParaResponse((await _colaboradores.GetByUsuarioIdAsync(id, ct))?.Id);
    }

    public async Task<UsuarioResponse> CriarAsync(CriarUsuarioRequest r, CancellationToken ct = default)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        if (await _usuarios.EmailExisteAsync(email, null, ct)) throw new ConflictException("Já existe um usuário com este e-mail.");

        var u = Usuario.Criar(r.Nome, email, _hasher.Hash(r.Senha), r.Perfil);
        await _usuarios.AddAsync(u, ct);
        await _uow.SalvarAsync(_atual.Id, ct);

        int? colabId = null;
        if (r.Perfil == PerfilUsuario.Colaborador) colabId = (await VincularOuCriarColaborador(u, r.ColaboradorId, ct)).Id;

        _log.LogInformation("Segurança: usuário {UsuarioId} criado com perfil {Perfil} por {AdminId}", u.Id, u.Perfil, _atual.Id);
        return u.ParaResponse(colabId);
    }

    public async Task<UsuarioResponse> AtualizarAsync(int id, AtualizarUsuarioRequest r, CancellationToken ct = default)
    {
        var u = await Carregar(id, ct);
        var email = r.Email.Trim().ToLowerInvariant();
        if (email != u.Email && await _usuarios.EmailExisteAsync(email, id, ct)) throw new ConflictException("Já existe um usuário com este e-mail.");

        var perfilAnterior = u.Perfil;
        if (r.Perfil != perfilAnterior && id == _atual.Id) throw new ConflictException("Você não pode alterar o próprio perfil.");

        u.Atualizar(r.Nome, email);
        u.AlterarPerfil(r.Perfil);
        await _uow.SalvarAsync(_atual.Id, ct);

        var colab = await _colaboradores.GetByUsuarioIdAsync(id, ct);
        if (r.Perfil == PerfilUsuario.Colaborador && colab == null) colab = await VincularOuCriarColaborador(u, null, ct);
        if (r.Perfil != perfilAnterior)
            _log.LogWarning("Segurança: perfil do usuário {UsuarioId} alterado de {De} para {Para} por {AdminId}", id, perfilAnterior, r.Perfil, _atual.Id);
        return u.ParaResponse(colab?.Id);
    }

    public async Task RedefinirSenhaAsync(int id, RedefinirSenhaRequest r, CancellationToken ct = default)
    {
        var u = await Carregar(id, ct);
        u.DefinirSenhaHash(_hasher.Hash(r.NovaSenha));
        await _uow.SalvarAsync(_atual.Id, ct);
        _log.LogWarning("Segurança: senha do usuário {UsuarioId} redefinida por {AdminId}", id, _atual.Id);
    }

    public async Task<UsuarioResponse> AlterarAtivoAsync(int id, bool ativo, CancellationToken ct = default)
    {
        var u = await Carregar(id, ct);
        if (!ativo && id == _atual.Id) throw new ConflictException("Você não pode inativar a própria conta.");
        if (ativo) u.Ativar(); else u.Inativar();
        await _uow.SalvarAsync(_atual.Id, ct);
        _log.LogWarning("Segurança: usuário {UsuarioId} {Acao} por {AdminId}", id, ativo ? "ativado" : "inativado", _atual.Id);
        return u.ParaResponse((await _colaboradores.GetByUsuarioIdAsync(id, ct))?.Id);
    }

    private async Task<Usuario> Carregar(int id, CancellationToken ct) =>
        await _usuarios.GetByIdAsync(id, ct) ?? throw new NotFoundException("Usuário não encontrado.");

    private async Task<Colaborador> VincularOuCriarColaborador(Usuario u, int? colaboradorId, CancellationToken ct)
    {
        Colaborador c;
        if (colaboradorId.HasValue)
        {
            c = await _colaboradores.GetByIdAsync(colaboradorId.Value, ct) ?? throw new NotFoundException("Colaborador não encontrado.");
            if (c.UsuarioId.HasValue && c.UsuarioId != u.Id) throw new ConflictException("O colaborador já está vinculado a outro usuário.");
            c.VincularUsuario(u.Id);
        }
        else
        {
            var emailLivre = !await _colaboradores.EmailExisteAsync(u.Email, null, ct);
            c = Colaborador.Criar(u.Nome, emailLivre ? u.Email : null, null, u.Id);
            await _colaboradores.AddAsync(c, ct);
        }
        await _uow.SalvarAsync(_atual.Id, ct);
        return c;
    }
}
