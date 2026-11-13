using BarbeariaDoZe.Modelos;
using BarbeariaDoZe.Repositorio;
namespace BarbeariaDoZe.Negocio;

public class ServicoNegocio(IServicoRepositorio repo)
{
    public Task<List<Servico>> ListarAsync() => repo.ListarAsync();
}
public class BarbeiroNegocio(IBarbeiroRepositorio repo)
{
    public Task<List<Barbeiro>> ListarAsync() => repo.ListarAsync();
}
public class UsuarioNegocio(IUsuarioRepositorio repo)
{
    // Hash fictício mantém o custo do login semelhante quando a conta não existe.
    private static readonly string HashFicticio = SegurancaSenha.CriarHash(Guid.NewGuid().ToString("N"));
    public Task<List<Usuario>> ListarClientesAsync() => repo.ListarClientesAsync();
    public Task<Usuario?> ObterAsync(int id) => repo.ObterAsync(id);
    public async Task<Usuario> AutenticarAsync(string email, string senha)
    {
        var u = await repo.ObterPorEmailAsync(email?.Trim() ?? "");
        var valida = SegurancaSenha.Verificar(senha, u?.SenhaHash ?? HashFicticio);
        if (!valida || u?.SenhaHash is null) throw new NegocioException("E-mail ou senha inválidos.");
        return u;
    }
    public async Task<Usuario> CadastrarClienteAsync(string nome, string telefone, string email, string senha)
    {
        var u = new Usuario { Nome = nome.Trim(), Telefone = telefone.Trim(), Email = email.Trim().ToLowerInvariant(), Tipo = "Cliente" };
        var erros = u.Validar().ToList();
        if (erros.Count > 0) throw new NegocioException(string.Join(" ", erros));
        SegurancaSenha.Validar(senha);
        if (await repo.ObterPorEmailAsync(u.Email) is not null) throw new NegocioException("Já existe um cadastro com este e-mail.");
        u.SenhaHash = SegurancaSenha.CriarHash(senha);
        try { await repo.AdicionarAsync(u); }
        catch (InvalidOperationException ex) { throw new NegocioException(ex.Message); }
        return u;
    }
    /// <summary>
    /// Contas de demonstração para a apresentação local (Demo:Ativo = true).
    /// Cria a conta somente se o e-mail ainda não existir; nunca altera contas existentes.
    /// </summary>
    public async Task GarantirContaDemoAsync(string email, string nome, string telefone, string senha, string tipo)
    {
        if (await repo.ObterPorEmailAsync(email) is not null) return;
        var u = new Usuario { Nome = nome, Email = email.Trim().ToLowerInvariant(), Telefone = telefone, Tipo = tipo, SenhaHash = SegurancaSenha.CriarHash(senha) };
        var erros = u.Validar().ToList();
        if (erros.Count > 0) throw new NegocioException(string.Join(" ", erros));
        try { await repo.AdicionarAsync(u); }
        catch (InvalidOperationException) { /* criada em paralelo pela API: nada a fazer */ }
    }
    public async Task CriarAdministradorInicialAsync(string email, string nome, string telefone, string senha)
    {
        if (await repo.ExisteAdministradorAsync()) return;
        if (await repo.ObterPorEmailAsync(email) is not null)
            throw new NegocioException("O e-mail de inicialização já está cadastrado. Não é permitido promover uma conta existente automaticamente.");
        var u = new Usuario { Nome = nome.Trim(), Email = email.Trim().ToLowerInvariant(), Telefone = telefone, Tipo = "Admin", SenhaHash = SegurancaSenha.CriarHash(senha) };
        var erros = u.Validar().ToList();
        if (erros.Count > 0) throw new NegocioException(string.Join(" ", erros));
        await repo.AdicionarAsync(u);
    }
}
