using System;
using NServiceBus.FoundationDB.Persistence.FoundationDB.TimeoutPersister;

namespace NServiceBus.FoundationDB.Tests
{
    public class FakeClock : IClock
    {
        public DateTime CurrentTime { get; set; }

        public DateTime Now()
        {
            return CurrentTime;
        }
    }
}