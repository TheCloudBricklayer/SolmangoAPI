namespace SolmangoAPI.Endpoint;

public interface IEndpointShutdownHandler
{
    void OnShutdown(IServiceProvider services);
}