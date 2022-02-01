using Newtonsoft.Json;
using SolmangoNET.Models;

namespace SolmangoAPI.Models;

[Serializable]
public class CollectionRaritiesModel
{
    [JsonProperty("rarities")]
    public List<RarityModel> Rarities { get; init; }

    public CollectionRaritiesModel()
    {
        Rarities = new List<RarityModel>();
    }
}