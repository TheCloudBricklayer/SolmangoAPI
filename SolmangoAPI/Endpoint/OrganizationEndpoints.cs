using BetterHaveIt.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SolmangoAPI.Data;
using SolmangoAPI.Models;
using SolmangoNET;
using SolmangoNET.Models;
using SolmangoNET.Rpc;
using Solnet.Rpc;
using Solnet.Wallet;

namespace SolmangoAPI.Endpoint;

public class OrganizationEndpoints : IEndpointDefinition, IEndpointShutdownHandler
{
    public void DefineEndpoints(WebApplication app)
    {
        app.MapGet("api/organization/status", HandleGETStatusEndpoint).AllowAnonymous();
        app.MapPost("api/organization/vote", HandlePOSTVoteEndpoint);

        app.MapGet("api/organization/members", HandleGETMemberEndpoint).AllowAnonymous();
        app.MapPut("api/organization/members", HandlePUTMember);
        app.MapDelete("api/organization/members", HandleDELETEMember);
    }

    public void DefineServices(IServiceCollection services, IConfiguration configuration)
    {
        var organizationRepository = new RepositoryJson<OrganizationData>(configuration.GetSection("Preferences:OrganizationFilePath").Get<string>());
        var collectionRepository = new RepositoryJson<CollectionModel>(configuration.GetSection("Preferences:CollectionFilePath").Get<string>());

        services.AddSingleton<IRepository<CollectionModel>>(collectionRepository);
        services.AddSingleton<IRepository<OrganizationData>>(organizationRepository);
    }

    public void OnShutdown(IServiceProvider services)
    {
        var orgRepo = services.GetService<IRepository<OrganizationData>>();
        orgRepo?.Save();
    }

    private async Task<IResult> HandlePUTMember(ILogger<OrganizationEndpoints> logger, IServiceProvider serviceProvider, [FromBody] MemberModel member)
    {
        IMemoryCache? cache = serviceProvider.GetService<IMemoryCache>();
        IRepository<OrganizationData>? organizationRepo = serviceProvider.GetService<IRepository<OrganizationData>>();
        IRpcClient? rpcClient = serviceProvider.GetService<IRpcClient>();
        IRpcScheduler? rpcScheduler = serviceProvider.GetService<IRpcScheduler>();
        if (organizationRepo is null || rpcClient is null || rpcScheduler is null)
            return Results.Problem("Unable to retrieve required services from backend", null, StatusCodes.Status500InternalServerError, "Resource exception", "Error");

        var oneOf = rpcScheduler.Schedule(() => rpcClient.GetBalanceAsync(member.Address));
        if (oneOf.TryPickT1(out var saturatedEx, out var token)) return Results.Problem("RPC scheduler saturated", null, StatusCodes.Status503ServiceUnavailable, "Internal exception", "Error");

        var res = await token;
        if (!res.WasRequestSuccessfullyHandled) return Results.Problem($"RPC error[{res.ServerErrorCode}]: {res.Reason}", null, StatusCodes.Status502BadGateway, "RPC exception", "Error");

        organizationRepo.Data.PutMember(member);
        organizationRepo.SaveAsync();
        return Results.Ok();
    }

    private async Task<IResult> HandleGETStatusEndpoint(ILogger<OrganizationEndpoints> logger, IServiceProvider serviceProvider)
    {
        IMemoryCache? cache = serviceProvider.GetService<IMemoryCache>();
        IRepository<OrganizationData>? organizationRepo = serviceProvider.GetService<IRepository<OrganizationData>>();
        IRepository<CollectionModel>? collectionRepo = serviceProvider.GetService<IRepository<CollectionModel>>();
        if (organizationRepo is null || collectionRepo is null)
            return Results.Problem("Unable to retrieve required services from backend", null, StatusCodes.Status500InternalServerError, "Resource exception", "Error");
        ulong balance;
        if (cache is not null && cache.TryGetValue(Resource.Balance.KEY, out ulong cachedBalance))
        {
            balance = cachedBalance;
        }
        else
        {
            IConfiguration? configuration = serviceProvider.GetService<IConfiguration>();
            IRpcClient? rpcClient = serviceProvider.GetService<IRpcClient>();
            IRpcScheduler? rpcScheduler = serviceProvider.GetService<IRpcScheduler>();

            if (rpcScheduler is null || rpcClient is null || configuration is null)
                return Results.Problem("Unable to retrieve required services from backend", null, StatusCodes.Status500InternalServerError, "Resource exception", "Error");

            string address = configuration.GetSection("Preferences:CreatorPublicKey").Get<string>();

            var oneOf = rpcScheduler.Schedule(() => rpcClient.GetBalanceAsync(address));
            if (oneOf.TryPickT1(out var saturatedEx, out var rpcJobToken)) return Results.Problem("RPC scheduler saturated", null, StatusCodes.Status503ServiceUnavailable, "Internal exception", "Error");
            var res = await rpcJobToken;
            if (!res.WasRequestSuccessfullyHandled) return Results.Problem($"RPC error[{res.ServerErrorCode}]: {res.Reason}", null, StatusCodes.Status502BadGateway, "RPC exception", "Error");
            balance = (ulong)(res.Result.Value * (configuration.GetValue<ulong>("Preferences:OrganizationPercentage") / 100F));
            cache.Set(Resource.Balance.KEY, balance, TimeSpan.FromSeconds(Resource.Balance.CACHE_TIME_S));
        }

        return Results.Ok(new OrganizationStatusModel() { Collection = collectionRepo.Data, Balance = balance, Votes = organizationRepo.Data.GetVotePercentages() });
    }

