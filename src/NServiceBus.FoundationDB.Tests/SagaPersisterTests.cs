using System;
using System.Threading.Tasks;
using FoundationDB.Client;
using FoundationDB.Layers.Directories;
using NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister;
using NUnit.Framework;
using FluentAssertions;

namespace NServiceBus.AddIn.Tests
{
    [TestFixture]
    public class SagaPersisterTests
    {
        private FDBSagaPersister sagaPersister;
        private IFdbDatabase db;

        [SetUp]
        public void SetUp()
        {
            Fdb.Start();
            var connectionConfiguration = new FDBConnectionConfigurationBuilder().Build();
            db = connectionConfiguration.ConnectToSagaStore();
            using (var tx = db.BeginTransaction())
            {
                tx.ClearRange(db.GlobalSpace);
                tx.CommitAsync().Wait();
            }
            sagaPersister = new FDBSagaPersister(connectionConfiguration, new JsonSerializer());
        }

        [TearDown]
        public void TearDown()
        {
            db.Dispose();
            sagaPersister.Dispose();
        }

        [Test]
        public void Can_store_and_load_saga_instance_with_all_properties_by_id()
        {

            var saga = new TestSaga()
                {
                    Id = Guid.NewGuid(),
                    Originator = "Originator",
                    OriginalMessageId = "OriginalMessageId",
                    StringProperty = "StringProperty",
                    StringUniqueProperty = "StringUniqueProperty",
                    GuidUniqueProperty = Guid.NewGuid(),
                    IntProperty = 42
                };

            sagaPersister.Save(saga);

            var loadedSaga = sagaPersister.Get<TestSaga>(saga.Id);
            loadedSaga.ShouldBeEquivalentTo(saga);
        }

        [Test]
        public void Detects_concurrency_violation_with_id_when_inserting()
        {
            var id = Guid.NewGuid();
            var successSaga = new TestSaga()
            {
                Id = id,
                Originator = "Success"
            };
            var failureSaga = new TestSaga()
            {
                Id = id,
                Originator = "Failure"
            };

            using (var txSuccess = db.BeginTransaction())
            using (var txFailure = db.BeginTransaction())
            {
                sagaPersister.DoSave(txFailure, failureSaga);
                sagaPersister.DoSave(txSuccess, successSaga);

                txSuccess.CommitAsync().Wait();
                Assert.Throws<AggregateException>(() => txFailure.CommitAsync().Wait());
            }

            var loadedSaga = sagaPersister.Get<TestSaga>(successSaga.Id);
            loadedSaga.ShouldBeEquivalentTo(successSaga);
        }

        [Test]
        public void Detects_concurrency_violation_with_unique_properties_when_inserting()
        {
            var successSaga = new TestSaga()
            {
                Id = Guid.NewGuid(),
                Originator = "Success",
                StringUniqueProperty = "Duplicate"
            };
            var failureSaga = new TestSaga()
            {
                Id = Guid.NewGuid(),
                Originator = "Failure",
                StringUniqueProperty = "Duplicate"
            };

            using (var txSuccess = db.BeginTransaction())
            using (var txFailure = db.BeginTransaction())
            {
                sagaPersister.DoSave(txFailure, failureSaga);
                sagaPersister.DoSave(txSuccess, successSaga);

                txSuccess.CommitAsync().Wait();
                Assert.Throws<AggregateException>(() => txFailure.CommitAsync().Wait());
            }

            var loadedSaga = sagaPersister.Get<TestSaga>("StringUniqueProperty", "Duplicate");
            loadedSaga.ShouldBeEquivalentTo(successSaga);
        }

        [Test]
        public void Detects_concurrency_violation_with_id_when_updating()
        {
            var saga = new TestSaga()
            {
                Id = Guid.NewGuid(),
            };

            sagaPersister.Save(saga);

            var failureSaga = sagaPersister.Get<TestSaga>(saga.Id);
            var successSaga = sagaPersister.Get<TestSaga>(saga.Id);

            successSaga.Originator = "Success";
            failureSaga.Originator = "Failure";

            sagaPersister.Save(successSaga);
            Assert.Throws<InvalidOperationException>(() => sagaPersister.Save(failureSaga));

            var loadedSaga = sagaPersister.Get<TestSaga>(saga.Id);
            loadedSaga.Originator.Should().Be("Success");
        }

        [Test]
        public void Can_store_and_load_saga_instance_by_string_unique_field()
        {

            var saga = new TestSaga()
            {
                Id = Guid.NewGuid(),
                StringUniqueProperty = "Unique"
            };

            sagaPersister.Save(saga);

            var loadedSaga = sagaPersister.Get<TestSaga>("StringUniqueProperty", "Unique");
            loadedSaga.ShouldBeEquivalentTo(saga);
        }
        
        [Test]
        public void Updates_indices_when_updating_a_unique_field()
        {

            var saga = new TestSaga()
            {
                Id = Guid.NewGuid(),
                StringUniqueProperty = "Unique"
            };

            sagaPersister.Save(saga);

            saga = sagaPersister.Get<TestSaga>(saga.Id);
            saga.StringUniqueProperty = "NewUnique";

            sagaPersister.Save(saga);

            var loadedSaga = sagaPersister.Get<TestSaga>("StringUniqueProperty", "Unique");
            loadedSaga.Should().BeNull();
            
            loadedSaga = sagaPersister.Get<TestSaga>("StringUniqueProperty", "NewUnique");
            loadedSaga.Should().NotBeNull();
        }

        [Test]
        public void Returns_null_if_property_lookup_failed()
        {

            var loadedSaga = sagaPersister.Get<TestSaga>("StringUniqueProperty", "NonExistingUniqueValue");
            Assert.IsNull(loadedSaga);
        }

        [Test]
        public void Throws_exception_when_trying_to_load_non_existing_saga()
        {

            var nonExistingId = Guid.NewGuid();

            Assert.Throws<InvalidOperationException>(() => sagaPersister.Get<TestSaga>(nonExistingId));
        }
    }
}