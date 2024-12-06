using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace WLSUser.Utils
{
    public class AddRequiredHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.GroupName == "v4")
            {
                if (operation.Parameters == null)
                {
                    operation.Parameters = new List<OpenApiParameter>();
                }

                operation.Parameters.Add(new HeaderParameter()
                {
                    Name = "X-User-Id",
                    In = ParameterLocation.Header,
                    Required = false,
                    Schema = new OpenApiSchema()
                    {
                        Type = "string"
                    }
                });

                operation.Parameters.Add(new HeaderParameter()
                {
                    Name = "X-Authz-Data",
                    In = ParameterLocation.Header,
                    Required = false,
                    Schema = new OpenApiSchema()
                    {
                        Type = "integer"
                    }
                });
            }
        }

        class HeaderParameter : OpenApiParameter
        {

        }
    }
}
