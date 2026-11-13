namespace BarbeariaDoZe.Negocio;

/// <summary>Dados necessários para solicitar um agendamento (independentes de MVC ou API).</summary>
public record SolicitacaoAgendamento(
    string Nome,
    string Telefone,
    string Email,
    int ServicoId,
    int BarbeiroId,
    DateTime Inicio);
