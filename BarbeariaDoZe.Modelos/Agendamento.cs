namespace BarbeariaDoZe.Modelos;

/// <summary>Registro central do sistema: relaciona cliente, barbeiro e serviço em um horário.</summary>
public class Agendamento : EntidadeBase
{
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public decimal? Preco { get; set; }
    public string Status { get; set; } = "Confirmado";   // Confirmado | Concluido | Cancelado

    public int UsuarioId { get; set; }
    public int? BarbeiroId { get; set; }
    public int ServicoId { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Barbeiro? Barbeiro { get; set; }
    public Servico Servico { get; set; } = null!;

    public override IEnumerable<string> Validar()
    {
        if (DataHoraFim <= DataHoraInicio) yield return "O término deve ser posterior ao início.";
        if (Status is not ("Confirmado" or "Concluido" or "Cancelado")) yield return "Status inválido.";
    }
}
