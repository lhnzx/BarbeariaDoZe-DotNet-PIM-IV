using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BarbeariaDoZe.Negocio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
namespace BarbeariaDoZe.Api.Controllers;

public record LoginDto([Required, EmailAddress] string Email, [Required, StringLength(128)] string Senha);
public record CadastroDto([Required, StringLength(100)] string Nome, [Required] string Telefone,
    [Required, EmailAddress, StringLength(255)] string Email, [Required, StringLength(128, MinimumLength = 12)] string Senha);
public record RecuperacaoDto([Required, EmailAddress] string Email);
[ApiController, Route("api/conta"), EnableRateLimiting("conta")]
public class ContaController(UsuarioNegocio usuarios, RecuperacaoSenhaNegocio recuperacao, IConfiguration config) : ControllerBase
{
    [HttpPost("cadastro")]
    public async Task<IActionResult> Cadastro(CadastroDto dto)
    {
        try { var u = await usuarios.CadastrarClienteAsync(dto.Nome, dto.Telefone, dto.Email, dto.Senha); return StatusCode(201, new { u.Id, u.Nome, u.Email }); }
        catch (NegocioException e) { return BadRequest(new { erro = e.Message }); }
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        try
        {
            var u = await usuarios.AutenticarAsync(dto.Email, dto.Senha);
            var expira = DateTime.UtcNow.AddHours(8);
            var claims = new[] { new Claim("sub", u.Id.ToString()), new Claim("name", u.Nome), new Claim("role", u.Tipo), new Claim("stamp", u.SecurityStamp), new Claim("jti", Guid.NewGuid().ToString("N")) };
            var jwt = new JwtSecurityToken("BarbeariaDoZe", "BarbeariaDoZe.Clientes", claims,
                notBefore: DateTime.UtcNow, expires: expira,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Auth:SigningKey"]!)), SecurityAlgorithms.HmacSha256));
            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(jwt), expiraEmUtc = expira, nome = u.Nome, email = u.Email, tipo = u.Tipo });
        }
        catch (NegocioException) { return Unauthorized(new { erro = "E-mail ou senha inválidos." }); }
    }
    [HttpPost("recuperacao")]
    public async Task<IActionResult> Recuperar(RecuperacaoDto dto)
    {
        try
        {
            var linkDemo = await recuperacao.SolicitarAsync(dto.Email);
            return Ok(new { mensagem = "Se houver uma conta com esse e-mail, enviaremos as instruções.", linkDemonstracao = linkDemo?.ToString() });
        }
        catch (NegocioException e) { return StatusCode(503, new { erro = e.Message }); }
    }
}

