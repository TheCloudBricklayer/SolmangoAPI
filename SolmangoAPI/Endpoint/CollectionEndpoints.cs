using BetterHaveIt.Repositories;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        app.MapGet("api/collection/metadata", HandleGETMetadata).AllowAnonymous();
    }

    public void DefineServices(IServiceCollection services, IConfiguration configuration)
    {
        var candyMachineRepository = new RepositoryJson<CandyMachineModel>(configuration.GetSection("Preferences:CandyMachineFilePath").Get<string>());

        services.AddSingleton<IRepository<CandyMachineModel>>(candyMachineRepository);
    }

    public void OnShutdown(IServiceProvider services)
    {
        var candyRepository = services.GetService<IRepository<CandyMachineModel>>();
        candyRepository?.Save();
    }

    private async Task<IResult> HandleGETMetadata(ILogger<OrganizationEndpoints> logger, IServiceProvider serviceProvider, [FromQuery] int id)
    {
        IRepository<CandyMachineModel>? candyRepository = serviceProvider.GetService<IRepository<CandyMachineModel>>();
        if (candyRepository is null)
        {
            return Results.Problem("Unable to retrieve required services from backend", null, StatusCodes.Status500InternalServerError, "Resource exception", "Error");
        }
        if (!candyRepository.Data.Items.ContainsKey(id))
        {
            return Results.NotFound($"Invalid mint id [{id}]");
        }
        var elem = candyRepository.Data.Items[id];
        var req = await httpClient.GetAsync(elem.Link);
        if (!req.IsSuccessStatusCode)
        {
            return Results.Problem("Unable to retrieve arewave metadata", null, StatusCodes.Status502BadGateway, "Metadata exception", "Error");
        }

        string content = await req.Content.ReadAsStringAsync();
        TokenMetadataModel? tokenMetadataModel = JsonConvert.DeserializeObject<TokenMetadataModel>(content);
        if (tokenMetadataModel is null)
        {
            return Results.Problem("Unable to deserialize arewave metadata", null, StatusCodes.Status500InternalServerError, "Metadata exception", "Error");
        }
        tokenMetadataModel.RarityScore = elem.RarityOrder;
        return Results.Ok(tokenMetadataModel);
    }
}