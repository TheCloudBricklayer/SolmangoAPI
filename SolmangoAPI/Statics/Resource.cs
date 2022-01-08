namespace SolmangoAPI;

public static class Resource
{
    public static class Balance
    {
        public const string KEY = "creator_balance";
        public const ulong CACHE_TIME_S = 10;
    }

    public static class Tokens
    {
        public static class Organization
        {
            public const string KEY = "tokens_organization";
            public const ulong CACHE_TIME_S = 5;
        }
    }
}