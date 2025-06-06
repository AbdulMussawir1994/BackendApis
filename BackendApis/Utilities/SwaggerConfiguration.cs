﻿using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace BackendApis.Utilities;


public static class SwaggerConfiguration
{
    public static WebApplication ConfigureSwagger(this WebApplication app)
    {
        bool swaggerAllowed = Convert.ToBoolean(app.Configuration["EncryptionSettings:IsSwaggerAllowed"]);
        if (swaggerAllowed)
        {
            app.UseSwagger();

            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            app.UseSwaggerUI(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"BackEnd-Api's{description.GroupName.ToUpperInvariant()}"
                    );
                }
            });
        }

        return app;
    }
}