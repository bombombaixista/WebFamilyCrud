namespace WebFamilyCrud.Models;

public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Relacionamento
    public int GrupoId { get; set; }
    public Grupo? Grupo { get; set; }
}

public class Grupo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    public List<Cliente> Clientes { get; set; } = new();
}
