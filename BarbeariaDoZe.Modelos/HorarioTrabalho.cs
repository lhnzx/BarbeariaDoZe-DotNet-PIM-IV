namespace BarbeariaDoZe.Modelos;

/// <summary>Disponibilidade semanal de um barbeiro.</summary>
public class HorarioTrabalho : EntidadeBase
{
    public int? BarbeiroId { get; set; }
    public string DiaSemana { get; set; } = string.Empty;
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFim { get; set; }

    public override IEnumerable<string> Validar()
    {
        if (HoraFim <= HoraInicio) yield return "A hora final deve ser posterior à inicial.";
    }
}
