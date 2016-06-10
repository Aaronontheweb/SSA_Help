using EY.SSA.CommonBusinessLogic.Messages.Actions;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.Messages.Commands
{
    // - Actions are abstract
    // - Commands is a kind of Action
    // - Requests is a kind of Action

    public class HTTPSourcedCommand:HTTPSourcedAction
    {
        public HTTPSourcedCommand():base()
        {

        }

        public HTTPSourcedCommand(string commandType, string area,  string fieldName = null, object data = null, string user = null,string connectionId=null, string id = null):base(commandType,area,user,connectionId)
        {
            CommandType = commandType;
            Data = data;
            FieldName = fieldName;
        }

        /// <summary>
        /// Returns a copy of the original command with the user name added.
        /// </summary>
        /// <param name="originalCommand"></param>
        /// <param name="user">user name</param>
        public HTTPSourcedCommand( HTTPSourcedCommand originalCommand,string user,string connectionId):base(originalCommand.CommandType,originalCommand.Area,user,connectionId)
        {
            CommandType = originalCommand.CommandType;
            Data = originalCommand.Data;
            FieldName = originalCommand.FieldName;
        }


        [JsonProperty]
        public string CommandType { get; private set; }
        [JsonProperty]
        public object Data { get; private set; }
        [JsonProperty]
        public string FieldName { get; private set; }

    }
}
