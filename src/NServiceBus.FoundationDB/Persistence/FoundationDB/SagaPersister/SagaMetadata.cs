using System.Collections.Generic;

namespace NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister
{
    public class SagaMetadata
    {
        private readonly int version;
        private readonly Dictionary<string, object> uniqueValues;

        public SagaMetadata(int version, Dictionary<string, object> uniqueValues)
        {
            this.version = version;
            this.uniqueValues = uniqueValues;
        }

        public int Version
        {
            get { return version; }
        }

        public Dictionary<string, object> UniqueValues
        {
            get { return uniqueValues; }
        }
    }
}