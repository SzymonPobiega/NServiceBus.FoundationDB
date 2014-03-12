using System.Configuration;
using System.IO;

namespace NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister
{
    public static class ConfigureFDBSagaPersister
    {
        public static Configure EventStoreSagaPersister(this Configure config)
        {
            if (!Configure.HasComponent<IFDBConnectionConfiguration>())
            {
                config.FoundationDB();
            }
            config.Configurer.ConfigureComponent<FDBSagaPersister>(DependencyLifecycle.InstancePerCall);
            return config;
        }
    }
}