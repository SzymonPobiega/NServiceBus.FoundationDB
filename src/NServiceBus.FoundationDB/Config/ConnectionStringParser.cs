using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister
{
    public class ConnectionStringParser
    {
        private static readonly List<PropertyParser> propertyParsers
            = new List<PropertyParser>()
                {
                    new PropertyParser((p, b) => b.WithClusterFilePath(p["clusterFilePath"]), "clusterFilePath"),
                    new PropertyParser((p, b) => b.WithSagaStoreDirectory(p["sagaStore"]), "sagaStore"),
                    new PropertyParser((p, b) => b.WithTimeoutStoreDirectory(p["timeoutStore"]), "timeoutStore"),
                };

        public FDBConnectionConfiguration Parse(string connectionString)
        {
            var builder = new FDBConnectionConfigurationBuilder();
            Parse(connectionString, builder);
            return builder.Build();
        }

        public void Parse(string connectionString, FDBConnectionConfigurationBuilder builder)
        {
            var settingsDictionary =
                connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Trim(new[] { ' ' }))
                                .Select(x => x.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                                .Where(x => x.Length == 1 || x.Length == 2)
                                .ToDictionary(x => x[0], x => x.Length == 1 ? null : x[1]);

            foreach (var parser in propertyParsers)
            {
                parser.Parse(settingsDictionary, builder);
            }
        }

        public class PropertyParser
        {
            private readonly string[] requiredParameters;
            private readonly Action<Dictionary<string, string>, FDBConnectionConfigurationBuilder> configAction;

            public PropertyParser(Action<Dictionary<string, string>, FDBConnectionConfigurationBuilder> configAction, params string[] requiredParameters)
            {
                this.requiredParameters = requiredParameters;
                this.configAction = configAction;
            }

            public void Parse(Dictionary<string, string> parameters, FDBConnectionConfigurationBuilder builder)
            {
                if (requiredParameters.All(parameters.ContainsKey))
                {
                    configAction(parameters, builder);
                }
            }
        }
    }
}