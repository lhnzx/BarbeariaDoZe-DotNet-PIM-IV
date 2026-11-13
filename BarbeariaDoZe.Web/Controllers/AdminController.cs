using BarbeariaDoZe.Negocio;
using BarbeariaDoZe.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarbeariaDoZe.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AgendamentoNegocio _agendamentos;
    private readonly UsuarioNegocio _usuarios;
    private readonly RelatorioNegocio _relatorios;

    public AdminController(AgendamentoNegocio agendamentos, UsuarioNegocio usuarios, RelatorioNegocio relatorios)
    {
        _agendamentos = agendamentos;
        _usuarios = usuarios;
        _relatorios = relatorios;
    }

    public async Task<IActionResult> Index(DateTime? inicio, DateTime? fim, string? mensagem)
    {
        var hoje = HorarioLocal.Hoje;
        var vm = new PainelViewModel
        {
            Inicio = inicio ?? new DateTime(hoje.Year, 1, 1),
            Fim = fim ?? new DateTime(hoje.Year, 12, 31),
            Mensagem = mensagem,
            Hoje = await _agendamentos.ListarDoDiaAsync(hoje),
            Todos = await _agendamentos.ListarTodosAsync(),
            Clientes = await _usuarios.ListarClientesAsync()
        };

        // As duas consultas abaixo executam a stored procedure sp_RelatorioServicos.
        vm.RelatorioDia = await _relatorios.ServicosAsync(hoje, hoje);
        try { vm.RelatorioPeriodo = await _relatorios.ServicosAsync(vm.Inicio, vm.Fim); }
        catch (NegocioException ex) { vm.Mensagem = ex.Message; }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(int id, string status)
    {
        try
        {
            await _agendamentos.AlterarStatusAsync(id, status, 0, true);
            return RedirectToAction(nameof(Index), new { mensagem = $"Agendamento #{id} atualizado para {status}." });
        }
        catch (NegocioException ex) { return RedirectToAction(nameof(Index), new { mensagem = ex.Message }); }
    }
}
