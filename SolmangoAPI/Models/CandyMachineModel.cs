using Newtonsoft.Json;

namespace SolmangoAPI.Models;

public class CandyMachineModel
{
    [JsonProperty("program")]
    public CandyMachinePropertiesModel CandyMachineProgram { get; init; }

    [JsonProperty("items")]
    public Dictionary<int, CandyMachineItemModel> Items { get; init; }

    public CandyMachineModel()
    {
        CandyMachineProgram = new CandyMachinePropertiesModel();
        Items = new Dictionary<int, CandyMachineItemModel>();
    }
}