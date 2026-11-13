namespace BarbeariaDoZe.Modelos;

/// <summary>
/// Contrato de validação. Cada entidade implementa a sua própria regra
/// (polimorfismo): quem chama só precisa conhecer a interface.
/// </summary>
public interface IValidavel
{
    IEnumerable<string> Validar();
}
