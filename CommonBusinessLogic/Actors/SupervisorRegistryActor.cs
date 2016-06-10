using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Logger;
using Akka.Actor;
using Akka.Event;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.State;
using System.Collections.Immutable;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.BridgeInterfaces;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Response;
using System.Collections.ObjectModel;

namespace EY.SSA.CommonBusinessLogic.Actors
{


    public class SupervisorRegistryActor:ReceiveActor
    {
        #region fields

        private ILoggingAdapter _logger = Context.GetLogger();

        private static string _ActorType = typeof(SupervisorRegistryActor).Name;

        public static string ActorType
        {
            get { return _ActorType; }
        }

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> Subscribers = new HashSet<IActorRef>();

        // This Dictionary stores the active child actors associated with this admin actor.
        private Dictionary<MicroServices.Area, SupervisorInfo> _KnownSupervisorsActors;

        #endregion fields

        public SupervisorRegistryActor() 
        {
            _KnownSupervisorsActors = new Dictionary<MicroServices.Area, SupervisorInfo>();

            // Put actor in recovering state
            Initializing();
        }

        #region Actor States

        /// <summary>
        /// This method sets the actor so that it can "boot" itself up.
        /// </summary>
        private void Initializing()
        {
            _logger.Debug("Initializing.");


            // This catch all will log if there are any weird unhandled messages.
            Receive<object>(o =>
            {
                _logger.Debug("Got unhandled message From:{0}", Sender.Path.ToStringWithAddress());
            });

            Become(Ready);
            _logger.Debug("Initialized.");

        }

        /// <summary>
        /// The command processing state sets this actor ready to receive any command from other actors.
        /// </summary>
        private void Ready()
        {
            _logger.Debug("Getting Ready.");
            // Supervisors will attempt to register
            Receive<RegisterSupervisor>(m => HandleRegisterSupervisor(m));

            // Handle a request to get a list of the supervisors
            Receive<SupervisorRegistryGetListRequest>(r => HandleGetListRequest(r));

            // String command handler
            Receive<string>(s=> HandleStringCommand(s));

            // This catch all will log if there are any weird unhandled messages.
            Receive<object>(o =>
            {
                _logger.Debug("Received unhandled message from:{0} Unhandled Message:{1}", Sender.Path.ToStringWithAddress(), o.GetType().Name);
            });

            _logger.Debug("Ready.");


        }

        #endregion Actor States

        #region String Messages/Commands
        private void HandleStringCommand(string s)
        {
            if (s != null)
            {

                switch (s)
                {
                    case "PrintSupervisorList":
                    {
                        foreach(KeyValuePair<MicroServices.Area,SupervisorInfo> kvp in _KnownSupervisorsActors)
                        {
                            _logger.Debug("Area:{0} - Supervisor:{1}", kvp.Key, kvp.Value.ActorType);
                        }
                        break;
                    }
                    default:
                        {
                            _logger.Debug("Received unhandled message from:{0} Unhandled Message:{1}", Sender.Path.ToStringWithAddress(), s);
                            break;
                        }

                }
            }
        }
        #endregion String Messages/Commands

        #region RequestHandlers
        private void HandleRegisterSupervisor(RegisterSupervisor m)
        {
            // Register the supervisor
            SupervisorInfo sI = new SupervisorInfo(m.ActorType, m.ResgistrationArea, m.Requestor);
            if(_KnownSupervisorsActors.Keys.Contains(sI.Area))
            {
                _KnownSupervisorsActors[sI.Area] = sI;
            }
            else
            {
                _KnownSupervisorsActors.Add(sI.Area, sI);
            }

            // Let the Supervisor know that we got it...
            Sender.Tell(new SupervisorRegistrationEvent(m, true));
        }

        private void HandleGetListRequest(SupervisorRegistryGetListRequest r)
        {
            // Immutable dictionaries are NOT serializable across the wire.
            //ImmutableDictionary<MicroServices.Area,IActorRef> immutableDictOfSupervisorsActors = 
            //    _KnownSupervisorsActors.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.SupervisorActorReference);
            //Sender.Tell(new SupervisorRegistryGetListResponse(r.Requestor,immutableDictOfSupervisorsActors,r));
            //Sender.Tell(new SupervisorRegistryGetListResponse(r.Requestor,ImmutableDictionary.Create<MicroServices.Area,IActorRef>(),r));
            // This does not work either
            //ImmutableDictionary<MicroServices.Area,SupervisorInfo> immutableDictOfSupervisorsActors =
            //    _KnownSupervisorsActors.ToImmutableDictionary<MicroServices.Area, SupervisorInfo>();

            _logger.Debug("Sending list of supervisors to:{0}", r.Requestor.Path.ToStringWithAddress());

            Dictionary<MicroServices.Area,IActorRef> dictOfSupervisorsActors = 
                _KnownSupervisorsActors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.SupervisorActorReference);

            Sender.Tell(new SupervisorRegistryGetListResponse(r.Requestor,dictOfSupervisorsActors,r));

        }


        #endregion RequestHandlers

        #region Helper Methods

        #endregion Helper Methods

        #region Subscribers

        #endregion Subscribers

        #region Life Cycle Event Handlers
        protected override void PreStart()
        {
            base.PreStart();

        }

        protected override void PreRestart(Exception reason, object message)
        {
            base.PreRestart(reason, message);

        }
        #endregion Life Cycle Event Handlers

    }
    public class SupervisorInfo
    {

        public SupervisorInfo() { }

        public SupervisorInfo(string actorType, MicroServices.Area area, IActorRef reference)
        {
            ActorType = actorType;
            Area = area;
            SupervisorActorReference = reference;

        }

        public SupervisorInfo(RegisterSupervisor rsMsg)
        {
            ActorType = rsMsg.ActorType;
            Area = rsMsg.Area;
            SupervisorActorReference = rsMsg.Requestor;

        }

        public string ActorType { get; private set; }
        public MicroServices.Area Area { get; private set; } // Client, Project, Entity, etc.

        public IActorRef SupervisorActorReference { get; private set; }
    }

}
