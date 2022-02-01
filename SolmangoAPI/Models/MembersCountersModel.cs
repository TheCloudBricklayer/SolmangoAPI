using SolmangoNET.Models;

namespace SolmangoAPI.Models;

public class MembersCountersModel
{
    public int Count { get; init; }

    public int WhitelistedCount { get; init; }

    public int TotalPromised { get; init; }

    public List<MemberModel> PromisedMembers { get; init; }

    public MembersCountersModel()
    {
        PromisedMembers = new List<MemberModel>();
    }
}