using Akka.Actor;
using EY.SSA.CommonBusinessLogic.General;

namespace EY.SSA.CommonBusinessLogic.Messages.Requests
{
    public abstract class SubscribeRequest:Request
    {

        public SubscribeRequest(IActorRef subscriber, MicroServices.Area area)
            : base(MicroServices.RequestType.Subscribe, area, subscriber)
        {
        }

    }

    class SubscribeForCommandEvents : SubscribeRequest
    {
        public SubscribeForCommandEvents(IActorRef subscriber, MicroServices.Area area) : base(subscriber, area)
        {
        }

    }

    class UnSubscribeForCommandEvents : SubscribeRequest
    {
        public UnSubscribeForCommandEvents(IActorRef subscriber, MicroServices.Area area)
            : base(subscriber, area)
        {

        }
    }

}
