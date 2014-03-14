using System;
using NServiceBus.Saga;

namespace NServiceBus.FoundationDB.Tests
{
    public class TestSaga : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public string StringProperty { get; set; }
        [Unique]
        public string StringUniqueProperty { get; set; }
        [Unique]
        public Guid GuidUniqueProperty { get; set; }
        public int IntProperty { get; set; }
    }
}