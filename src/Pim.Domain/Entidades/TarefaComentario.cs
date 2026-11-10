using Pim.Domain.Excecoes;

namespace Pim.Domain.Entidades;

public class TarefaComentario
{
    public int Id { get; private set; }
    public int TarefaId { get; private set; }
    public int UsuarioId { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public DateTime DataCriacao { get; private set; }
    public Usuario? Usuario { get; private set; }

    private TarefaComentario() { } // EF Core

    public static TarefaComentario Criar(int tarefaId, int usuarioId, string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) throw new RegraDeNegocioException("O comentário não pode ser vazio.");
        if (texto.Trim().Length > 1000) throw new RegraDeNegocioException("O comentário deve ter no máximo 1000 caracteres.");
        return new TarefaComentario { TarefaId = tarefaId, UsuarioId = usuarioId, Texto = texto.Trim(), DataCriacao = DateTime.UtcNow };
    }
}
