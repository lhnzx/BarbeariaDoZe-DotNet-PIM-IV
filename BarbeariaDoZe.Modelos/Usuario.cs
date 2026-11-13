namespace BarbeariaDoZe.Modelos;

/// <summary>Pessoa cadastrada no sistema: cliente, barbeiro ou administrador.</summary>
public class Usuario : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Cliente";   // Admin | Cliente | Barbeiro

    // Nunca são expostos pelos DTOs da API.
    public string? SenhaHash { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public string? RecuperacaoHash { get; set; }
    public DateTime? RecuperacaoExpiraEmUtc { get; set; }

    public override IEnumerable<string> Validar()
    {
        if (string.IsNullOrWhiteSpace(Nome) || !Nome.All(c => char.IsLetter(c) || c == ' '))
            yield return "O nome deve conter apenas letras e espaços.";
        if (string.IsNullOrWhiteSpace(Telefone) || !Telefone.All(char.IsDigit) || Telefone.Length is < 10 or > 11)
            yield return "O telefone deve conter apenas números (DDD + número).";
        if (string.IsNullOrWhiteSpace(Email) || !System.Net.Mail.MailAddress.TryCreate(Email, out var endereco) || endereco.Address != Email)
            yield return "O e-mail informado é inválido.";
        if (Nome.Length > 100 || Email.Length > 255) yield return "Nome ou e-mail excede o limite permitido.";
        if (Tipo is not ("Admin" or "Cliente" or "Barbeiro"))
            yield return "Tipo de usuário inválido.";
    }
}
