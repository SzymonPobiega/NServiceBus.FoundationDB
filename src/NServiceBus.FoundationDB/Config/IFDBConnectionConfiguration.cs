using FoundationDB.Client;

namespace NServiceBus.FoundationDB.Config
{
    public interface IFDBConnectionConfiguration
    {
        IFdbDatabase ConnectToSagaStore();
        IFdbDatabase ConnectToTimeoutStore();
    }
}