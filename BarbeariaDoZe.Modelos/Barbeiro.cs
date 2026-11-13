namespace BarbeariaDoZe.Modelos;

/// <summary>Profissional da barbearia. Todo barbeiro também é um Usuario (mesma chave).</summary>
public class Barbeiro : EntidadeBase
{
    public DateTime DataAdmissao { get; set; }
    public string? Foto { get; set; }
    public decimal Avaliacao { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public ICollection<HorarioTrabalho> Horarios { get; set; } = new List<HorarioTrabalho>();

    public string Nome => Usuario?.Nome ?? string.Empty;

    public override IEnumerable<string> Validar()
    {
        if (Avaliacao is < 0 or > 5)
            yield return "A avaliação deve estar entre 0 e 5.";
    }
}
