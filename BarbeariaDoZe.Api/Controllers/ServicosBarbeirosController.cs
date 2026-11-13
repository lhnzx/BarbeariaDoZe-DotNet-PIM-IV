using BarbeariaDoZe.Api.Dtos;
using BarbeariaDoZe.Negocio;
using Microsoft.AspNetCore.Mvc;

namespace BarbeariaDoZe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicosController : ControllerBase
{
    private readonly ServicoNegocio _negocio;
    public ServicosController(ServicoNegocio negocio) => _negocio = negocio;

    /// <summary>Lista os serviços oferecidos.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServicoDto>>> Get() =>
        Ok((await _negocio.ListarAsync()).Select(ServicoDto.De));
}

[ApiController]
[Route("api/[controller]")]
public class BarbeirosController : ControllerBase
{
    private readonly BarbeiroNegocio _negocio;
    public BarbeirosController(BarbeiroNegocio negocio) => _negocio = negocio;

    /// <summary>Lista os barbeiros cadastrados.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BarbeiroDto>>> Get() =>
        Ok((await _negocio.ListarAsync()).Select(BarbeiroDto.De));
}
