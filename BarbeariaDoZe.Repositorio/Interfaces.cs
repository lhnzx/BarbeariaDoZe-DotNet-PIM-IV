using BarbeariaDoZe.Modelos;

namespace BarbeariaDoZe.Repositorio;

public interface IServicoRepositorio
{
    Task<List<Servico>> ListarAsync();
    Task<Servico?> ObterAsync(int id);
}

public interface IBarbeiroRepositorio
{
    Task<List<Barbeiro>> ListarAsync();
    Task<Barbeiro?> ObterAsync(int id);
}

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObterPorEmailAsync(string email);
    Task<List<Usuario>> ListarClientesAsync();
    Task AdicionarAsync(Usuario usuario);
    Task<Usuario?> ObterAsync(int id);
    Task<Usuario?> ObterPorRecuperacaoAsync(string hash);
    Task AtualizarSegurancaAsync(Usuario usuario);
    Task<bool> ExisteAdministradorAsync();
    Task<bool> ConsumirRecuperacaoAsync(string hash, string senhaHash, DateTime agoraUtc);
}

public interface IAgendamentoRepositorio
{
    Task<List<Agendamento>> ListarPorDiaAsync(DateTime dia, int? barbeiroId = null);
    Task<List<Agendamento>> ListarTodosAsync();
    Task<List<Agendamento>> ListarPorClienteAsync(int usuarioId);
    Task<Agendamento?> ObterAsync(int id);
    Task AdicionarAsync(Agendamento agendamento);
    Task AtualizarStatusAsync(int id, string status);
}

public interface IRelatorioRepositorio
{
    /// <summary>Executa a stored procedure sp_RelatorioServicos.</summary>
    Task<List<RelatorioServico>> RelatorioServicosAsync(DateTime inicio, DateTime fim);
}

/// <summary>Conflito de gravação traduzido pelo Repositório, sem dependência do idioma do SQL Server.</summary>
public sealed class ConflitoAgendamentoException(Exception original)
    : Exception("Conflito de gravação do agendamento.", original);
