using System.ComponentModel.DataAnnotations;
using BarbeariaDoZe.Modelos;

namespace BarbeariaDoZe.Api.Dtos;

// DTOs: a API nunca expõe as entidades diretamente.
public record ServicoDto(int Id, string Nome, string Descricao, decimal Preco, int TempoMinutos)
{
    public static ServicoDto De(Servico s) =>
        new(s.Id, s.Nome, s.Descricao, s.Preco, (int)(s.TempoEstimado?.TotalMinutes ?? 0));
}

public record BarbeiroDto(int Id, string Nome, decimal Avaliacao)
{
    public static BarbeiroDto De(Barbeiro b) => new(b.Id, b.Nome, b.Avaliacao);
}

public record AgendamentoDto(int Id, string Cliente, string Servico, string Barbeiro,
                             DateTime Inicio, DateTime Fim, decimal Valor, string Status)
{
    public static AgendamentoDto De(Agendamento a) =>
        new(a.Id, a.Usuario.Nome, a.Servico.Nome, a.Barbeiro?.Nome ?? "-", a.DataHoraInicio, a.DataHoraFim, a.Preco ?? 0, a.Status);
}

public class NovoAgendamentoDto
{
    [Required] public string Nome { get; set; } = string.Empty;
    [Required] public string Telefone { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public int ServicoId { get; set; }
    [Required] public int BarbeiroId { get; set; }
    [Required] public DateTime Inicio { get; set; }
}

public class AlterarStatusDto
{
    [Required] public string Status { get; set; } = string.Empty;
}
