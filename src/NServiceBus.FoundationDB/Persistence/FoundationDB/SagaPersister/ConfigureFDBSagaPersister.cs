﻿using System.Configuration;
using System.IO;
using NServiceBus.FoundationDB.Config;
using NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister;

// ReSharper disable CheckNamespace
namespace NServiceBus
// ReSharper restore CheckNamespace
{
    public static class ConfigureFDBSagaPersister
    {
        public static Configure FoundationDBSagaPersister(this Configure config)
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