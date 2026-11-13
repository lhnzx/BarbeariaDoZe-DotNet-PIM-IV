using BarbeariaDoZe.Negocio;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using BarbeariaDoZe.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BarbeariaDoZe.Web.Controllers;

public class AgendarController : Controller
{
    private readonly AgendamentoNegocio _agendamentos;
    private readonly ServicoNegocio _servicos;
    private readonly BarbeiroNegocio _barbeiros;

    public AgendarController(AgendamentoNegocio agendamentos, ServicoNegocio servicos, BarbeiroNegocio barbeiros)
    {
        _agendamentos = agendamentos;
        _servicos = servicos;
        _barbeiros = barbeiros;
    }

    public async Task<IActionResult> Index() => View(await Preencher(new AgendarViewModel()));

    /// <summary>Usado pela tela (fetch) para listar os horários livres.</summary>
    [HttpGet]
    public async Task<IActionResult> Horarios(DateTime data, int servicoId, int barbeiroId)
    {
        try
        {
            var livres = await _agendamentos.HorariosDisponiveisAsync(data, servicoId, barbeiroId);
            return Json(livres.Select(h => h.ToString("HH:mm")));
        }
        catch (NegocioException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Index(AgendarViewModel vm)
    {
        await Preencher(vm);
        if (!ModelState.IsValid) return View(vm);

        try
        {
            var inicio = vm.Data!.Value.Date + (TimeSpan.TryParseExact(vm.Hora!, @"hh\:mm", System.Globalization.CultureInfo.InvariantCulture, out var hora) ? hora : throw new NegocioException("Horário inválido."));
            var ag = await _agendamentos.AgendarAsync(new SolicitacaoAgendamento(
                "", "", "", vm.ServicoId!.Value, vm.BarbeiroId!.Value, inicio), int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
            return RedirectToAction(nameof(Confirmado), new { id = ag.Id });
        }
        catch (NegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Confirmado(int id)
    {
        var meus = await _agendamentos.ListarDoClienteAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        var ag = meus.FirstOrDefault(a => a.Id == id);
        return ag is null ? RedirectToAction(nameof(Index)) : View(ag);
    }

    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Meus() => View(await _agendamentos.ListarDoClienteAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)));
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Cancelar(int id)
    {
        try { await _agendamentos.AlterarStatusAsync(id, "Cancelado", int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), false); }
        catch (AcessoNegadoException) { return Forbid(); }
        catch (NegocioException e) { TempData["Mensagem"] = e.Message; }
        return RedirectToAction(nameof(Meus));
    }

    private async Task<AgendarViewModel> Preencher(AgendarViewModel vm)
    {
        vm.Servicos = await _servicos.ListarAsync();
        vm.Barbeiros = await _barbeiros.ListarAsync();
        return vm;
    }
}
