using System;
using FoundationDB.Client;

namespace NServiceBus.FoundationDB
{
    public interface ISerializer
    {
        object Deserialize(Slice slice, Type type);
        Slice Serialize(object objectToSerialize);
    }
}