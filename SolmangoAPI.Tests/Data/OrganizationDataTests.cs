using SolmangoAPI.Data;
using SolmangoNET.Models;
using Xunit;

namespace SolmangoAPI.Tests.Endpoints;

public class OrganizationDataTests
{
    public static readonly IEnumerable<object[]> VOTE_PERCENTAGES_DATA =
    new List<object[]>
    {
            new object[]
            {
                new string[] { "vote_1", "vote_2" },
                new (string, string, int)[] { },
                new Dictionary<string, float>() { { "vote_1", 0.5F }, { "vote_2", 0.5F } }
            },
            new object[]
            {
                new string[] { "vote_1", "vote_2" },
                new (string, string, int)[] { ("address_1", "vote_1", 1) },
                new Dictionary<string, float>() { { "vote_1", 1F }, { "vote_2", 0F } }
            },
            new object[]
            {
                new string[] { "vote_1", "vote_2" },
                new (string, string, int)[] { ("address_1", "vote_1", 1), ("address_1", "vote_2", 1) },
                new Dictionary<string, float>() { { "vote_1", 0F }, { "vote_2", 1F } }
            },
            new object[]
            {
                new string[] { "vote_1", "vote_2" },
                new (string, string, int)[] { ("address_1", "vote_1", 1), ("address_2", "vote_2", 3) },
                new Dictionary<string, float>() { { "vote_1", 0.25F }, { "vote_2", 0.75F } }
            },
            new object[]
            {
                new string[] { "vote_1", "vote_2" },
                new (string, string, int)[] { ("address_1", "vote_1", 0), ("address_2", "vote_2", -1) },
                new Dictionary<string, float>() { { "vote_1", 0.5F }, { "vote_2", 0.5F } }
            },
            new object[]
            {
                new string[] { "vote_1", "vote_2" },
                new (string, string, int)[] { ("address_1", "vote_3", 0) },
                new Dictionary<string, float>() { { "vote_1", 0.5F }, { "vote_2", 0.5F } }
            },
            new object[]
            {
                new string[] { "vote_1", "vote_2" },
                new (string, string, int)[] {("address_1", "vote_1", 1), ("address_2", "vote_2", 1) },
                new Dictionary<string, float>() { { "vote_1", 0.5F }, { "vote_2", 0.5F } }
            },
    };

    [Theory]
    [MemberData(nameof(VOTE_PERCENTAGES_DATA))]
    public void VotesPercentagesShouldBe(string[] votes, (string, string, int)[] votersPreferences, Dictionary<string, float> expectedVotes)
    {
        OrganizationData organizationData = new OrganizationData() { Votes = votes.ToList() };
        foreach (var pref in votersPreferences)
        {
            organizationData.TryUpdateVote(pref.Item1, pref.Item2, pref.Item3);
        }
        foreach (var vote in organizationData.GetVotePercentages())
        {
            Assert.True(expectedVotes.TryGetValue(vote.Key, out float percentage));
            Assert.Equal(vote.Value, percentage);
        }
    }

    [Fact]
    public void DeleteMember()
    {
        MemberModel first = new MemberModel("address_1");
        MemberModel second = new MemberModel("address_2");
        OrganizationData organizationData = new OrganizationData() { Members = new List<MemberModel> { first, second } };
        organizationData.DeleteMember(first);
        Assert.Single(organizationData.Members);
        organizationData.DeleteMember(second.Address);
        Assert.Empty(organizationData.Members);
    }

    [Fact]
    public void PutMember()
    {
        OrganizationData organizationData = new OrganizationData();
        organizationData.PutMember(new MemberModel("address_1"));
        Assert.Single(organizationData.Members);
        MemberModel member = new MemberModel("address_1") { Promised = 1 };
        organizationData.PutMember(member);
        var getMember = organizationData.GetMember(member.Address);
        Assert.NotNull(getMember);
        Assert.Equal(1, getMember!.Promised);
    }
}