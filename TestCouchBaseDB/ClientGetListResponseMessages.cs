using Akka.Actor;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.State;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Response
{
    public class ClientIdGetListResponse : Response
    {
        public ClientIdGetListResponse(IActorRef originalRequestor, ImmutableArray<string> arrClientIds, ClientIdGetListRequest originalRequest) : base(originalRequestor, arrClientIds, originalRequest)
        {

        }

        public ImmutableArray<string> ListOfClientIds
        {
            get
            {
                if (Reply != null)
                    return (ImmutableArray<string>)Reply;
                else
                    return new ImmutableArray<string>();
            }
        }
    }

    public class ClientGetChildActorRefsResponse : Response
    {
        public ClientGetChildActorRefsResponse(IActorRef originalRequestor, ImmutableArray<IActorRef> arrActorRefs, ClientGetChildActorRefs originalRequest) : base(originalRequestor, arrActorRefs, originalRequest)
        {

        }

        public ImmutableArray<IActorRef> ListOfChildActorRefs
        {
            get
            {
                if (Reply != null)
                    return (ImmutableArray<IActorRef>)Reply;
                else
                    return new ImmutableArray<IActorRef>();
            }
        }
    }

    public class ClientGetListResponse : Response
    {
        public ClientGetListResponse() : base(){ }

        public ClientGetListResponse(IActorRef requestor, ImmutableList<ClientState> reply, ClientGetListRequest originalRequest) : base(requestor, reply, originalRequest)
        {

        }

        [JsonIgnore]
        public ImmutableList<ClientState> ListOfClientStates
        {
            get {
                if (Reply != null)
                    return (ImmutableList<ClientState>)Reply;
                else
                    return ImmutableList<ClientState>.Empty;
            }
        }


    }

    public class ClientGetStateResponse : Response
    {
        public ClientGetStateResponse(IActorRef originalRequestor, ClientState clientState, ClientGetStateRequest originalRequest) : base(originalRequestor, clientState, originalRequest)
        {

        }

        public ClientState ReplyClientState
        {
            get
            {
                if (Reply != null)
                    return (ClientState)Reply;
                else
                    return null;
            }
        }
    }

}
