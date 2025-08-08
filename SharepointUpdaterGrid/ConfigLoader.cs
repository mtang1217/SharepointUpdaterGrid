using System;
using Microsoft.Extensions.Configuration;

public static class ConfigLoader
{
    public static IConfiguration Config { get; }

    static ConfigLoader()
    {
        Config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }
}
