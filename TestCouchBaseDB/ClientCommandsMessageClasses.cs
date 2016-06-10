using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.State;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.Messages.Commands
{
    public abstract class ClientCommand : Command
    {
        public ClientCommand() { }

        public ClientCommand(string id, MicroServices.CommandType commandType, string field, object data, string user, string connectionId) : base(id, commandType, field, data, MicroServices.Area.Client, user, connectionId)
        {
        }
    }

    class ClientDeleteCommand : ClientCommand
    {
        public ClientDeleteCommand(string id, string user, string connectionId)
            : base(id, MicroServices.CommandType.Delete, "isActive", null, user, connectionId)
        {
            ActorType = ClientActor.ActorType;
        }

        public string ActorType { get; private set; }

    }
    class ClientUnDeleteCommand : ClientCommand
    {
        public ClientUnDeleteCommand(string id, string user, string connectionId)
            : base(id, MicroServices.CommandType.Undelete, "isActive", null, user, connectionId)
        {
            ActorType = ClientActor.ActorType;
        }

        public string ActorType { get; private set; }
    }

    public class ClientInsertCommand : ClientCommand
    {
        ClientInsertCommand() { }
        public ClientInsertCommand(ClientState data, string user, string connectionId)
            : base(ExtractClientId(data),MicroServices.CommandType.Insert, "all", data, user, connectionId)
        {
            ActorType = ClientActor.ActorType;
        }

        private static string ExtractClientId(ClientState data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        [JsonIgnore] // Have to ignore this otherwise JSON.NET will serialize the data twice once in the base class Data and then this.
        public ClientState ClientStateData { get { return base.Data as ClientState; } private set { } }

    }

    public class ClientUpdateCommand : ClientCommand
    {
        public ClientUpdateCommand(ClientState data, string user, string connectionId)
            : base(ExtractId(data), MicroServices.CommandType.Update, "", data, user, connectionId)
        {
            ActorType = ClientActor.ActorType;
        }

        private static string ExtractId(ClientState data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        public ClientState ClientStateData { get { return base.Data as ClientState; } private set { } }
    }

    public class ClientUpsertCommand : ClientCommand
    {
        public ClientUpsertCommand(ClientState data, string user, string connectionId)
            : base(ExtractId(data), MicroServices.CommandType.Upsert,"", data, user, connectionId)
        {
            ActorType = ClientActor.ActorType;
        }

        private static string ExtractId(ClientState data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        [JsonIgnore] // Have to ignore this otherwise JSON.NET will serialize the data twice once in the base class Data and then this.
        public ClientState ClientStateData { get { return base.Data as ClientState; } private set { } }

    }

}
