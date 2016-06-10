using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.State;

namespace EY.SSA.CommonBusinessLogic.Messages.Events
{
    class UserListItemDeletedEvent : CommandEventMessage
    {
        public UserListItemDeletedEvent(UserListItem UserListItem, string user, string connectionId)
            : base(UserListItem.Id, UserListItem.UserName, true, UserListActor.ActorType, MicroServices.CommandType.Delete, MicroServices.Area.User, "User deleted from list.", UserListItem, user, connectionId)
        {
        }
        public UserListItem ResultUserIListItem { get { return (UserListItem)Data; } }
    }

    class UserListItemUnDeletedEvent : CommandEventMessage
    {
        public UserListItemUnDeletedEvent(UserListItem UserListItem, string user, string connectionId)
            : base(UserListItem.Id, UserListItem.UserName, true, UserListActor.ActorType, MicroServices.CommandType.Undelete, MicroServices.Area.User, "User deleted from list.", UserListItem, user, connectionId)
        {
        }
        public UserListItem ResultUserIListItem { get { return (UserListItem)Data; } }
    }

    class UserListUpdatedEvent :CommandEventMessage
    {
        public UserListUpdatedEvent(UserListItem UserListItem, string user, string connectionId)
            : base(UserListItem.Id, UserListItem.UserName, true, UserListActor.ActorType,MicroServices.CommandType.Update, MicroServices.Area.User, "User list updated.", UserListItem, user, connectionId)
        {
        }
        public UserListItem ResultUserIListItem { get { return (UserListItem)Data; } }

    }
    class UserListInsertedEvent:CommandEventMessage
    {
        public UserListInsertedEvent(UserListItem UserListItem, string user, string connectionId)
            : base(UserListItem.Id, UserListItem.UserName, true, UserListActor.ActorType,MicroServices.CommandType.Insert, MicroServices.Area.User, "User added to list.", UserListItem, user, connectionId)
        {
        }
        public UserListItem ResultUserIListItem { get { return (UserListItem)Data; } }
    }

    class UserListInsertFailedEvent : CommandEventMessage
    {
        public UserListInsertFailedEvent(string reason, UserListItem originalData, string user, string connectionId)
            : base(originalData.Id, originalData.UserName, true, UserListActor.ActorType,MicroServices.CommandType.Insert, MicroServices.Area.User, reason, originalData, user, connectionId)
        {

        }
        public UserListItem OriginalData { get { return (UserListItem) base.Data;} }
    }
    class UserListUpdateFailedEvent : CommandEventMessage
    {
        public UserListUpdateFailedEvent(string reason, UserListItem originalData, string user, string connectionId)
            : base(originalData.Id, originalData.UserName, true, UserListActor.ActorType,MicroServices.CommandType.Update, MicroServices.Area.User, reason, originalData, user, connectionId)
        {

        }
        public UserListItem OriginalData { get { return (UserListItem)base.Data; } }
    }
    class UserListDeleteFailedEvent : CommandEventMessage
    {
        public UserListDeleteFailedEvent(string reason, UserListItem originalData, string user, string connectionId)
            : base(originalData.Id, originalData.UserName, true, UserListActor.ActorType,MicroServices.CommandType.Delete, MicroServices.Area.User, reason, originalData, user, connectionId)
        {

        }
        public UserListItem OriginalData { get { return (UserListItem)base.Data; } }
    }
}
