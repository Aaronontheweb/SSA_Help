using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.State;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.Messages.Events
{
    class ClientDeletedEvent : DeleteCommandEventMessage
    {
        public ClientDeletedEvent() { }

        public ClientDeletedEvent(ClientState cs, string user, string connectionId)
            : base(cs.Id, cs.Name, ClientActor.ActorType, MicroServices.Area.Client, "Client Deleted", cs, user, connectionId)
        {
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public ClientState ResultClientState { get { return (ClientState)Data; } }
    }
    class ClientUnDeletedEvent : UnDeleteCommandEventMessage
    {
        public ClientUnDeletedEvent() { }

        public ClientUnDeletedEvent(ClientState cs, string user, string connectionId)
            :base(cs.Id, cs.Name, ClientActor.ActorType, MicroServices.Area.Client, "Client Activated", cs, user, connectionId)
        {
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public ClientState ResultClientState { get { return (ClientState)Data; } }
    }
    class ClientUpdatedEvent : UpdateCommandEventMessage
    {
        public ClientUpdatedEvent(){}

        public ClientUpdatedEvent(ClientState cs, string user, string connectionId)
            : base(cs.Id, cs.Name, ClientActor.ActorType,MicroServices.Area.Client, "Client Updated", cs, user, connectionId)
        {
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public ClientState ResultClientState { get { return (ClientState)Data; } }

    }
    class ClientInsertedEvent : InsertCommandEventMessage
    {
        public ClientInsertedEvent() { }
        public ClientInsertedEvent(ClientState cs, string user, string connectionId)
            : base(cs.Id, cs.Name, ClientActor.ActorType, MicroServices.Area.Client, "Client Inserted", cs, user, connectionId)
        {
        }

        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public ClientState ResultClientState { get { return (ClientState)Data; } }

    }

    class ClientFailedInsertEvent : FailedInsertCommandEventMessage
    {
        public ClientFailedInsertEvent() { }

        public ClientFailedInsertEvent(string reason, ClientState originalData, string user, string connectionId)
            : base(originalData.Id, originalData.Name, ClientActor.ActorType, MicroServices.Area.Client, "Client Insert Failed", originalData, user, connectionId)
        {

        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public ClientState OriginalClientState { get { return (ClientState)Data; } }

    }
    class ClientUpsertedEvent : UpsertCommandEventMessage
    {
        public ClientUpsertedEvent() { }
        public ClientUpsertedEvent(ClientState cs, string user, string connectionId)
            : base(cs.Id, cs.Name, ClientActor.ActorType, MicroServices.Area.Client, "Client Upserted", cs, user, connectionId)
        {
        }

        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public ClientState ResultClientState { get { return (ClientState)Data; } }

    }

    class ClientFailedUpsertEvent : FailedUpsertCommandEventMessage
    {
        public ClientFailedUpsertEvent() { }

        public ClientFailedUpsertEvent(string reason, ClientState originalData, string user, string connectionId)
            : base(originalData.Id, originalData.Name, ClientActor.ActorType, MicroServices.Area.Client, "Client Insert Failed", originalData, user, connectionId)
        {

        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public ClientState OriginalClientState { get { return (ClientState)Data; } }

    }
    class ClientFailedUpdateEvent : FailedUpdateCommandEventMessage
    {
        public ClientFailedUpdateEvent(){}

        public ClientFailedUpdateEvent(string reason, ClientState originalData, string user, string connectionId)
            : base(originalData.Id, originalData.Name, ClientActor.ActorType,MicroServices.Area.Client, "Client Update Failed", originalData, user, connectionId)
        {

        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public ClientState OriginalClientState { get { return (ClientState)Data; }  }
    }
    class ClientFailedDeleteEvent : FailedDeleteCommandEventMessage
    {
        public ClientFailedDeleteEvent() { }

        public ClientFailedDeleteEvent(string reason, ClientState originalData, string user, string connectionId)
            : base(originalData.Id, originalData.Name, ClientActor.ActorType, MicroServices.Area.Client, reason, originalData, user, connectionId)
        {

        }
        public ClientFailedDeleteEvent(string reason, string id, string user, string connectionId)
            : base(id, "", ClientActor.ActorType, MicroServices.Area.Client, reason, null, user, connectionId)
        {

        }
    }
    class ClientFailedUnDeleteEvent : FailedUnDeleteCommandEventMessage
    {
        public ClientFailedUnDeleteEvent() { }

        public ClientFailedUnDeleteEvent(string reason, string id, string user, string connectionId)
            : base(id, null, ClientActor.ActorType, MicroServices.Area.Client, reason, null, user, connectionId)
        {

        }
    }
}
