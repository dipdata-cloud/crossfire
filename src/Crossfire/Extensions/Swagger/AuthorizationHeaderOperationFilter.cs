// Copyright (c) George Zubrienko (george.zubrienko@dipdata.cloud), Vitaliy Savitskiy (savitskiy.vitaliy.m@gmail.com). All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

[ExcludeFromCodeCoverage]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Do not document Swagger extensions.")]
public class AuthorizationHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters.Add(new OpenApiParameter
        {
            Required = true,
            AllowEmptyValue = false,
            In = ParameterLocation.Header,
            Description = "Use JWT provided by DipData Platform as Bearer auth token",
            Name = "Authorization",
            Schema = new OpenApiSchema { Type = "apiKey" },
        });
    }
}
