using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SolmangoAPI.Middleware;

public class AddRequiredHeaderParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var allowAnonymous = metadata.Any(c => c.GetType() == typeof(AllowAnonymousAttribute));
        if (!allowAnonymous)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Api-Key",
                In = ParameterLocation.Header,
                Description = "Api key to access backend",
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new OpenApiString("")
                }
            });
        }
    }
}