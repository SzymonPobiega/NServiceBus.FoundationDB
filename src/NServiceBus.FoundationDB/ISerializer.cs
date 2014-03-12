using System;
using FoundationDB.Client;

namespace NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister
{
    public interface ISerializer
    {
        object Deserialize(Slice slice, Type type);
        Slice Serialize(object objectToSerialize);
    }
}