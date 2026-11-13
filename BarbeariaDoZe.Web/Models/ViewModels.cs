using BarbeariaDoZe.Modelos;
using System.ComponentModel.DataAnnotations;

namespace BarbeariaDoZe.Web.Models;

/// <summary>ViewModel da tela de agendamento: reúne os dados exibidos e os campos preenchidos.</summary>
public class AgendarViewModel
{
    public List<Servico> Servicos { get; set; } = new();
    public List<Barbeiro> Barbeiros { get; set; } = new();

    [Required(ErrorMessage = "Escolha um serviço.")] public int? ServicoId { get; set; }
    [Required(ErrorMessage = "Escolha um profissional.")] public int? BarbeiroId { get; set; }
    [Required(ErrorMessage = "Informe a data.")] public DateTime? Data { get; set; }
    [Required(ErrorMessage = "Escolha um horário.")] public string? Hora { get; set; }

}

public class LoginViewModel
{
    public string? ReturnUrl { get; set; }
    public bool Lembrar { get; set; }
    [Required(ErrorMessage = "Informe o e-mail.")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe a senha.")] public string Senha { get; set; } = string.Empty;
    public string? Erro { get; set; }
}

public class CadastroViewModel
{
    [Required, StringLength(128, MinimumLength = 12, ErrorMessage = "A senha deve ter entre 12 e 128 caracteres.")] public string Senha { get; set; } = "";
    [Compare(nameof(Senha), ErrorMessage = "As senhas devem ser iguais.")] public string ConfirmarSenha { get; set; } = "";
    [Required(ErrorMessage = "Informe o nome.")] public string Nome { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe o e-mail.")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe o telefone.")] public string Telefone { get; set; } = string.Empty;
    public string? Erro { get; set; }
    public bool Sucesso { get; set; }
}

public class PainelViewModel
{
    public List<Agendamento> Hoje { get; set; } = new();
    public List<Agendamento> Todos { get; set; } = new();
    public List<Usuario> Clientes { get; set; } = new();
    public List<RelatorioServico> RelatorioDia { get; set; } = new();
    public List<RelatorioServico> RelatorioPeriodo { get; set; } = new();
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string? Mensagem { get; set; }
}

public class RecuperacaoViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    public string? Mensagem { get; set; }
    public bool Erro { get; set; }
    /// <summary>Preenchido somente no modo demonstração, quando não há SMTP configurado.</summary>
    public string? LinkDemonstracao { get; set; }
}
public class RedefinirViewModel
{
    [Required] public string Token { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12)] public string Senha { get; set; } = "";
    [Compare(nameof(Senha))] public string ConfirmarSenha { get; set; } = "";
    public string? Mensagem { get; set; }
    public bool Sucesso { get; set; }
}

