using Akka.Actor;
using EY.SSA.CommonBusinessLogic.General;

namespace EY.SSA.CommonBusinessLogic.Messages.Requests
{
    public class ClientListRequest : Request
    {
        public ClientListRequest(){}
        public ClientListRequest(IActorRef requestor, MicroServices.RequestType requestType, HTTPSourcedRequest originalRequest = null)
            : base(requestType, MicroServices.Area.Client, requestor, originalRequest)
        {
        }

    }

    public sealed class ClientGetListRequest : ClientListRequest
    {
        public ClientGetListRequest(){}

        public ClientGetListRequest(IActorRef requestor, HTTPSourcedRequest originalRequest = null)
            : base(requestor,MicroServices.RequestType.GetList,originalRequest)
        {
        }

        public HTTPSourcedRequest OriginalHTTPRequest{
                get{
                    if (OriginalRequest == null)
                        return null;
                    else
                        return (HTTPSourcedRequest)OriginalRequest;
                }
            }

    }

    public sealed class ClientIdGetListRequest : ClientListRequest
    {
        public ClientIdGetListRequest(){}

        public ClientIdGetListRequest(IActorRef requestor, HTTPSourcedRequest originalRequest = null)
            : base(requestor,MicroServices.RequestType.GetList, originalRequest)
        {
        }

    }

    public sealed class ClientGetChildActorRefs:ClientListRequest
    {
        public ClientGetChildActorRefs(){}
        public ClientGetChildActorRefs(IActorRef requestor, HTTPSourcedRequest originalRequest = null)
            : base(requestor,MicroServices.RequestType.GetList, originalRequest)
        {
        }

    }
}
