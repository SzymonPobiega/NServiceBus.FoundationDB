using System.Configuration;
using System.IO;

namespace NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister
{
    public static class FDBConfigurator
    {
        public static Configure FoundationDB(this Configure config)
        {
            if (Configure.HasComponent<IFDBConnectionConfiguration>())
            {
                return config;
            }
            var connectionString = "";
            var connectionStringEntry = ConfigurationManager.ConnectionStrings["NServiceBus.Persistence"] ??
                                        ConfigurationManager.ConnectionStrings["NServiceBus/Persistence"];
            if (connectionStringEntry != null)
            {
                connectionString = connectionStringEntry.ConnectionString;
            }
            var parser = new ConnectionStringParser();
            var connectionConfiguration = parser.Parse(connectionString);
            config.Configurer.RegisterSingleton<IFDBConnectionConfiguration>(connectionConfiguration);
            config.Configurer.ConfigureComponent<JsonSerializer>(DependencyLifecycle.InstancePerCall);
            return config;
        }
    }
}