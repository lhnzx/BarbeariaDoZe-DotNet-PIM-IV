using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using BarbeariaDoZe.Repositorio;
namespace BarbeariaDoZe.Negocio;

public class RecuperacaoSenhaNegocio(IUsuarioRepositorio repo, IConfiguration config)
{
    /// <summary>
    /// Gera o link de uso único. Com SMTP configurado, envia por e-mail e retorna null.
    /// Sem SMTP e com Demo:Ativo = true (apresentação local), retorna o link para ser exibido na tela.
    /// </summary>
    public async Task<Uri?> SolicitarAsync(string email)
    {
        var host = config["Smtp:Host"];
        var origem = config["Smtp:From"];
        var url = config["PublicUrls:Web"];
        var semSmtp = string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(origem);
        var demonstracao = semSmtp && config.GetValue<bool>("Demo:Ativo");
        // Configuração verificada antes da busca: resposta não revela se a conta existe.
        if ((semSmtp && !demonstracao) || !Uri.TryCreate(url, UriKind.Absolute, out var baseUrl))
            throw new NegocioException("Recuperação indisponível. O responsável precisa configurar o envio de e-mail.");
        var u = await repo.ObterPorEmailAsync(email.Trim().ToLowerInvariant());
        if (u is null) return null;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        u.RecuperacaoHash = Hash(token);
        u.RecuperacaoExpiraEmUtc = DateTime.UtcNow.AddMinutes(30);
        await repo.AtualizarSegurancaAsync(u);
        var link = new Uri(baseUrl, "/Conta/Redefinir?token=" + Uri.EscapeDataString(token));
        if (demonstracao) return link;
        using var mail = new MailMessage(origem!, u.Email, "Redefinição de senha — Barbearia do Zé",
            $"Use o link para definir sua senha: {link}\nO link expira em 30 minutos e só pode ser usado uma vez. Se não solicitou, ignore este e-mail.");
        using var smtp = new SmtpClient(host, config.GetValue<int?>("Smtp:Port") ?? 587)
        { EnableSsl = config.GetValue<bool?>("Smtp:EnableSsl") ?? true };
        if (!string.IsNullOrWhiteSpace(config["Smtp:Username"]))
            smtp.Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"]);
        try { await smtp.SendMailAsync(mail); }
        catch (SmtpException) { throw new NegocioException("Não foi possível enviar o e-mail. Tente novamente mais tarde."); }
        return null;
    }
    public async Task RedefinirAsync(string token, string senha)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 64) throw new NegocioException("Link inválido ou expirado.");
        var hash = SegurancaSenha.CriarHash(senha);
        if (!await repo.ConsumirRecuperacaoAsync(Hash(token), hash, DateTime.UtcNow))
            throw new NegocioException("Link inválido, expirado ou já utilizado.");
    }
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}


