using BarbeariaDoZe.Modelos;
using BarbeariaDoZe.Repositorio;

namespace BarbeariaDoZe.Negocio;

/// <summary>Regras do agendamento: disponibilidade, conflito, preço, duração e status.</summary>
public class AgendamentoNegocio
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DuracaoPadrao = TimeSpan.FromMinutes(45);

    private static readonly string[] Dias =
        { "Domingo", "Segunda-Feira", "Terça-Feira", "Quarta-Feira", "Quinta-Feira", "Sexta-Feira", "Sábado" };

    private readonly IAgendamentoRepositorio _agendamentos;
    private readonly IServicoRepositorio _servicos;
    private readonly IBarbeiroRepositorio _barbeiros;
    private readonly IUsuarioRepositorio _usuarios;

    public AgendamentoNegocio(IAgendamentoRepositorio agendamentos, IServicoRepositorio servicos,
                              IBarbeiroRepositorio barbeiros, IUsuarioRepositorio usuarios)
    {
        _agendamentos = agendamentos;
        _servicos = servicos;
        _barbeiros = barbeiros;
        _usuarios = usuarios;
    }

    /// <summary>Retorna os horários livres do barbeiro em um dia, respeitando duração do serviço e agenda.</summary>
    public async Task<List<DateTime>> HorariosDisponiveisAsync(DateTime dia, int servicoId, int barbeiroId)
    {
        if (dia.Date < HorarioLocal.Hoje) return new();
        if (dia.Date > HorarioLocal.Hoje.AddDays(90)) throw new NegocioException("Escolha uma data nos próximos 90 dias.");
        var servico = await _servicos.ObterAsync(servicoId) ?? throw new NegocioException("Serviço não encontrado.");
        var barbeiro = await _barbeiros.ObterAsync(barbeiroId) ?? throw new NegocioException("Barbeiro não encontrado.");

        var expedientes = barbeiro.Horarios.Where(h => h.DiaSemana == Dias[(int)dia.DayOfWeek]).ToList();
        if (expedientes.Count == 0) return new();

        var duracao = servico.TempoEstimado ?? DuracaoPadrao;
        var ocupados = (await _agendamentos.ListarPorDiaAsync(dia, barbeiroId))
                       .Where(a => a.Status != "Cancelado").ToList();

        var livres = new List<DateTime>();
        foreach (var expediente in expedientes)
            for (var t = expediente.HoraInicio; t + duracao <= expediente.HoraFim; t += Intervalo)
            {
                var inicio = dia.Date + t;
                var fim = inicio + duracao;
                bool passado = inicio <= HorarioLocal.Agora;
                bool conflito = ocupados.Any(a => inicio < a.DataHoraFim && fim > a.DataHoraInicio);
                if (!passado && !conflito) livres.Add(inicio);
            }
        return livres.Distinct().OrderBy(h => h).ToList();
    }

    /// <summary>Cria o agendamento aplicando todas as regras. Cadastra o cliente se ainda não existir.</summary>
    public async Task<Agendamento> AgendarAsync(SolicitacaoAgendamento s, int usuarioAutenticadoId)
    {
        var servico = await _servicos.ObterAsync(s.ServicoId) ?? throw new NegocioException("Serviço não encontrado.");
        _ = await _barbeiros.ObterAsync(s.BarbeiroId) ?? throw new NegocioException("Barbeiro não encontrado.");

        var disponiveis = await HorariosDisponiveisAsync(s.Inicio, s.ServicoId, s.BarbeiroId);
        if (!disponiveis.Contains(s.Inicio))
            throw new NegocioException("O horário escolhido não está disponível para este barbeiro.");

        var cliente = await _usuarios.ObterAsync(usuarioAutenticadoId)
            ?? throw new NegocioException("Conta não encontrada. Entre novamente.");
        if (cliente.Tipo != "Cliente") throw new NegocioException("Utilize uma conta de cliente para reservar um horário.");

        var agendamento = new Agendamento
        {
            UsuarioId = cliente.Id,
            BarbeiroId = s.BarbeiroId,
            ServicoId = servico.Id,
            DataHoraInicio = s.Inicio,
            DataHoraFim = s.Inicio + (servico.TempoEstimado ?? DuracaoPadrao),
            Preco = servico.Preco,
            Status = "Confirmado"
        };

        try
        {
            await _agendamentos.AdicionarAsync(agendamento);
        }
        catch (ConflitoAgendamentoException)
        {
            // Segunda linha de defesa: a trigger do banco barrou uma corrida entre dois clientes.
            throw new NegocioException("Este horário acabou de ser reservado. Escolha outro.");
        }

        return (await _agendamentos.ObterAsync(agendamento.Id))!;
    }

    /// <summary>Confirmado → Concluido ou Cancelado. Agendamentos encerrados não mudam mais.</summary>
    public async Task AlterarStatusAsync(int id, string novoStatus, int solicitanteId, bool administrador)
    {
        var atual = await _agendamentos.ObterAsync(id) ?? throw new NegocioException("Agendamento não encontrado.");
        if (!administrador && atual.UsuarioId != solicitanteId)
            throw new AcessoNegadoException("Este agendamento pertence a outra conta.");
        if (!administrador && novoStatus != "Cancelado")
            throw new AcessoNegadoException("Somente o administrador pode concluir atendimentos.");
        if (novoStatus is not ("Concluido" or "Cancelado"))
            throw new NegocioException("Status de destino inválido.");
        if (atual.Status != "Confirmado")
            throw new NegocioException($"Agendamento já está {atual.Status.ToLower()} e não pode ser alterado.");

        await _agendamentos.AtualizarStatusAsync(id, novoStatus);
    }

    public Task<List<Agendamento>> ListarDoDiaAsync(DateTime dia) => _agendamentos.ListarPorDiaAsync(dia);
    public Task<List<Agendamento>> ListarTodosAsync() => _agendamentos.ListarTodosAsync();

    public Task<List<Agendamento>> ListarDoClienteAsync(int usuarioId) => _agendamentos.ListarPorClienteAsync(usuarioId);
}
