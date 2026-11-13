using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace BarbeariaDoZe.Api;

public sealed class AcessoPublicoSwagger : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadados = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (!metadados.OfType<IAuthorizeData>().Any() || metadados.OfType<IAllowAnonymous>().Any()) operation.Security = [];
    }
}
