using BarbeariaDoZe.Repositorio;
using Microsoft.Extensions.DependencyInjection;

namespace BarbeariaDoZe.Negocio;

public static class NegocioExtensions
{
    /// <summary>
    /// Ponto único de configuração consumido por Web e Api. Assim os projetos de apresentação
    /// não precisam conhecer o Repositório: o acesso a dados passa sempre pelo Negócio.
    /// </summary>
    public static IServiceCollection AddNegocio(this IServiceCollection services, string connectionString)
    {
        services.AddRepositorio(connectionString);
        services.AddScoped<ServicoNegocio>();
        services.AddScoped<BarbeiroNegocio>();
        services.AddScoped<UsuarioNegocio>();
        services.AddScoped<RecuperacaoSenhaNegocio>();
        services.AddScoped<AgendamentoNegocio>();
        services.AddScoped<RelatorioNegocio>();
        return services;
    }
}
