using FluentValidation;
using Pim.Application.Dtos;

namespace Pim.Application.Validators;

internal static class Regras
{
    public static bool SenhaForte(string? s) =>
        !string.IsNullOrEmpty(s) && s.Length >= 8 && s.Any(char.IsLetter) && s.Any(char.IsDigit);
    public const string MsgSenha = "A senha deve ter ao menos 8 caracteres, com letras e números.";
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Senha).NotEmpty().MaximumLength(200);
    }
}

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(500);
}

public class CriarUsuarioRequestValidator : AbstractValidator<CriarUsuarioRequest>
{
    public CriarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Senha).Must(Regras.SenhaForte).WithMessage(Regras.MsgSenha);
        RuleFor(x => x.Perfil).IsInEnum();
    }
}

public class AtualizarUsuarioRequestValidator : AbstractValidator<AtualizarUsuarioRequest>
{
    public AtualizarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Perfil).IsInEnum();
    }
}

public class RedefinirSenhaRequestValidator : AbstractValidator<RedefinirSenhaRequest>
{
    public RedefinirSenhaRequestValidator() => RuleFor(x => x.NovaSenha).Must(Regras.SenhaForte).WithMessage(Regras.MsgSenha);
}

public class ColaboradorRequestValidator : AbstractValidator<ColaboradorRequest>
{
    public ColaboradorRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Cargo).MaximumLength(100);
    }
}

public class EquipeRequestValidator : AbstractValidator<EquipeRequest>
{
    public EquipeRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public class CriarTarefaRequestValidator : AbstractValidator<CriarTarefaRequest>
{
    public CriarTarefaRequestValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(2000);
        RuleFor(x => x.Prioridade).IsInEnum();
    }
}

public class AtualizarTarefaRequestValidator : AbstractValidator<AtualizarTarefaRequest>
{
    public AtualizarTarefaRequestValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(2000);
        RuleFor(x => x.Prioridade).IsInEnum();
    }
}

public class AlterarStatusRequestValidator : AbstractValidator<AlterarStatusRequest>
{
    public AlterarStatusRequestValidator() => RuleFor(x => x.Status).IsInEnum();
}

public class ComentarioRequestValidator : AbstractValidator<ComentarioRequest>
{
    public ComentarioRequestValidator() => RuleFor(x => x.Texto).NotEmpty().MaximumLength(1000);
}

public class TarefaFiltroValidator : AbstractValidator<TarefaFiltro>
{
    public TarefaFiltroValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Busca).MaximumLength(100);
    }
}
