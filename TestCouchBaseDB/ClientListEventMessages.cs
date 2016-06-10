using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.State;

namespace EY.SSA.CommonBusinessLogic.Messages.Events
{
    class ClientListItemDeletedEvent : CommandEventMessage
    {
        public ClientListItemDeletedEvent(ClientListItem clientListItem, string user, string connectionId)
            : base(clientListItem.Id, clientListItem.Name, true, ClientListActor.ActorType, MicroServices.CommandType.Delete, MicroServices.Area.Client, "Client deleted from list.", clientListItem, user, connectionId)
        {
        }
        public ClientListItem ResultClientIListItem { get { return (ClientListItem)Data; } }
    }

    class ClientListItemUnDeletedEvent : CommandEventMessage
    {
        public ClientListItemUnDeletedEvent(ClientListItem clientListItem, string user, string connectionId)
            : base(clientListItem.Id, clientListItem.Name, true, ClientListActor.ActorType, MicroServices.CommandType.Undelete, MicroServices.Area.Client, "Client deleted from list.", clientListItem, user, connectionId)
        {
        }
        public ClientListItem ResultClientIListItem { get { return (ClientListItem)Data; } }
    }

    class ClientListUpdatedEvent :CommandEventMessage
    {
        public ClientListUpdatedEvent(ClientListItem clientListItem, string user, string connectionId)
            : base(clientListItem.Id, clientListItem.Name, true, ClientListActor.ActorType,MicroServices.CommandType.Update, MicroServices.Area.Client, "Client list updated.", clientListItem, user, connectionId)
        {
        }
        public ClientListItem ResultClientIListItem { get { return (ClientListItem)Data; } }

    }
    class ClientListInsertedEvent:CommandEventMessage
    {
        public ClientListInsertedEvent(ClientListItem clientListItem, string user, string connectionId)
            : base(clientListItem.Id, clientListItem.Name, true, ClientListActor.ActorType,MicroServices.CommandType.Insert, MicroServices.Area.Client, "Client added to list.", clientListItem, user, connectionId)
        {
        }
        public ClientListItem ResultClientIListItem { get { return (ClientListItem)Data; } }
    }

    class ClientListInsertFailedEvent : CommandEventMessage
    {
        public ClientListInsertFailedEvent(string reason, ClientListItem originalData, string user, string connectionId)
            : base(originalData.Id, originalData.Name, true, ClientListActor.ActorType,MicroServices.CommandType.Insert, MicroServices.Area.Client, reason, originalData, user, connectionId)
        {

        }
        public ClientListItem OriginalData { get { return (ClientListItem) base.Data;} }
    }
    class ClientListUpdateFailedEvent : CommandEventMessage
    {
        public ClientListUpdateFailedEvent(string reason, ClientListItem originalData, string user, string connectionId)
            : base(originalData.Id, originalData.Name, true, ClientListActor.ActorType,MicroServices.CommandType.Update, MicroServices.Area.Client, reason, originalData, user, connectionId)
        {

        }
        public ClientListItem OriginalData { get { return (ClientListItem)base.Data; } }
    }
    class ClientListDeleteFailedEvent : CommandEventMessage
    {
        public ClientListDeleteFailedEvent(string reason, ClientListItem originalData, string user, string connectionId)
            : base(originalData.Id, originalData.Name, true, ClientListActor.ActorType,MicroServices.CommandType.Delete, MicroServices.Area.Client, reason, originalData, user, connectionId)
        {

        }
        public ClientListItem OriginalData { get { return (ClientListItem)base.Data; } }
    }
}
