namespace SolmangoAPI.Models;

public class CollectionModel
{
    public string Name { get; init; }

    public string Symbol { get; init; }

    public int Mints { get; init; }

    public CollectionModel()
    {
        Name = string.Empty;
        Symbol = string.Empty;
        Mints = 0;
    }
}