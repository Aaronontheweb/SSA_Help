using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.State;
using Newtonsoft.Json;
using System;

namespace EY.SSA.CommonBusinessLogic.Messages.Commands
{
    public abstract class UserCommand : Command
    {
        public UserCommand() { }

        public UserCommand(string id, MicroServices.CommandType commandType, string field, object data, string user, string connectionId) : base(id, commandType, field, data, MicroServices.Area.User, user, connectionId)
        {
        }
    }

    class UserDeleteCommand : UserCommand
    {
        public UserDeleteCommand(string id, string user, string connectionId)
            : base(id, MicroServices.CommandType.Delete, "isActive", null, user, connectionId)
        {
            ActorType = UserActor.ActorType;
        }

        public string ActorType { get; private set; }

    }
    class UserUnDeleteCommand : UserCommand
    {
        public UserUnDeleteCommand(string id, string user, string connectionId)
            : base(id, MicroServices.CommandType.Undelete, "isActive", null, user, connectionId)
        {
            ActorType = UserActor.ActorType;
        }

        public string ActorType { get; private set; }
    }

    public class UserInsertCommand : UserCommand
    {
        UserInsertCommand() { }
        public UserInsertCommand(UserState data, string user, string connectionId)
            : base(ExtractUserId(data),MicroServices.CommandType.Insert, "all", data, user, connectionId)
        {
            ActorType = UserActor.ActorType;
        }

        private static string ExtractUserId(UserState data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        [JsonIgnore] // Have to ignore this otherwise JSON.NET will serialize the data twice once in the base class Data and then this.
        public UserState UserStateData { get { return base.Data as UserState; } private set { } }

    }

    public class UserUpdateCommand : UserCommand
    {
        public UserUpdateCommand(UserState data, string user, string connectionId)
            : base(ExtractId(data), MicroServices.CommandType.Update, "", data, user, connectionId)
        {
            ActorType = UserActor.ActorType;
        }

        private static string ExtractId(UserState data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        public UserState UserStateData { get { return base.Data as UserState; } private set { } }
    }

    public class UserUpsertCommand : UserCommand
    {
        public UserUpsertCommand(UserState data, string user, string connectionId)
            : base(ExtractId(data), MicroServices.CommandType.Upsert,"", data, user, connectionId)
        {
            ActorType = UserActor.ActorType;
        }

        private static string ExtractId(UserState data)
        {
            return data.Id;
        }

        public string ActorType { get; private set; }

        [JsonIgnore] // Have to ignore this otherwise JSON.NET will serialize the data twice once in the base class Data and then this.
        public UserState UserStateData { get { return base.Data as UserState; } private set { } }

    }

}
