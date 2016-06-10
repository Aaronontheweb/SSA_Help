using EY.SSA.CommonBusinessLogic.State;
using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.General;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.Messages.Commands
{
    public abstract class ClientListCommand : Command
    {
        public ClientListCommand(string id, MicroServices.CommandType commandType, string field, object data, string user, string connectionId) : base(id, commandType, field, data, MicroServices.Area.Client, user, connectionId)
        {
        }
    }

    class ClientListInsertCommand :ClientListCommand
    {
        public ClientListInsertCommand(ClientListItem data, string user, string connectionId)
            : base(ExtractId(data),MicroServices.CommandType.Insert, "all", data, user,connectionId)
        {
            ActorType = ClientListActor.ActorType;
        }

        private static string ExtractId(ClientListItem data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        public ClientListItem ClientListItemData { get { return (ClientListItem)base.Data; } }

    }


    class ClientListDeleteCommand : ClientListCommand
    {
        public ClientListDeleteCommand(ClientListItem data, string user, string connectionId)
            : base(ExtractId(data), MicroServices.CommandType.Delete, "IsActive", data, user, connectionId)
        {
            ActorType = ClientListActor.ActorType;
        }
        private static string ExtractId(ClientListItem data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        public string ClientId { get { return FieldName; } }

        public ClientListItem ClientListItemData { get { return (ClientListItem)base.Data; } }

    }
    class ClientListUnDeleteCommand : ClientListCommand
    {
        public ClientListUnDeleteCommand(ClientListItem data, string user, string connectionId)
            : base(ExtractId(data), MicroServices.CommandType.Undelete, "IsActive", data, user, connectionId)
        {
            ActorType = ClientListActor.ActorType;
        }
        private static string ExtractId(ClientListItem data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        public string ClientId { get { return FieldName; } }

        public ClientListItem ClientListItemData { get { return (ClientListItem)base.Data; } }

    }

    class ClientListUpdateCommand : ClientListCommand
    {
        public ClientListUpdateCommand(ClientListItem data, string user, string connectionId)
            : base(ExtractId(data), MicroServices.CommandType.Update,"", data, user, connectionId)
        {
            ActorType = ClientListActor.ActorType;
        }

        private static string ExtractId(ClientListItem data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        [JsonIgnoreAttribute] // Have to ignore this otherwise JSON.NET will serialize the data twice once in the base class Data and then this.
        public ClientListItem ClientListItemData { get { return (ClientListItem)base.Data; } }

    }


}
