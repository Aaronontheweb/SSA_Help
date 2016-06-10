using Akka.Actor;
using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EY.SSA.CommonBusinessLogic.Messages.Response
{
    public class SupervisorRegistryGetListResponse:Response
    {
        public SupervisorRegistryGetListResponse() { }

        public SupervisorRegistryGetListResponse(IActorRef requestor, Dictionary<MicroServices.Area,IActorRef> areaToSupervisorActorRef, SupervisorRegistryGetListRequest originalRequest)
            : base(requestor, areaToSupervisorActorRef, originalRequest)
        {
                    AreaToSupervisorActorRef = areaToSupervisorActorRef;
        }

        public override object Reply {

            // Needs the ternary operator here to handle external code initialization.
            get { return base.Reply == null ? null: new Dictionary<MicroServices.Area, IActorRef>(base.Reply as Dictionary<MicroServices.Area, IActorRef>) ; }
            protected set { base.Reply = value; }
        }

        private Dictionary<MicroServices.Area, IActorRef> _AreaToSupervisorActorRef;

        public Dictionary<MicroServices.Area, IActorRef> AreaToSupervisorActorRef
        {
            get
            {
                return new Dictionary<MicroServices.Area, IActorRef>(_AreaToSupervisorActorRef as Dictionary<MicroServices.Area, IActorRef>);
            }
            private set { _AreaToSupervisorActorRef = value; }
        }

    }

    /// <summary>
    /// SPECIAL CASE PATTERN.
    ///
    /// Supervisor registry list was requested but it timed out. 
    /// </summary>
    public sealed class SupervisorRegistryGetListTimeout
    {
        public SupervisorRegistryGetListTimeout(SupervisorRegistryGetListRequest request)
        {
            Request = request;

        }

        public SupervisorRegistryGetListRequest Request { get; private set; }

    }
}
