using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BarbeariaDoZe.Repositorio;

public static class RepositorioExtensions
{
    /// <summary>Registra DbContext e repositórios. A connection string vem do appsettings.json.</summary>
    public static IServiceCollection AddRepositorio(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BarbeariaContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IServicoRepositorio, ServicoRepositorio>();
        services.AddScoped<IBarbeiroRepositorio, BarbeiroRepositorio>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<IAgendamentoRepositorio, AgendamentoRepositorio>();
        services.AddScoped<IRelatorioRepositorio, RelatorioRepositorio>();
        return services;
    }
}
