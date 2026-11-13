namespace BarbeariaDoZe.Modelos;

/// <summary>Classe base abstrata: toda entidade persistida possui um identificador.</summary>
public abstract class EntidadeBase : IValidavel
{
    public int Id { get; set; }

    // Cada entidade derivada fornece as suas validações.
    public abstract IEnumerable<string> Validar();

    public bool EhValida() => !Validar().Any();
}
