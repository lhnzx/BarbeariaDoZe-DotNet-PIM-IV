using System.Data;
using BarbeariaDoZe.Modelos;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace BarbeariaDoZe.Repositorio;

/// <summary>
/// Executa a stored procedure sp_RelatorioServicos com Dapper, reaproveitando a
/// conexão do DbContext. O parâmetro é enviado de forma parametrizada (sem SQL Injection).
/// </summary>
public class RelatorioRepositorio : IRelatorioRepositorio
{
    private readonly BarbeariaContext _db;
    public RelatorioRepositorio(BarbeariaContext db) => _db = db;

    public async Task<List<RelatorioServico>> RelatorioServicosAsync(DateTime inicio, DateTime fim)
    {
        var conexao = _db.Database.GetDbConnection();
        var linhas = await conexao.QueryAsync<RelatorioServico>(
            "sp_RelatorioServicos",
            new { DataInicio = inicio.Date, DataFim = fim.Date },
            commandType: CommandType.StoredProcedure);
        return linhas.ToList();
    }
}
