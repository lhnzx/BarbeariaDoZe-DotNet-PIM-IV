using BarbeariaDoZe.Negocio;
using Microsoft.AspNetCore.Mvc;

namespace BarbeariaDoZe.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Erro() { Response.StatusCode = 500; return Content("Não foi possível concluir a operação. Tente novamente mais tarde."); }
    public IActionResult Contato() => View();
}

public class ServicosController : Controller
{
    private readonly ServicoNegocio _negocio;
    public ServicosController(ServicoNegocio negocio) => _negocio = negocio;

    // O controller só recebe a requisição, chama o Negócio e devolve a view.
    public async Task<IActionResult> Index() => View(await _negocio.ListarAsync());
}
