using EY.SSA.CommonBusinessLogic.Messages.Actions;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.Messages.Requests
{
    // - Actions are abstract
    // - Commands is a kind of Action
    // - Requests is a kind of Action


    public class HTTPSourcedRequest:HTTPSourcedAction
    {
        public HTTPSourcedRequest():base()
        {

        }

        public HTTPSourcedRequest(string requestType, string area,  string user = null, string connectionId=null,string id = null):base(requestType,area,user,connectionId)
        {
            RequestType = requestType;
        }

        /// <summary>
        /// Returns a copy of the original command with the user name added.
        /// </summary>
        /// <param name="originalRequest"></param>
        /// <param name="user">user name</param>
        public HTTPSourcedRequest(HTTPSourcedRequest originalRequest, string user, string connectionId):base(originalRequest.Action,originalRequest.Area,user,connectionId)
        {
            RequestType = originalRequest.RequestType;
        }


        [JsonProperty]
        public string RequestType { get; private set; }

    }
}
