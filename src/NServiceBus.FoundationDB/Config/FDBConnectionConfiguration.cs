using FoundationDB.Client;

namespace NServiceBus.FoundationDB.Config
{
    public class FDBConnectionConfiguration : IFDBConnectionConfiguration
    {
        private readonly string clusterFilePath;
        private readonly string sagaStoreDirectory;
        private readonly string timeoutStoreDirectory;

        public FDBConnectionConfiguration(string clusterFilePath, string sagaStoreDirectory, string timeoutStoreDirectory)
        {
            this.clusterFilePath = clusterFilePath;
            this.sagaStoreDirectory = sagaStoreDirectory;
            this.timeoutStoreDirectory = timeoutStoreDirectory;
        }

        public IFdbDatabase ConnectToSagaStore()
        {
            return Connect(new FdbSubspace(Slice.FromString(sagaStoreDirectory)));
        }

        public IFdbDatabase ConnectToTimeoutStore()
        {
            return Connect(new FdbSubspace(Slice.FromString(timeoutStoreDirectory)));
        }

        private IFdbDatabase Connect(FdbSubspace globalSubspace)
        {
            return clusterFilePath != null
                       ? Fdb.OpenAsync(clusterFilePath, "DB", globalSubspace).Result
                       : Fdb.OpenAsync(globalSubspace).Result;
        }
    }
}