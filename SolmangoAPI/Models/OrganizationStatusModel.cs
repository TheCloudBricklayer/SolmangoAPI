namespace SolmangoAPI.Models;

public class OrganizationStatusModel
{
    public ulong Balance { get; init; }

    public Dictionary<string, float> Votes { get; init; }

    public OrganizationStatusModel()
    {
        Votes = new Dictionary<string, float>();
    }
}