using BetterHaveIt;
using HandierCli;
using Newtonsoft.Json;
using NFTGenerator.Metadata;
using SolmangoAPI.Endpoint;
using SolmangoAPI.Middleware;
using SolmangoAPI.Models;
using SolmangoNET.Models;
using SolmangoNET.Rpc;
using Solnet.Rpc;

#region Methods

void DebugAdjustRarities()
{
    //TODO adjust scores

    var rarities = Directory.GetFiles(@"C:\Users\matteo\Desktop\CollectionMetaBackup\rarities", "*.json");
    var metas = Directory.GetFiles(@"C:\Users\matteo\Desktop\CollectionMetaBackup", "*.json");
    //List<RarityMetadata> rar = new List<RarityMetadata>();

    HttpClient client = new HttpClient();
    List<RarityMetadata> elem = new List<RarityMetadata>();
    List<TokenMetadataModel> tokens = new List<TokenMetadataModel>();
    foreach (var rarity in rarities)
    {
        if (Serializer.DeserializeJson(string.Empty, rarity, out RarityMetadata meta))
        {
            elem.Add(meta);
        }
    }

    for (var i = 0; i < metas.Length; i++)
    {
        var meta = metas[i];
        if (Serializer.DeserializeJson<TokenMetadataModel>(string.Empty, meta, out var token))
        {
            tokens.Add(token);
        }
    }

    tokens = tokens.OrderBy(t =>
    {
        var index = t.Name.IndexOf('#');
        return double.Parse(t.Name[(index + 1)..]);
    }).ToList();
    elem = elem.OrderBy(e => e.Id).ToList();

    var zip = tokens.Zip(elem);

    Console.WriteLine(zip.Count());
    foreach (var pair in zip)
    {
        Console.WriteLine($"{pair.First.Name}, {pair.Second.Id}");
        double rar = CalculateRarity(pair.First);
        pair.Second.Rarity = rar;
    }
    Console.WriteLine("Max: " + elem.MinBy(e => e.Rarity).Id);
    Console.WriteLine("Min: " + elem.MaxBy(e => e.Rarity).Id);
    foreach (var rar in elem)
    {
        Serializer.SerializeJson($@"C:\Users\matteo\Desktop\CollectionMetaBackup\raritiesMod\", $@"{rar.Id}-rar.json", rar);
    }
    double CalculateRarity(TokenMetadataModel token)
    {
        double rarity = 1F;
        for (int i = 0; i < 9; i++)
        {
            if (i < token.Attributes.Count)
            {
                rarity += (token.Attributes[i].Rarity);
            }
            else
            {
                rarity += 30F;
            }
        }
        return rarity;
    }
    //rar = rar.OrderByDescending(r => r.Rarity).ToList();
    //Console.WriteLine("Rarest: " + rar.First().Id);
    //Console.WriteLine("Less: " + rar.Last().Id);
    elem = elem.OrderBy(e => e.Rarity).ToList();
    if (Serializer.DeserializeJson<CandyMachineModel>(string.Empty, "C:\\Users\\matteo\\Documents\\Progetti\\Siamango\\SolmangoAPI\\SolmangoAPI\\bin\\Debug\\net6.0\\res\\candyMachine.json", out var candy))
    {
        foreach (var item in candy.Items)
        {
            item.Value.RarityOrder = elem.IndexOf(elem.Find(r => r.Id.Equals(item.Key)));
        }

        //foreach (var item in candy.Items) //{ //}
        Serializer.SerializeJson(string.Empty, @"C:\Users\matteo\Desktop\rar\mintsData.json", candy);
    }
}

void ConfigureServices(WebApplicationBuilder builder)
{
    Cluster cluster = Cluster.DevNet;
    try
    {
        cluster = Enum.Parse<Cluster>(builder.Configuration.GetSection("Preferences:SolanaCluster").Get<string>());
    }
    catch (Exception)
    {
        Logger.ConsoleInstance.LogError("Unable to parse RPC client from options file");
        Environment.Exit(1);
    }

    IRpcScheduler scheduler = new BasicRpcScheduler(100);
    scheduler.Start();

    builder.Services.AddCors();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => c.OperationFilter<AddRequiredHeaderParameter>());
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton(ClientFactory.GetClient(cluster));
    builder.Services.AddSingleton(scheduler);
    builder.AddEndpointDefinitionsServices();
    builder.WebHost.ConfigureKestrel(options =>
    {
        int port = builder.Configuration.GetSection("Preferences:Port").Get<int>();
        if (builder.Configuration.GetSection("Security:AllowTLS").Get<bool>())
        {
            options.ListenAnyIP(port, opt => opt.UseHttps(
                builder.Configuration.GetSection("Security:CertificatePath").Get<string>(),
                builder.Configuration.GetSection("Security:CertificatePassword").Get<string>()));
        }
        else
        {
            options.ListenAnyIP(port);
        }
    });
}

void Configure(WebApplication app)
{
    var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    applicationLifetime.ApplicationStopping.Register(() => OnShutdown(app.Services));
    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    app.UseRouting();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
    app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    app.UseEndpointDefinitions();
}

void OnShutdown(IServiceProvider services)
{
    var handler = services.GetService<IReadOnlyCollection<IEndpointShutdownHandler>>();
    if (handler is not null)
    {
        foreach (var service in handler)
        {
            service.OnShutdown(services);
        }
    }
}

#endregion Methods

var builder = WebApplication.CreateBuilder(args);
ConfigureServices(builder);
var app = builder.Build();
Configure(app);

app.Logger.LogInformation("Running in {env}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
await app.RunAsync();