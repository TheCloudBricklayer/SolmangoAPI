using Newtonsoft.Json;

namespace SolmangoAPI.Models;

[Serializable]
public class MemberModel : IEquatable<MemberModel>
{
    [JsonProperty("address")]
    public string Address { get; private set; }

    [JsonProperty("whitelisted")]
    public bool Whitelisted { get; set; }

    [JsonProperty("vote")]
    public string Vote { get; set; }

    [JsonProperty("last_vote_power")]
    public int LastVotePower { get; set; }

    [JsonProperty("promised")]
    public int Promised { get; set; }

    public MemberModel(string address)
    {
        Address = address;
        Whitelisted = false;
        Vote = string.Empty;
        LastVotePower = 0;
        Promised = 0;
    }

    public bool Equals(MemberModel? other) => other is not null && other.Address == Address;
}