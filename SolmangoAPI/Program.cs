using HandierCli;
using SolmangoAPI.Endpoint;
using SolmangoAPI.Middleware;
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
    builder.Services.AddSwaggerGen();
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
    app.UseSwaggerUI();
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

await app.RunAsync();