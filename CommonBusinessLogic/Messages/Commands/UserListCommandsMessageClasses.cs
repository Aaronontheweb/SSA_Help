using EY.SSA.CommonBusinessLogic.State;
using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.General;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.Messages.Commands
{
    public abstract class UserListCommand : Command
    {
        public UserListCommand(string id, MicroServices.CommandType commandType, string field, object data, string user, string connectionId) : base(id, commandType, field, data, MicroServices.Area.User, user, connectionId)
        {
        }
    }

    class UserListInsertCommand :UserListCommand
    {
        public UserListInsertCommand(UserListItem data, string user, string connectionId)
            : base(ExtractId(data),MicroServices.CommandType.Insert, "all", data, user,connectionId)
        {
            ActorType = UserListActor.ActorType;
        }

        private static string ExtractId(UserListItem data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        public UserListItem UserListItemData { get { return (UserListItem)base.Data; } }

    }


    class UserListDeleteCommand : UserListCommand
    {
        public UserListDeleteCommand(UserListItem data, string user, string connectionId)
            : base(ExtractId(data), MicroServices.CommandType.Delete, "IsActive", data, user, connectionId)
        {
            ActorType = UserListActor.ActorType;
        }
        private static string ExtractId(UserListItem data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        public string UserId { get { return FieldName; } }

        public UserListItem UserListItemData { get { return (UserListItem)base.Data; } }

    }
    class UserListUnDeleteCommand : UserListCommand
    {
        public UserListUnDeleteCommand(UserListItem data, string user, string connectionId)
            : base(ExtractId(data), MicroServices.CommandType.Undelete, "IsActive", data, user, connectionId)
        {
            ActorType = UserListActor.ActorType;
        }
        private static string ExtractId(UserListItem data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        public string UserId { get { return FieldName; } }

        public UserListItem UserListItemData { get { return (UserListItem)base.Data; } }

    }

    class UserListUpdateCommand : UserListCommand
    {
        public UserListUpdateCommand(UserListItem data, string user, string connectionId)
            : base(ExtractId(data), MicroServices.CommandType.Update,"", data, user, connectionId)
        {
            ActorType = UserListActor.ActorType;
        }

        private static string ExtractId(UserListItem data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        [JsonIgnoreAttribute] // Have to ignore this otherwise JSON.NET will serialize the data twice once in the base class Data and then this.
        public UserListItem UserListItemData { get { return (UserListItem)base.Data; } }

    }


}
