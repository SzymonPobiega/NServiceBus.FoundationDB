using System;

namespace NServiceBus.FoundationDB.Persistence.FoundationDB.TimeoutPersister
{
    public interface IClock
    {
        DateTime Now();
    }
}