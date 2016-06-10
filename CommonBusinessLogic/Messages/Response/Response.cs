using Akka.Actor;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.Messages.Response
{
    public abstract class Response
    {
        public Response() { }

        public Response(IActorRef requestor, object reply, Request originalRequest)
        {
            Requestor = requestor;
            Reply = reply;
            OriginalRequest = originalRequest;
        }

        [JsonProperty]
        public IActorRef Requestor { get; private set; }

        [JsonProperty]
        public virtual object Reply { get; protected set; }

        [JsonProperty]
        public Request OriginalRequest { get; private set; }
    }
}
