namespace Pim.Domain.Excecoes;

/// <summary>Violação de uma regra de negócio do domínio (a camada Application a traduz para HTTP 422/409).</summary>
public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem) : base(mensagem) { }
}
