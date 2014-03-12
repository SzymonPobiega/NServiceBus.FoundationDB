using System;
using FoundationDB.Client;
using NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister;
using NServiceBus.FoundationDB.Persistence.FoundationDB.TimeoutPersister;
using NServiceBus.Timeout.Core;
using NUnit.Framework;
using FluentAssertions;

namespace NServiceBus.AddIn.Tests
{
    [TestFixture]
    public class TimeoutPersisterTests
    {
        private FDBTimeoutPersister timeoutPersister;
        private IFdbDatabase db;
        private FakeClock fakeClock;

        [SetUp]
        public void SetUp()
        {
            Fdb.Start();
            var connectionConfiguration = new FDBConnectionConfigurationBuilder().Build();
            db = connectionConfiguration.ConnectToTimeoutStore();
            using (var tx = db.BeginTransaction())
            {
                tx.ClearRange(db.GlobalSpace);
                tx.CommitAsync().Wait();
            }
            fakeClock = new FakeClock();
            timeoutPersister = new FDBTimeoutPersister(connectionConfiguration, new JsonSerializer(), fakeClock);
        }

        [TearDown]
        public void TearDown()
        {
            db.Dispose();
            timeoutPersister.Dispose();
        }

        [Test]
        public void Can_read_timeouts_by_date()
        {
            var baseTime = new DateTime(1984, 04, 09, 10, 0, 0);

            fakeClock.CurrentTime = baseTime.AddMinutes(15).AddMilliseconds(1);

            timeoutPersister.Add(CreateTimeout("a", baseTime.AddMinutes(5)));
            timeoutPersister.Add(CreateTimeout("10", baseTime.AddMinutes(10)));
            timeoutPersister.Add(CreateTimeout("bbb", baseTime.AddMinutes(15)));
            timeoutPersister.Add(CreateTimeout("uuu", baseTime.AddMinutes(15).AddMilliseconds(1)));

            DateTime nextRun;
            var results = timeoutPersister.GetNextChunk(baseTime, out nextRun);

            var expected = new System.Collections.Generic.List<Tuple<string, DateTime>>()
                {
                    Tuple.Create("a", baseTime.AddMinutes(5)),
                    Tuple.Create("10", baseTime.AddMinutes(10)),
                    Tuple.Create("bbb", baseTime.AddMinutes(15)),
                };

            results.ShouldAllBeEquivalentTo(expected);
            nextRun.Should().Be(baseTime.AddMinutes(15));
        }

        [Test]
        public void Handles_two_timeouts_with_same_time()
        {
            var baseTime = new DateTime(1984, 04, 09, 10, 0, 0);

            fakeClock.CurrentTime = baseTime.AddMinutes(15);

            timeoutPersister.Add(CreateTimeout("1", baseTime.AddMinutes(5)));
            timeoutPersister.Add(CreateTimeout("2", baseTime.AddMinutes(5)));

            DateTime nextRun;
            var results = timeoutPersister.GetNextChunk(baseTime, out nextRun);

            var expected = new System.Collections.Generic.List<Tuple<string, DateTime>>()
                {
                    Tuple.Create("1", baseTime.AddMinutes(5)),
                    Tuple.Create("2", baseTime.AddMinutes(5)),
                };

            results.ShouldAllBeEquivalentTo(expected);
        }

        [Test]
        public void Can_find_timeout_by_id()
        {
            var baseTime = new DateTime(1984, 04, 09, 10, 0, 0);

            timeoutPersister.Add(CreateTimeout("1", baseTime));

            TimeoutData existingTimeout;
            var result = timeoutPersister.TryRemove("1", out existingTimeout);

            result.Should().BeTrue();
            existingTimeout.Should().NotBeNull();
        }

        [Test]
        public void Returns_null_when_looking_for_non_existing_timeout_by_id()
        {
            TimeoutData existingTimeout;
            var result = timeoutPersister.TryRemove("1", out existingTimeout);

            result.Should().BeFalse();
            existingTimeout.Should().BeNull();
        }

        [Test]
        public void Can_remove_timeout_by_saga_id()
        {
            var baseTime = new DateTime(1984, 04, 09, 10, 0, 0);
            fakeClock.CurrentTime = baseTime.AddMinutes(5);
            var sagaId = Guid.NewGuid();
            timeoutPersister.Add(CreateTimeout("1", baseTime, sagaId));

            timeoutPersister.RemoveTimeoutBy(sagaId);

            //Ensure not reachable by ID
            TimeoutData existingTimeout;
            var result = timeoutPersister.TryRemove("1", out existingTimeout);
            result.Should().BeFalse();
            existingTimeout.Should().BeNull();

            //Ensure not reachable by time
            //Ensure not reachable by time
            DateTime nextRun;
            var results = timeoutPersister.GetNextChunk(baseTime, out nextRun);
            results.Count.Should().Be(0);
        }

        [Test]
        public void Can_remove_timeout_by_id()
        {
            var baseTime = new DateTime(1984, 04, 09, 10, 0, 0);
            fakeClock.CurrentTime = baseTime.AddMinutes(5);
            var sagaId = Guid.NewGuid();
            timeoutPersister.Add(CreateTimeout("1", baseTime, sagaId));

            TimeoutData existingTimeout;
            timeoutPersister.TryRemove("1", out existingTimeout);

            //Ensure not reachable by ID
            var result = timeoutPersister.TryRemove("1", out existingTimeout);
            result.Should().BeFalse();
            existingTimeout.Should().BeNull();

            //Ensure not reachable by time
            DateTime nextRun;
            var results = timeoutPersister.GetNextChunk(baseTime, out nextRun);
            results.Count.Should().Be(0);
        }

        private static TimeoutData CreateTimeout(string id, DateTime time, Guid? sagaId = null)
        {
            return new TimeoutData()
                {
                    Id = id,
                    SagaId = sagaId ?? Guid.NewGuid(),
                    Time = time
                };
        }
    }
}