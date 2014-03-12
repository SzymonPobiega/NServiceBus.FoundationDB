using System;

namespace NServiceBus.FoundationDB.Config
{
    public class FDBConnectionConfigurationBuilder
    {
        private string clusterFilePath;
        private string sagaStoreDirectory = "Sagas";
        private string timeoutStoreDirectory = "Timeouts";

        public FDBConnectionConfigurationBuilder WithClusterFilePath(string aClusterFilePath)
        {
            if (aClusterFilePath == null)
            {
                throw new ArgumentNullException("aClusterFilePath");
            }
            if (aClusterFilePath == string.Empty)
            {
                throw new ArgumentException("Cluster path cannot be empty string","aClusterFilePath");
            }
            clusterFilePath = aClusterFilePath;
            return this;
        }

        public FDBConnectionConfigurationBuilder WithSagaStoreDirectory(string aSagaStoreDirectory)
        {
            if (aSagaStoreDirectory == null)
            {
                throw new ArgumentNullException("aSagaStoreDirectory");
            }
            if (aSagaStoreDirectory == string.Empty)
            {
                throw new ArgumentException("Saga store directory cannot be empty string", "aSagaStoreDirectory");
            }
            sagaStoreDirectory = aSagaStoreDirectory;
            return this;
        }

        public FDBConnectionConfigurationBuilder WithTimeoutStoreDirectory(string aTimeoutStoreDirectory)
        {
            if (aTimeoutStoreDirectory == null)
            {
                throw new ArgumentNullException("aTimeoutStoreDirectory");
            }
            if (aTimeoutStoreDirectory == string.Empty)
            {
                throw new ArgumentException("Timeout store directory cannot be empty string", "aTimeoutStoreDirectory");
            }
            timeoutStoreDirectory = aTimeoutStoreDirectory;
            return this;
        }

        public FDBConnectionConfiguration Build()
        {
            return new FDBConnectionConfiguration(clusterFilePath, sagaStoreDirectory, timeoutStoreDirectory);
        }
    }
}