using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using EY.SSA.CommonBusinessLogic.General;
using Newtonsoft.Json.Converters;

namespace EY.SSA.CommonBusinessLogic.Messages.Commands
{
    public abstract class Command
    {
        public Command() { }
        public Command(string id, MicroServices.CommandType commandType, string fieldName, object data, MicroServices.Area area, string user, string connectionId)
        {
            Id = id;
            Area = area;
            CommandType = commandType;
            FieldName = fieldName;
            Data = data;
            User = user;
            ConnectionId = connectionId;
        }

        [JsonProperty]
        public string Id { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MicroServices.Area Area { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MicroServices.CommandType CommandType { get; private set; } //Insert, Delete, Update, Upsert 

        [JsonProperty]
        public string FieldName { get; private set; }

        [JsonProperty]
        public object Data { get; private set; }

        [JsonProperty]
        protected string CommandClass {
            get { return this.GetType().Name; } 
        }


        [JsonProperty]
        public string User { get; private set; }
        [JsonProperty]
        public string ConnectionId { get; private set; }
    }
}
