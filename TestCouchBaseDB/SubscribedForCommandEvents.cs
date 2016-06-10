using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Events
{
    class SubscribedForCommandEvents : Subscribed
    {
        public SubscribedForCommandEvents(string subscriberActorType, string subscriberActorId, IActorRef subscriberActorRef)
            : base(subscriberActorType, subscriberActorId, subscriberActorRef)
        {

        }
    }


}
