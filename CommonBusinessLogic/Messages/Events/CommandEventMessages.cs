using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.State;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.Messages.Events
{
    public class CommandEventMessage
    {
        //Client Information
        public string Id { get; private set; }
        public string Name { get; private set; }

        //StateInfo
        public bool Success { get; private set; }

        public string ActorType { get; private set; }

        public MicroServices.Area Area { get; private set; }

        [JsonProperty]
        public object Data { get; private set; }

        public string Message { get; private set; }

        public string User { get; private set; }

        public string ConnectionId { get; private set; }

        public MicroServices.CommandType CommandType { get; private set; }

        public CommandEventMessage() { }

        public CommandEventMessage(string id, string name, bool isSuccesful, string actorType, MicroServices.CommandType commandType,MicroServices.Area area, string message, object data, string user, string connectionId)
        {
            Id = id;
            Name = name;
            Success = isSuccesful;
            ActorType = actorType;
            CommandType = commandType;
            Area = area;
            Message = message;
            Data = data;
            User = user;
            ConnectionId = connectionId;
        }

    }

    public class InsertCommandEventMessage : CommandEventMessage
    {
        public InsertCommandEventMessage() : base() { }
        public InsertCommandEventMessage(string id, string name, string actorType, MicroServices.Area area, string message, object data, string user, string connectionId)
            :base(id, name, true, actorType, MicroServices.CommandType.Insert, area, message, data, user, connectionId)
        {

        }
    }
    public class UpdateCommandEventMessage : CommandEventMessage
    {
        public UpdateCommandEventMessage() : base() { }
        public UpdateCommandEventMessage(string id, string name, string actorType, MicroServices.Area area, string message, object data, string user, string connectionId) 
            :base(id, name, true, actorType, MicroServices.CommandType.Update, area, message, data, user, connectionId)
        {

        }
    }
    public class UpsertCommandEventMessage : CommandEventMessage
    {
        public UpsertCommandEventMessage() : base() { }
        public UpsertCommandEventMessage(string id, string name, string actorType, MicroServices.Area area, string message, object data, string user, string connectionId) 
            :base(id, name, true, actorType, MicroServices.CommandType.Upsert, area, message, data, user, connectionId)
        {

        }
    }
    public class DeleteCommandEventMessage : CommandEventMessage
    {
        public DeleteCommandEventMessage() : base() { }
        public DeleteCommandEventMessage(string id, string name, string actorType, MicroServices.Area area, string message, object data, string user, string connectionId) 
            :base(id, name, true, actorType, MicroServices.CommandType.Delete, area, message, data, user, connectionId)
        {

        }
    }
    public class UnDeleteCommandEventMessage : CommandEventMessage
    {
        public UnDeleteCommandEventMessage() : base() { }
        public UnDeleteCommandEventMessage(string id, string name, string actorType, MicroServices.Area area, string message, object data, string user, string connectionId) 
            :base(id, name, true, actorType, MicroServices.CommandType.Undelete, area, message, data, user, connectionId)
        {

        }
    }
    public class FailedInsertCommandEventMessage : CommandEventMessage
    {
        public FailedInsertCommandEventMessage() : base() { }
        public FailedInsertCommandEventMessage(string id, string name, string actorType, MicroServices.Area area, string message, object data, string user, string connectionId)
            : base(id, name, false, actorType, MicroServices.CommandType.Insert, area, message, data, user, connectionId)
        {

        }
    }
    public class FailedUpdateCommandEventMessage : CommandEventMessage
    {
        public FailedUpdateCommandEventMessage() : base() { }
        public FailedUpdateCommandEventMessage(string id, string name, string actorType, MicroServices.Area area, string message, object data, string user, string connectionId) :
            base(id, name, false, actorType, MicroServices.CommandType.Update, area, message, data, user, connectionId)
        {

        }
    }
    public class FailedUpsertCommandEventMessage : CommandEventMessage
    {
        public FailedUpsertCommandEventMessage() : base() { }
        public FailedUpsertCommandEventMessage(string id, string name, string actorType, MicroServices.Area area, string message, object data, string user, string connectionId) :
            base(id, name, false, actorType, MicroServices.CommandType.Upsert, area, message, data, user, connectionId)
        {

        }
    }
    public class FailedDeleteCommandEventMessage : CommandEventMessage
    {
        public FailedDeleteCommandEventMessage() : base() { }
        public FailedDeleteCommandEventMessage(string id, string name, string actorType, MicroServices.Area area, string message, object data, string user, string connectionId) :
            base(id, name, false, actorType, MicroServices.CommandType.Delete, area, message, data, user, connectionId)
        {

        }
    }
    public class FailedUnDeleteCommandEventMessage : CommandEventMessage
    {
        public FailedUnDeleteCommandEventMessage() : base() { }
        public FailedUnDeleteCommandEventMessage(string id, string name, string actorType, MicroServices.Area area, string message, object data, string user, string connectionId) :
            base(id, name, false, actorType, MicroServices.CommandType.Undelete, area, message, data, user, connectionId)
        {

        }
    }

}
