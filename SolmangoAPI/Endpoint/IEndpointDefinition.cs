namespace SolmangoAPI.Endpoint;

public interface IEndpointDefinition
{
    void DefineServices(IServiceCollection services, IConfiguration configuration);

    void DefineEndpoints(WebApplication app);
}