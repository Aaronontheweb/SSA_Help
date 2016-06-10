using Akka.Actor;
using EY.SSA.CommonBusinessLogic.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Requests
{
    public class SupervisorRequest : Request
    {
        public SupervisorRequest() { }

        public SupervisorRequest(IActorRef requestor, MicroServices.RequestType requestType, HTTPSourcedRequest originalRequest = null)
            : base(requestType, MicroServices.Area.Supervision, requestor, originalRequest)
        {
        }

    }

    public class SupervisorRegistryGetListRequest:SupervisorRequest
    {
        public SupervisorRegistryGetListRequest(){}

        public SupervisorRegistryGetListRequest(IActorRef requestor)
            : base(requestor,MicroServices.RequestType.GetList)
        {
        }

    }

    public class RegisterSupervisor:SupervisorRequest
    {
        public RegisterSupervisor(IActorRef requestor, string actorType, MicroServices.Area registrationArea):base(requestor,MicroServices.RequestType.Register,null)
        {
            ActorType = actorType;
            ResgistrationArea = registrationArea;
        }

        public string ActorType { get; private set; }
        public MicroServices.Area ResgistrationArea { get; private set; } // Client, Project, Entity, etc.

    }

    /// <summary>
    /// SPECIAL USE CASE
    /// 
    /// Supervisor attempted to register with the SupervisorRegistry and the operation timed out.
    /// </summary>
    public sealed class RegisterSupervisorTimeout
    {
        public RegisterSupervisorTimeout(RegisterSupervisor registration)
        {
            Registration = registration;
        }

        internal RegisterSupervisor Registration { get; private set; }
    }

    class SubscribeForSupervisorEventsRequest : SubscribeRequest
    {
        public SubscribeForSupervisorEventsRequest(IActorRef subscriber, MicroServices.Area area)
            : base(subscriber, area)
        {
        }

    }

    class UnSubscribeForSupervisorEventsRequest : SubscribeRequest
    {
        public UnSubscribeForSupervisorEventsRequest(IActorRef subscriber, MicroServices.Area area)
            : base(subscriber, area)
        {

        }
    }

}
