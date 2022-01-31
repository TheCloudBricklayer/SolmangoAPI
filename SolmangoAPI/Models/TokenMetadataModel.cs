// Copyright Matteo Beltrame

using Newtonsoft.Json;

namespace SolmangoAPI.Models;

[Serializable]
internal class TokenMetadataModel
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("rarityScore")]
    public int RarityScore { get; set; }

    [JsonProperty("symbol")]
    public string Symbol { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("seller_fee_basis_points")]
    public int SellerFeeBasisPoints { get; set; }

    [JsonProperty("image")]
    public string Image { get; set; }

    [JsonProperty("attributes")]
    public List<AttributeMetadata> Attributes { get; set; }

    [JsonProperty("properties")]
    public PropertiesMetadata Properties { get; set; }

    [JsonProperty("collection")]
    public CollectionMetadata Collection { get; set; }

    public TokenMetadataModel()
    {
        Name = string.Empty;
        Symbol = string.Empty;
        Description = string.Empty;
        Image = string.Empty;
        Attributes = new List<AttributeMetadata>();
        Properties = new PropertiesMetadata();
        Collection = new CollectionMetadata();
    }

    [Serializable]
    internal class PropertiesMetadata
    {
        [JsonProperty("files")]
        public List<FileMetadata> Files { get; set; }

        [JsonProperty("creators")]
        public List<CreatorMetadata> Creators { get; set; }

        public PropertiesMetadata()
        {
            Files = new List<FileMetadata>();
            Creators = new List<CreatorMetadata>();
        }

        [Serializable]
        internal class FileMetadata
        {
            [JsonProperty("uri")]
            public string Uri { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            public FileMetadata()
            {
                Uri = string.Empty;
                Type = string.Empty;
            }
        }

        [Serializable]
        internal class CreatorMetadata
        {
            [JsonProperty("address")]
            public string Address { get; set; }

            [JsonProperty("share")]
            public int Share { get; set; }

            public CreatorMetadata()
            {
                Address = string.Empty;
            }
        }
    }

    [Serializable]
    internal class CollectionMetadata
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("family")]
        public string Family { get; set; }

        public CollectionMetadata()
        {
            Name = string.Empty;
            Family = string.Empty;
        }
    }

    [Serializable]
    internal class AttributeMetadata : IEquatable<AttributeMetadata>
    {
        [JsonProperty("trait_type")]
        public string Trait { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }

        [JsonProperty("rarity")]
        public float Rarity { get; set; }

        public AttributeMetadata()
        {
            Trait = string.Empty;
            Value = string.Empty;
        }

        public bool Equals(AttributeMetadata? other) => other is not null && Trait.Equals(other.Trait);
    }
}