using Akka.Actor;
using EY.SSA.CommonBusinessLogic.General;

namespace EY.SSA.CommonBusinessLogic.Messages.Requests
{
    public class ClientRequest : Request
    {

        public ClientRequest(IActorRef requestor, MicroServices.RequestType requestType, HTTPSourcedRequest originalRequest = null)
            : base(requestType, MicroServices.Area.Client, requestor, originalRequest)
        {
        }

    }

    public sealed class ClientGetStateRequest:ClientRequest
    {
        public ClientGetStateRequest(IActorRef requestor):base(requestor, MicroServices.RequestType.GetState)
        {
        }
    }
}