using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FoundationDB.Client;
using NServiceBus.Saga;
using Newtonsoft.Json;

namespace NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister
{
    public class FDBSagaPersister : IPersistSagas, IDisposable
    {
        private readonly ISerializer serializer;
        private static readonly ConditionalWeakTable<IContainSagaData, SagaMetadata> versionInformation = new ConditionalWeakTable<IContainSagaData, SagaMetadata>();
        private readonly IFdbDatabase db;

        public FDBSagaPersister(IFDBConnectionConfiguration connectionConfiguration, ISerializer serializer)
        {
            this.serializer = serializer;
            db = connectionConfiguration.ConnectToSagaStore();
        }

        public void Save(IContainSagaData saga)
        {
            using (var tx = db.BeginTransaction())
            {
                DoSave(tx, saga);
                tx.CommitAsync().Wait();
            }
        }

        public void DoSave(IFdbTransaction tx, IContainSagaData saga)
        {
            var sagaType = saga.GetType();
            var idKey = GetSagaKey(sagaType, saga.Id);
            var versionKey = GetSagaVersionKey(idKey);

            tx.GetAsync(idKey).Wait();

            tx.Set(idKey, serializer.Serialize(saga));

            SagaMetadata existingMetadata;

            int versionToStore;
            if (versionInformation.TryGetValue(saga, out existingMetadata))
            {
                var currentVersion = tx.GetAsync(versionKey).Result.ToInt32();
                if (currentVersion != existingMetadata.Version)
                {
                    throw new InvalidOperationException(string.Format("Concurrency violation. Expected version {0} but found {1}", existingMetadata, currentVersion));
                }
                versionToStore = existingMetadata.Version + 1;
            }
            else
            {
                versionToStore = 0;
            }

            tx.Set(versionKey, Slice.FromInt32(versionToStore));

            foreach (var uniqueProperty in GetUniqueProperties(sagaType))
            {
                var currentValue = uniqueProperty.GetValue(saga);
                var newUniqueKey = GetUniqueKey(sagaType, uniqueProperty.Name, currentValue);
                tx.GetAsync(newUniqueKey).Wait();
                tx.Set(newUniqueKey, Slice.FromGuid(saga.Id));
                if (existingMetadata != null && existingMetadata.UniqueValues[uniqueProperty.Name] != currentValue)
                {
                    var oldUniqueKey = GetUniqueKey(sagaType, uniqueProperty.Name, existingMetadata.UniqueValues[uniqueProperty.Name]);
                    tx.Clear(oldUniqueKey);
                }
            }
        }

        
        private static IEnumerable<PropertyInfo> GetUniqueProperties(Type sagaType)
        {
            return sagaType.GetProperties().Where(x => x.GetCustomAttributes(false).OfType<UniqueAttribute>().Any());
        }

        private static Slice GetSagaVersionKey(Slice idKey)
        {
            return Slice.Concat(idKey, Slice.FromString("version"));
        }

        private Slice GetSagaKey(Type sagaType, Guid sagaId)
        {
            return db.Pack(sagaType.FullName, sagaId);
        }

        private Slice GetUniqueKey(Type sagaType, string uniqueProperty, object currentValue)
        {
            return db.Pack(sagaType.FullName + "By" + uniqueProperty, currentValue);
        }

        public void Update(IContainSagaData saga)
        {
            using (var tx = db.BeginTransaction())
            {
                DoUpdate(tx, saga);
                tx.CommitAsync().Wait();
            }
        }

        private void DoUpdate(IFdbTransaction tx, IContainSagaData saga)
        {
            tx.Set(GetSagaKey(saga.GetType(),saga.Id), serializer.Serialize(saga));
        }

        public T Get<T>(Guid sagaId) where T : IContainSagaData
        {
            using (var tx = db.BeginReadOnlyTransaction())
            {
                return DoGet<T>(sagaId, tx);
            }
        }

        public T DoGet<T>(Guid sagaId, IFdbReadOnlyTransaction tx) where T : IContainSagaData
        {
            var sagaType = typeof (T);
            var idKey = GetSagaKey(sagaType, sagaId);
            var range = tx.GetRange(FdbKeyRange.StartsWith(idKey));
            var items = range.ToListAsync().Result;
            if (items.Count != 2)
            {
                throw new InvalidOperationException("Saga with id " + sagaId + " does not exist.");
            }
            var jsonSlice = items[0].Value;
            var version = items[1].Value.ToInt32();
            var result = (T)serializer.Deserialize(jsonSlice, sagaType);
            var uniqueValues = ExtractUniqueValues(sagaType, result);
            var metadata = new SagaMetadata(version, uniqueValues);
            versionInformation.Add(result, metadata);
            return result;
        }

        private static Dictionary<string, object> ExtractUniqueValues<T>(Type sagaType, T result) where T : IContainSagaData
        {
            var uniqueValues = new Dictionary<string, object>();
            foreach (var uniqueProperty in GetUniqueProperties(sagaType))
            {
                var uniqueValue = uniqueProperty.GetValue(result);
                uniqueValues[uniqueProperty.Name] = uniqueValue;
            }
            return uniqueValues;
        }

        public T Get<T>(string property, object value) where T : IContainSagaData
        {
            using (var tx = db.BeginReadOnlyTransaction())
            {
                return DoGet<T>(property, value, tx);
            }
        }

        public T DoGet<T>(string property, object value, IFdbReadOnlyTransaction tx) where T : IContainSagaData
        {
            var sagaIdSlice = tx.GetAsync(GetUniqueKey(typeof (T), property, value)).Result;
            var sagaId = sagaIdSlice.ToGuid();
            if (sagaId == Guid.Empty)
            {
                return default(T);
            }
            return DoGet<T>(sagaId, tx);
        }

        public void Complete(IContainSagaData saga)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            //Injected
        }
    }
}