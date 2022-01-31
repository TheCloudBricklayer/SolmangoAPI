namespace SolmangoAPI.Models;

public class OrganizationStatusModel
{
    public CollectionModel Collection { get; init; }

    public ulong Balance { get; init; }

    public Dictionary<string, float> Votes { get; init; }

    public OrganizationStatusModel()
    {
        Collection = new CollectionModel();
        Votes = new Dictionary<string, float>();
    }
}