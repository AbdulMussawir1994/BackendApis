﻿using Serilog;
using Serilog.Formatting.Json;

namespace BackendApis.Utilities;

public static class AppExtensionLog
{
    public static void SerilogConfiguration(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, loggerConfig) =>
        {
            loggerConfig.WriteTo.Console();
            loggerConfig.WriteTo.File(new JsonFormatter(), "Logs/applogs.txt", rollingInterval: RollingInterval.Day);
        });
    }

}