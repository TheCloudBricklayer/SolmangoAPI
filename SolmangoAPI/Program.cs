using BetterHaveIt.Repositories;
using HandierCli;
using SolmangoAPI.Endpoint;
using SolmangoAPI.Middleware;
using SolmangoNET.Models;
using SolmangoNET.Rpc;
using Solnet.Rpc;

#region Methods

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
    builder.Services.AddSingleton<IRepository<CollectionModel>>(new RepositoryJson<CollectionModel>(builder.Configuration.GetValue<string>("Preferences:CollectionFilePath")));
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