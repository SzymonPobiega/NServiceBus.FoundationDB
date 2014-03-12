using System.Configuration;
using System.IO;
using NServiceBus.FoundationDB.Config;
using NServiceBus.FoundationDB.Persistence.FoundationDB.TimeoutPersister;

// ReSharper disable CheckNamespace
namespace NServiceBus
// ReSharper restore CheckNamespace
{
    public static class ConfigureFDBTimeoutPersister
    {
        public static Configure FoundationDBTimeoutPersister(this Configure config)
        {
            if (!Configure.HasComponent<IFDBConnectionConfiguration>())
            {
                config.FoundationDB();
            }
            config.Configurer.ConfigureComponent<FDBTimeoutPersister>(DependencyLifecycle.InstancePerCall);
            return config;
        }
    }
}