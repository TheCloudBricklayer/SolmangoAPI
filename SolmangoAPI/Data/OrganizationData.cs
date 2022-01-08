using Newtonsoft.Json;
using SolmangoAPI.Models;

namespace SolmangoAPI.Data;

[Serializable]
public class OrganizationData
{
    [JsonProperty("votes")]
    public List<string> Votes { get; init; }

    [JsonProperty("members")]
    public List<MemberModel> Members { get; init; }

    public OrganizationData()
    {
        Members = new List<MemberModel>();
        Votes = new List<string>();
    }

    public bool TryUpdateVote(string address, string vote, int power)
    {
        if (!Votes.Contains(vote) || power <= 0)
            return false;
        MemberModel? member = Members.Find(m => m.Address.Equals(address));
        if (member == null)
        {
            member ??= new MemberModel(address);
            if (!Members.Contains(member))
            {
                Members.Add(member);
            }
        }
        member.Vote = vote;
        member.LastVotePower = power;
        return true;
    }

    public MemberModel? GetMember(string address) => Members.Find(m => m.Address.Equals(address));

    public void DeleteMember(MemberModel member) => Members.RemoveAll(m => m.Equals(member));

    public void DeleteMember(string memberAddress) => Members.RemoveAll(m => m.Address.Equals(memberAddress));

    public void PutMember(MemberModel member)
    {
        Members.RemoveAll(m => m.Equals(member));
        Members.Add(member);
    }

    public Dictionary<string, float> GetVotePercentages()
    {
        int total = 0;
        Dictionary<string, int> percentages = new Dictionary<string, int>();
        foreach (string elem in Votes)
        {
            int count = 0;
            Members.ForEach(m => count += elem.Equals(m.Vote) ? m.LastVotePower : 0);
            percentages.Add(elem, count);
            total += count;
        }
        Dictionary<string, float> tot = new Dictionary<string, float>();
        foreach (var pair in percentages)
        {
            tot.Add(pair.Key, total > 0 ? (float)pair.Value / total : 0.5F);
        }
        return tot;
    }
}