using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Events
{
    abstract public class Subscribed
    {
        public Subscribed(string actorType, string id, IActorRef actorRef)
        {
            ActorRef = actorRef;
            Id = id;
            ActorType = actorType;

        }

        public IActorRef ActorRef { get; private set; }

        public string ActorType { get; private set; }

        public string Id { get; private set; }
    }
}
