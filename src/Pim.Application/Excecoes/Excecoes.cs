namespace Pim.Application.Excecoes;

public class NotFoundException : Exception { public NotFoundException(string m) : base(m) { } }
public class ConflictException : Exception { public ConflictException(string m) : base(m) { } }
public class ForbiddenException : Exception { public ForbiddenException(string m) : base(m) { } }
public class UnauthorizedException : Exception { public UnauthorizedException(string m) : base(m) { } }

public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Erros { get; }
    public ValidationException(IDictionary<string, string[]> erros)
        : base("Um ou mais campos são inválidos.") => Erros = new Dictionary<string, string[]>(erros);
    public ValidationException(string campo, string mensagem)
        : this(new Dictionary<string, string[]> { [campo] = new[] { mensagem } }) { }
}
