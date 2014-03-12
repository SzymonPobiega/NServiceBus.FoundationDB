using FoundationDB.Client;

namespace NServiceBus.FoundationDB.Persistence.FoundationDB.SagaPersister
{
    public interface IFDBConnectionConfiguration
    {
        IFdbDatabase ConnectToSagaStore();
        IFdbDatabase ConnectToTimeoutStore();
    }
}