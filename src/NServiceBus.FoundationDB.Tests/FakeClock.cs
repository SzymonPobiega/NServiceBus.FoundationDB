using System;
using NServiceBus.FoundationDB.Persistence.FoundationDB.TimeoutPersister;

namespace NServiceBus.AddIn.Tests
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