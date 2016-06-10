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
    public class UserIdGetListResponse : Response
    {
        public UserIdGetListResponse(IActorRef originalRequestor, ImmutableArray<string> arrUserIds, UserIdGetListRequest originalRequest) : base(originalRequestor, arrUserIds, originalRequest)
        {

        }

        public ImmutableArray<string> ListOfUserIds
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

    public class UserGetChildActorRefsResponse : Response
    {
        public UserGetChildActorRefsResponse(IActorRef originalRequestor, ImmutableArray<IActorRef> arrActorRefs, UserGetChildActorRefs originalRequest) : base(originalRequestor, arrActorRefs, originalRequest)
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

    public class UserGetListResponse : Response
    {
        public UserGetListResponse() : base(){ }

        public UserGetListResponse(IActorRef requestor, ImmutableList<UserState> reply, UserGetListRequest originalRequest) : base(requestor, reply, originalRequest)
        {

        }

        [JsonIgnore]
        public ImmutableList<UserState> ListOfUserStates
        {
            get {
                if (Reply != null)
                    return (ImmutableList<UserState>)Reply;
                else
                    return ImmutableList<UserState>.Empty;
            }
        }


    }

    public class UserGetStateResponse : Response
    {
        public UserGetStateResponse(IActorRef originalRequestor, UserState userState, UserGetStateRequest originalRequest) : base(originalRequestor, userState, originalRequest)
        {

        }

        public UserState ReplyUserState
        {
            get
            {
                if (Reply != null)
                    return (UserState)Reply;
                else
                    return null;
            }
        }
    }

}
