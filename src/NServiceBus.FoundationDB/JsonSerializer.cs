using System;
using FoundationDB.Client;
using Newtonsoft.Json;

namespace NServiceBus.FoundationDB
{
    public class JsonSerializer : ISerializer
    {
        public object Deserialize(Slice slice, Type type)
        {
            return JsonConvert.DeserializeObject(slice.ToUnicode(), type);
        }

        public Slice Serialize(object objectToSerialize)
        {
            return Slice.FromString(JsonConvert.SerializeObject(objectToSerialize));
        }
    }
}