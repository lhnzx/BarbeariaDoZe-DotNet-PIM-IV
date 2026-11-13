namespace BarbeariaDoZe.Negocio;

/// <summary>Erro de regra de negócio, com mensagem pronta para ser exibida ao usuário.</summary>
public class NegocioException : Exception
{
    public NegocioException(string mensagem) : base(mensagem) { }
}
