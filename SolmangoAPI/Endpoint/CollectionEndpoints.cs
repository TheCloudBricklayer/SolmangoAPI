using BetterHaveIt.Repositories;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SolmangoAPI.Models;
using SolmangoNET.Models;

namespace SolmangoAPI.Endpoint;

public class CollectionEndpoints : IEndpointDefinition, IEndpointShutdownHandler
{
    private readonly HttpClient httpClient;

    public CollectionEndpoints()
    {
        httpClient = new HttpClient();
    }

    public void DefineEndpoints(WebApplication app)
    {
        app.MapGet("api/collection/info", HandleGETMetadata).AllowAnonymous();
    }

    public void DefineServices(IServiceCollection services, IConfiguration configuration)
    {
        var collectionRaritiesRepo = new RepositoryJson<CollectionRaritiesModel>(configuration.GetValue<string>("Preferences:RaritiesFilePath"));
        var candyMachineRepository = new RepositoryJson<CandyMachineModel>(configuration.GetValue<string>("Preferences:CandyMachineFilePath"));

        services.AddSingleton<IRepository<CandyMachineModel>>(candyMachineRepository);
        services.AddSingleton<IRepository<CollectionRaritiesModel>>(collectionRaritiesRepo);
    }

    public void OnShutdown(IServiceProvider services)
    {
    }

    private async Task<IResult> HandleGETMetadata(ILogger<OrganizationEndpoints> logger, IServiceProvider serviceProvider, [FromQuery] int id)
    {
        IRepository<CandyMachineModel>? candyRepository = serviceProvider.GetService<IRepository<CandyMachineModel>>();
        IRepository<CollectionRaritiesModel>? raritiesRepo = serviceProvider.GetService<IRepository<CollectionRaritiesModel>>();
        if (candyRepository is null)
        {
            return Results.Problem("Unable to retrieve required services from backend", null, StatusCodes.Status500InternalServerError, "Resource exception", "Error");
        }
        TokenMetadataModel? tokenMetadataModel = null;
        if (candyRepository.Data.Items.ContainsKey(id))
        {
            var elem = candyRepository.Data.Items[id];
            var req = await httpClient.GetAsync(elem.Link);
            if (!req.IsSuccessStatusCode)
            {
                return Results.Problem("Unable to retrieve arewave metadata", null, StatusCodes.Status502BadGateway, "Metadata exception", "Error");
            }

            string content = await req.Content.ReadAsStringAsync();
            tokenMetadataModel = JsonConvert.DeserializeObject<TokenMetadataModel>(content);
            if (tokenMetadataModel is null)
            {
                return Results.Problem("Unable to deserialize arewave metadata", null, StatusCodes.Status500InternalServerError, "Metadata exception", "Error");
            }
        }

        RarityModel? rarity = null;
        if (raritiesRepo is not null)
        {
            rarity = raritiesRepo.Data.Rarities.Find(r => r.Id == id);
        }
        MintInfoModel mintInfo = new MintInfoModel()
        {
            Rarity = rarity,
            Metadata = tokenMetadataModel
        };
        return Results.Ok(mintInfo);
    }
}