using System.Security.Claims;
using BarbeariaDoZe.Negocio;
using BarbeariaDoZe.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace BarbeariaDoZe.Web.Controllers;

public class ContaController(UsuarioNegocio usuarios, RecuperacaoSenhaNegocio recuperacao) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("conta")]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Erro = "Informe e-mail e senha."; return View(vm); }
        try
        {
            var u = await usuarios.AutenticarAsync(vm.Email, vm.Senha);
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim(ClaimTypes.Name, u.Nome), new Claim(ClaimTypes.Role, u.Tipo), new Claim("stamp", u.SecurityStamp) };
            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")),
                // "Lembrar de mim" mantém a sessão por 30 dias; sem marcar, a sessão dura enquanto o navegador estiver aberto.
                new AuthenticationProperties { IsPersistent = vm.Lembrar, ExpiresUtc = vm.Lembrar ? DateTimeOffset.UtcNow.AddDays(30) : null, AllowRefresh = true });
            if (Url.IsLocalUrl(vm.ReturnUrl)) return LocalRedirect(vm.ReturnUrl!);
            return u.Tipo == "Admin" ? RedirectToAction("Index", "Admin") : RedirectToAction("Meus", "Agendar");
        }
        catch (NegocioException e) { vm.Erro = e.Message; return View(vm); }
    }
    [HttpGet] public IActionResult Cadastro() => View(new CadastroViewModel());
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("conta")]
    public async Task<IActionResult> Cadastro(CadastroViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Erro = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)); return View(vm); }
        try
        {
            await usuarios.CadastrarClienteAsync(vm.Nome, vm.Telefone, vm.Email, vm.Senha);
            TempData["Mensagem"] = "Conta criada com sucesso! Entre com seu e-mail e senha.";
            return RedirectToAction(nameof(Login));
        }
        catch (NegocioException e) { vm.Erro = e.Message; return View(vm); }
    }
    [HttpGet] public IActionResult Senha() => View(new RecuperacaoViewModel());
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("conta")]
    public async Task<IActionResult> Senha(RecuperacaoViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Erro = true; vm.Mensagem = "Informe um e-mail válido."; return View(vm); }
        try
        {
            var linkDemo = await recuperacao.SolicitarAsync(vm.Email);
            vm.LinkDemonstracao = linkDemo?.ToString();
            vm.Mensagem = "Se houver uma conta com esse e-mail, enviaremos as instruções.";
        }
        catch (NegocioException e) { vm.Erro = true; vm.Mensagem = e.Message; }
        return View(vm);
    }
    [HttpGet] public IActionResult Redefinir(string token) => View(new RedefinirViewModel { Token = token });
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("conta")]
    public async Task<IActionResult> Redefinir(RedefinirViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Mensagem = "Informe senhas iguais, com 12 a 128 caracteres."; return View(vm); }
        try { await recuperacao.RedefinirAsync(vm.Token, vm.Senha); vm.Sucesso = true; vm.Mensagem = "Senha atualizada. Entre com a nova senha."; }
        catch (NegocioException e) { vm.Mensagem = e.Message; }
        return View(vm);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sair() { await HttpContext.SignOutAsync("Cookies"); return RedirectToAction("Index", "Home"); }
    public IActionResult AcessoNegado() { Response.StatusCode = 403; return View(); }
}

