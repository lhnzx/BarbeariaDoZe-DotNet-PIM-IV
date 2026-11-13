using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BarbeariaDoZe.Api.Dtos;
using BarbeariaDoZe.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BarbeariaDoZe.Api.Controllers;

public record ReservaDto([Range(1, int.MaxValue)] int ServicoId, [Range(1, int.MaxValue)] int BarbeiroId, DateTime Inicio);

[ApiController, Route("api/[controller]"), Authorize]
public class AgendamentosController(AgendamentoNegocio negocio) : ControllerBase
{
    private int UsuarioId => int.Parse(User.FindFirstValue("sub")!);
    [HttpGet("horarios-disponiveis"), AllowAnonymous]
    public async Task<IActionResult> Horarios(DateTime data, int servicoId, int barbeiroId)
    {
        try { return Ok(await negocio.HorariosDisponiveisAsync(data, servicoId, barbeiroId)); }
        catch (NegocioException e) { return NotFound(new { erro = e.Message }); }
    }
    // O parâmetro email deixou de filtrar dados: a identidade vem do token validado.
    [HttpGet]
    public async Task<IActionResult> Meus() => Ok((await negocio.ListarDoClienteAsync(UsuarioId)).Select(AgendamentoDto.De));
    [HttpPost]
    public async Task<IActionResult> Criar(ReservaDto dto)
    {
        try
        {
            var ag = await negocio.AgendarAsync(new SolicitacaoAgendamento("", "", "", dto.ServicoId, dto.BarbeiroId, dto.Inicio), UsuarioId);
            return CreatedAtAction(nameof(Meus), null, AgendamentoDto.De(ag));
        }
        catch (NegocioException e) { return BadRequest(new { erro = e.Message }); }
    }
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> AlterarStatus(int id, AlterarStatusDto dto)
    {
        try { await negocio.AlterarStatusAsync(id, dto.Status, UsuarioId, User.IsInRole("Admin")); return NoContent(); }
        catch (AcessoNegadoException e) { return StatusCode(403, new { erro = e.Message }); }
        catch (NegocioException e) { return BadRequest(new { erro = e.Message }); }
    }
}
[ApiController, Route("api/[controller]"), Authorize(Roles = "Admin")]
public class RelatoriosController(RelatorioNegocio negocio) : ControllerBase
{
    [HttpGet("servicos")]
    public async Task<IActionResult> Servicos(DateTime inicio, DateTime fim)
    {
        try { return Ok(await negocio.ServicosAsync(inicio, fim)); }
        catch (NegocioException e) { return BadRequest(new { erro = e.Message }); }
    }
}
