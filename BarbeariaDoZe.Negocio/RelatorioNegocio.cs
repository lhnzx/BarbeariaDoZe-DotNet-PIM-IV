using BarbeariaDoZe.Modelos;
using BarbeariaDoZe.Repositorio;

namespace BarbeariaDoZe.Negocio;

public class RelatorioNegocio
{
    private readonly IRelatorioRepositorio _repo;
    public RelatorioNegocio(IRelatorioRepositorio repo) => _repo = repo;

    /// <summary>Relatório por serviço em um período, calculado pela stored procedure.</summary>
    public Task<List<RelatorioServico>> ServicosAsync(DateTime inicio, DateTime fim)
    {
        if (inicio.Year < 1753 || fim.Year < 1753 || fim.Date >= DateTime.MaxValue.Date)
            throw new NegocioException("Informe um período válido a partir de 1753 e anterior a 31/12/9999.");
        if (fim < inicio) throw new NegocioException("A data final deve ser posterior à inicial.");
        return _repo.RelatorioServicosAsync(inicio, fim);
    }
}
