using System.Runtime.InteropServices;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;

namespace Claims;

/// <summary>Starts SQL Server, MongoDB and the Service Bus emulator in containers, for local development and tests.</summary>
internal static class LocalInfrastructure
{
    /// <returns>The configuration values that point the application at the containers.</returns>
    public static async Task<Dictionary<string, string?>> StartAsync()
    {
        var sqlContainer = new MsSqlBuilder(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? "mcr.microsoft.com/mssql/server:2022-latest"
                : "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04")
            .Build();

        // A replica set, because MongoDB transactions require one and the outbox relies on them.
        var mongoContainer = new MongoDbBuilder("mongo:latest")
            .WithReplicaSet()
            .Build();

        var serviceBusContainer = new ServiceBusBuilder("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithAcceptLicenseAgreement(true)
            .WithConfig(Path.Combine(AppContext.BaseDirectory, "servicebus-emulator.json"))
            .Build();

        await Task.WhenAll(sqlContainer.StartAsync(), mongoContainer.StartAsync(), serviceBusContainer.StartAsync());

        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:AuditDatabase"] = sqlContainer.GetConnectionString(),
            ["ConnectionStrings:ClaimsDatabase"] = mongoContainer.GetConnectionString(),
            ["ConnectionStrings:ServiceBus"] = serviceBusContainer.GetConnectionString()
        };
    }
}
