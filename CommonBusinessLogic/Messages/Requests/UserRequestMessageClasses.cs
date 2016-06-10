using Akka.Actor;
using EY.SSA.CommonBusinessLogic.General;

namespace EY.SSA.CommonBusinessLogic.Messages.Requests
{
    public sealed class UserGetStateRequest:Request
    {
        public UserGetStateRequest(IActorRef requestor):base(MicroServices.RequestType.GetState,MicroServices.Area.User,requestor)
        {
        }
    }
}