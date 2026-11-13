namespace BarbeariaDoZe.Modelos;

/// <summary>Linha do relatório retornado pela stored procedure sp_RelatorioServicos.</summary>
public class RelatorioServico
{
    public string Servico { get; set; } = string.Empty;
    public int TotalAgendamentos { get; set; }
    public int Concluidos { get; set; }
    public int Cancelados { get; set; }
    public decimal Receita { get; set; }
    public decimal TaxaCancelamento { get; set; }
}
