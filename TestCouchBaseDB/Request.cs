using Newtonsoft.Json;
using Akka.Actor;
using EY.SSA.CommonBusinessLogic.General;
using Newtonsoft.Json.Converters;

namespace EY.SSA.CommonBusinessLogic.Messages.Requests
{
    public abstract class Request
    {
        public Request() { }

        public Request(MicroServices.RequestType requestType, MicroServices.Area area, IActorRef requestor = null, object originalRequest=null)
        {
            Area = area;
            RequestType = requestType;
            Requestor = requestor;
            OriginalRequest = originalRequest;
        }

        [JsonProperty]
        public IActorRef Requestor { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MicroServices.Area Area { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MicroServices.RequestType RequestType { get; private set; } //getlist, subscription, etc.

        [JsonProperty]
        protected string RequestClass {
            get { return this.GetType().Name; } 
        }
        [JsonProperty]
        public object OriginalRequest { get; private set; }
    }
}
