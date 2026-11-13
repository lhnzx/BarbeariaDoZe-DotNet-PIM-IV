namespace BarbeariaDoZe.Modelos;

/// <summary>Serviço oferecido (corte, barba, combo, luzes etc.).</summary>
public class Servico : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public TimeSpan? TempoEstimado { get; set; }

    public override IEnumerable<string> Validar()
    {
        if (string.IsNullOrWhiteSpace(Nome)) yield return "O nome do serviço é obrigatório.";
        if (Preco <= 0) yield return "O preço deve ser maior que zero.";
    }
}