    private IResult HandleDELETEMember(ILogger<OrganizationEndpoints> logger, IServiceProvider serviceProvider, [FromQuery] string address)
    {
        IRepository<OrganizationData>? organizationRepo = serviceProvider.GetService<IRepository<OrganizationData>>();
        if (organizationRepo is null)
            return Results.Problem("Unable to retrieve required services from backend", null, StatusCodes.Status500InternalServerError, "Resource exception", "Error");

        organizationRepo.Data.DeleteMember(address);
        organizationRepo.SaveAsync();
        return Results.Ok();
    }

    //private IResult HandleGETCollectionEndpoint(ILogger<OrganizationEndpoints> logger, IServiceProvider serviceProvider)
    //{
    //    IRepository<CollectionData>? collectionRepository = serviceProvider.GetService<IRepository<CollectionData>>();
    //    return collectionRepository is null
    //        ? Results.Problem("Unable to retrieve required services from backend", null, StatusCodes.Status500InternalServerError, "Resource exception", "Error")
    //        : Results.Ok(new CollectionModel() { Name = collectionRepository.Data.Name, Symbol = collectionRepository.Data.Symbol, Mints = collectionRepository.Data.Mints.Count });
    //}

    private IResult HandleGETMemberEndpoint(ILogger<OrganizationEndpoints> logger, IServiceProvider serviceProvider, [FromQuery] string? address)
    {
        IRepository<OrganizationData>? organizationRepo = serviceProvider.GetService<IRepository<OrganizationData>>();
        IRepository<CollectionData>? collectionRepo = serviceProvider.GetService<IRepository<CollectionData>>();
        IRpcScheduler? rpcScheduler = serviceProvider.GetService<IRpcScheduler>();
        IRpcClient? rpcClient = serviceProvider.GetService<IRpcClient>();
        if (organizationRepo is null || rpcScheduler is null || rpcClient is null || collectionRepo is null)
        {
            return Results.Problem("Unable to retrieve required services from backend", null, StatusCodes.Status500InternalServerError, "Resource exception", "Error");
        }
        if (address is null)
        {
            int count = organizationRepo.Data.Members.Count;
            int whitelistedCount = 0;
            int promisedCount = 0;
            foreach (var val in organizationRepo.Data.Members)
            {
                if (val.Whitelisted)
                {
                    whitelistedCount++;
                }
                promisedCount += val.Promised;
            }
            return Results.Ok(new MembersCountersModel() { Count = count, WhitelistedCount = whitelistedCount, TotalPromised = promisedCount });
        }

        MemberModel? member = organizationRepo.Data.Members.Find(m => m.Address.Equals(address));
        member ??= new MemberModel(address);

        return Results.Ok(member);
    }

    private async Task<IResult> HandlePOSTVoteEndpoint(ILogger<OrganizationEndpoints> logger, IServiceProvider serviceProvider, [FromQuery] string? address, [FromQuery] string? vote)
    {
        if (address is null || vote is null) return Results.Problem("Invalid synthax", null, StatusCodes.Status400BadRequest, "Synthax exception", "Error");
        IRepository<OrganizationData>? organizationRepo = serviceProvider.GetService<IRepository<OrganizationData>>();
        IRepository<CollectionData>? collectionRepo = serviceProvider.GetService<IRepository<CollectionData>>();
        IRpcScheduler? rpcScheduler = serviceProvider.GetService<IRpcScheduler>();
        IRpcClient? rpcClient = serviceProvider.GetService<IRpcClient>();
        if (organizationRepo is null || rpcScheduler is null || rpcClient is null || collectionRepo is null)
        {
            return Results.Problem("Unable to retrieve required services from backend", null, StatusCodes.Status500InternalServerError, "Resource exception", "Error");
        }

        var oneOf = rpcScheduler.Schedule(() => Solmango.FilterMintsByOwner(rpcClient, collectionRepo.Data.Mints, new PublicKey(address)));
        if (oneOf.TryPickT1(out var exception, out var token))
        {
            return Results.Problem("RPC scheduler saturated", null, StatusCodes.Status503ServiceUnavailable, "Internal exception", "Error");
        }
        var res = await token;
        if (res.TryPickT1(out var solmangoRpcException, out var mints))
        {
            return Results.Problem($"RPC error[{solmangoRpcException.Code}]: {solmangoRpcException.Reason}", null, StatusCodes.Status502BadGateway, "RPC exception", "Error");
        }

        if (mints.Count <= 0)
        {
            return Results.Problem($"Address {address} has no voting power", null, StatusCodes.Status403Forbidden, "Exception", "Error");
        }

        var succeed = organizationRepo.Data.TryUpdateVote(address, vote, mints.Count);
        if (succeed) organizationRepo.SaveAsync();
        return succeed ? Results.Ok()
            : Results.Problem($"Vote {vote} does not exists", null, StatusCodes.Status422UnprocessableEntity, "Exception", "Error");
    }
}