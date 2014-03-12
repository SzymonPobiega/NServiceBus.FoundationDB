using System;
using System.Collections.Generic;
using System.Linq;
using FoundationDB.Client;
using FoundationDB.Layers.Tuples;
using NServiceBus.FoundationDB.Config;
using NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister;
using NServiceBus.Timeout.Core;

namespace NServiceBus.FoundationDB.Persistence.FoundationDB.TimeoutPersister
{
    public class FDBTimeoutPersister : IPersistTimeouts, IDisposable
    {
        private readonly ISerializer serializer;
        private readonly IClock clock;
        private readonly IFdbDatabase db;  

        public FDBTimeoutPersister(IFDBConnectionConfiguration connectionConfiguration, ISerializer serializer, IClock clock)
        {
            this.serializer = serializer;
            this.clock = clock;
            db = connectionConfiguration.ConnectToTimeoutStore();
        }

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var now = clock.Now();
            using (var tx = db.BeginReadOnlyTransaction())
            {
                var range = tx.Snapshot.GetRange(GetTimeIndexKey(startSlice, ""), GetTimeIndexKey(now, "")).ToListAsync().Result;
                var results = range.Select(x => Tuple.Create(x.Value.ToString(), new DateTime((long) db.Unpack(x.Key)[1]))).ToList();
                nextTimeToRunQuery = results.Any() ? results.Last().Item2 : startSlice;
                return results;
            }
        }

        public void Add(TimeoutData timeout)
        {
            using (var tx = db.BeginTransaction())
            {
                var timeoutKey = GetTimeoutKey(timeout);

                var existingTimeout = tx.GetAsync(timeoutKey).Result;
                if (existingTimeout.HasValue)
                {
                    throw new InvalidOperationException("Timeout with this ID already exists.");
                }
                tx.Set(timeoutKey, serializer.Serialize(timeout));
                tx.Set(db.Pack(timeout.Id, "Time"), Slice.FromInt64(timeout.Time.Ticks));
                tx.Set(db.Pack(timeout.SagaId, timeout.Id), Slice.Empty);
                var byTimeKey = GetTimeIndexKey(timeout.Time, timeout.Id);
                tx.Set(byTimeKey, Slice.FromString(timeout.Id));
                tx.CommitAsync().Wait();
            }
        }

        private Slice GetTimeoutKey(TimeoutData timeout)
        {
            return db.Pack(timeout.Id);
        }

        private Slice GetTimeIndexKey(DateTime time, string timeoutId)
        {
            return db.GlobalSpace.Pack("ByTime", time, timeoutId);
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            using (var tx = db.BeginTransaction())
            {
                var timeoutKey = db.Pack(timeoutId);
                var timeoutSlice = tx.GetAsync(timeoutKey).Result;
                if (!timeoutSlice.HasValue)
                {
                    timeoutData = null;
                    return false;
                }
                timeoutData = (TimeoutData)serializer.Deserialize(timeoutSlice, typeof(TimeoutData));
                tx.Clear(timeoutKey);
                RemoveEntryInTimeIndex(tx, timeoutId);
                tx.CommitAsync().Wait();
                return true;
            }
            
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            using (var tx = db.BeginTransaction())
            {
                var range = FdbKeyRange.PrefixedBy(db.Pack(sagaId));
                var ids = tx.GetRange(range).ToListAsync().Result;
                foreach (var timeoutAndSagaIdPair in ids)
                {
                    var timeoutId = db.Unpack(timeoutAndSagaIdPair.Key).Last<string>();
                    tx.Clear(db.Pack(timeoutId));
                    RemoveEntryInTimeIndex(tx, timeoutId);
                }
                tx.ClearRange(range);
                tx.CommitAsync().Wait();
            }
        }

        private void RemoveEntryInTimeIndex(IFdbTransaction tx, string timeoutId)
        {
            var time = tx.GetAsync(db.Pack(timeoutId, "Time")).Result.ToInt64();
            var byTimeKey = GetTimeIndexKey(new DateTime(time), timeoutId);
            tx.Clear(byTimeKey);
        }

        public void Dispose()
        {
            //Injected
        }
    }
}