using BarbeariaDoZe.Modelos;
using Microsoft.EntityFrameworkCore;

namespace BarbeariaDoZe.Repositorio;

public class ServicoRepositorio : IServicoRepositorio
{
    private readonly BarbeariaContext _db;
    public ServicoRepositorio(BarbeariaContext db) => _db = db;

    public Task<List<Servico>> ListarAsync() =>
        _db.Servicos.AsNoTracking().OrderBy(s => s.Preco).ToListAsync();

    public Task<Servico?> ObterAsync(int id) =>
        _db.Servicos.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
}

public class BarbeiroRepositorio : IBarbeiroRepositorio
{
    private readonly BarbeariaContext _db;
    public BarbeiroRepositorio(BarbeariaContext db) => _db = db;

    public Task<List<Barbeiro>> ListarAsync() =>
        _db.Barbeiros.AsNoTracking().Include(b => b.Usuario).Include(b => b.Horarios)
           .OrderBy(b => b.Usuario.Nome).ToListAsync();

    public Task<Barbeiro?> ObterAsync(int id) =>
        _db.Barbeiros.AsNoTracking().Include(b => b.Usuario).Include(b => b.Horarios)
           .FirstOrDefaultAsync(b => b.Id == id);
}

public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly BarbeariaContext _db;
    public UsuarioRepositorio(BarbeariaContext db) => _db = db;

    public Task<Usuario?> ObterPorEmailAsync(string email) =>
        _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => EF.Property<string>(u, "EmailNormalizado") == email.Trim().ToLowerInvariant());

    public Task<List<Usuario>> ListarClientesAsync() =>
        _db.Usuarios.AsNoTracking().Where(u => u.Tipo == "Cliente").OrderBy(u => u.Nome).ToListAsync();

    public Task<Usuario?> ObterAsync(int id) => _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    public Task<Usuario?> ObterPorRecuperacaoAsync(string hash) =>
        _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.RecuperacaoHash == hash);
    public async Task<bool> ConsumirRecuperacaoAsync(string hash, string senhaHash, DateTime agoraUtc)
    {
        var stamp = Guid.NewGuid().ToString("N");
        return await _db.Usuarios.Where(u => u.RecuperacaoHash == hash && u.RecuperacaoExpiraEmUtc > agoraUtc)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.SenhaHash, senhaHash)
                .SetProperty(u => u.SecurityStamp, stamp).SetProperty(u => u.RecuperacaoHash, (string?)null)
                .SetProperty(u => u.RecuperacaoExpiraEmUtc, (DateTime?)null)) == 1;
    }
    public Task<bool> ExisteAdministradorAsync() => _db.Usuarios.AnyAsync(u => u.Tipo == "Admin" && u.SenhaHash != null);
    public async Task AtualizarSegurancaAsync(Usuario usuario)
    {
        var atual = await _db.Usuarios.FindAsync(usuario.Id) ?? throw new KeyNotFoundException();
        atual.SenhaHash = usuario.SenhaHash;
        atual.SecurityStamp = usuario.SecurityStamp;
        atual.RecuperacaoHash = usuario.RecuperacaoHash;
        atual.RecuperacaoExpiraEmUtc = usuario.RecuperacaoExpiraEmUtc;
        await _db.SaveChangesAsync();
    }

    public async Task AdicionarAsync(Usuario usuario)
    {
        _db.Usuarios.Add(usuario);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.GetBaseException() is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 2601 or 2627)
        {
            _db.Entry(usuario).State = EntityState.Detached;
            throw new InvalidOperationException("Já existe um cadastro com este e-mail.");
        }
    }
}

public class AgendamentoRepositorio : IAgendamentoRepositorio
{
    private readonly BarbeariaContext _db;
    public AgendamentoRepositorio(BarbeariaContext db) => _db = db;

    private IQueryable<Agendamento> Completo() =>
        _db.Agendamentos.AsNoTracking()
           .Include(a => a.Usuario).Include(a => a.Servico).Include(a => a.Barbeiro!).ThenInclude(b => b.Usuario);

    public Task<List<Agendamento>> ListarPorDiaAsync(DateTime dia, int? barbeiroId = null)
    {
        var inicio = dia.Date;
        var fim = inicio.AddDays(1);
        var q = Completo().Where(a => a.DataHoraInicio >= inicio && a.DataHoraInicio < fim);
        if (barbeiroId.HasValue) q = q.Where(a => a.BarbeiroId == barbeiroId);
        return q.OrderBy(a => a.DataHoraInicio).ToListAsync();
    }

    public Task<List<Agendamento>> ListarTodosAsync() =>
        Completo().OrderByDescending(a => a.DataHoraInicio).ToListAsync();

    public Task<List<Agendamento>> ListarPorClienteAsync(int usuarioId) =>
        Completo().Where(a => a.UsuarioId == usuarioId).OrderByDescending(a => a.DataHoraInicio).ToListAsync();

    public Task<Agendamento?> ObterAsync(int id) =>
        Completo().FirstOrDefaultAsync(a => a.Id == id);

    public async Task AdicionarAsync(Agendamento agendamento)
    {
        _db.Agendamentos.Add(agendamento);
        // Códigos estáveis: 50001 = trigger; 1205 = transação escolhida em deadlock.
        // O texto da mensagem pode variar com o idioma do servidor.
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.GetBaseException() is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 50001 or 1205)
        {
            throw new ConflitoAgendamentoException(ex);
        }
    }

    public async Task AtualizarStatusAsync(int id, string status)
    {
        var ag = await _db.Agendamentos.FindAsync(id) ?? throw new KeyNotFoundException("Agendamento não encontrado.");
        ag.Status = status;
        await _db.SaveChangesAsync();
    }
}
