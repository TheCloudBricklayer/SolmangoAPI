using System.Reflection;

namespace SolmangoAPI.Endpoint;

public static class EndpointDefinitionExtensions
{
    public static void AddEndpointDefinitionsServices(this WebApplicationBuilder builder)
    {
        var endpointDefinitions = new List<IEndpointDefinition>();
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        endpointDefinitions.AddRange(currentAssembly.ExportedTypes
            .Where(x => typeof(IEndpointDefinition).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
            .Select(Activator.CreateInstance).Cast<IEndpointDefinition>());
        var shutdownHandlers = new List<IEndpointShutdownHandler>();
        for (int i = 0; i < endpointDefinitions.Count; i++)
        {
            if (endpointDefinitions[i] is IEndpointShutdownHandler shutdown)
            {
                shutdownHandlers.Add(shutdown);
            }
            endpointDefinitions[i].DefineServices(builder.Services, builder.Configuration);
        }
        builder.Services.AddSingleton(endpointDefinitions as IReadOnlyCollection<IEndpointDefinition>);
        builder.Services.AddSingleton(shutdownHandlers as IReadOnlyCollection<IEndpointShutdownHandler>);
    }

    public static void UseEndpointDefinitions(this WebApplication app)
    {
        var definitions = app.Services.GetRequiredService<IReadOnlyCollection<IEndpointDefinition>>();
        foreach (var endpointDefinition in definitions)
        {
            endpointDefinition.DefineEndpoints(app);
        }
    }
}