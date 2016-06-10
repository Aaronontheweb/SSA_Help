using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Actions
{
    public abstract class HTTPSourcedAction
    {
        public HTTPSourcedAction()
        {
        }

        public HTTPSourcedAction(string action, string area, string user = null, string connectionId = null)
        {
            Action = action;
            Area = area;
            ConnectionId = connectionId;
            User = user;
        }

        [JsonProperty]
        public string Action { get; private set; }
        [JsonProperty]
        public string Area { get; private set; }
        [JsonProperty]
        public string User { get; private set; }
        [JsonProperty]
        public string ConnectionId { get; private set; }
    }
}
