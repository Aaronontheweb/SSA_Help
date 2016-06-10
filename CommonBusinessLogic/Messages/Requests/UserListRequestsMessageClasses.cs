using Akka.Actor;
using EY.SSA.CommonBusinessLogic.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Requests
{
    public class UserListRequest : Request
    {

        public UserListRequest(IActorRef requestor, MicroServices.RequestType requestType, HTTPSourcedRequest originalRequest = null)
            : base(requestType, MicroServices.Area.User, requestor, originalRequest)
        {
        }

    }

    public sealed class UserGetListRequest : UserListRequest
    {

        public UserGetListRequest(IActorRef requestor, HTTPSourcedRequest originalRequest = null)
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

    public sealed class UserIdGetListRequest : UserListRequest
    {

        public UserIdGetListRequest(IActorRef requestor, HTTPSourcedRequest originalRequest = null)
            : base(requestor,MicroServices.RequestType.GetList, originalRequest)
        {
        }

    }

    public sealed class UserGetChildActorRefs : UserListRequest
    {
        public UserGetChildActorRefs(IActorRef requestor, HTTPSourcedRequest originalRequest = null)
            : base(requestor, MicroServices.RequestType.GetList, originalRequest)
        {
        }

    }
}
