using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using System.Collections.Immutable;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Response;

namespace EY.SSA.CommonBusinessLogic.Actors
{
    public class ClientSupervisor : ReceiveActor, IWithUnboundedStash
    {
        #region fields

        private ILoggingAdapter _logger = Context.GetLogger();

        private static string _ActorType = typeof(ClientSupervisor).Name;

        private static string _ClientListPersistenceId = "ClientList";

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> Subscribers = new HashSet<IActorRef>();

        private string _ClientStateAccumulator = "ClientStateAccumulator"; // Maintains the list of clients up-to-date.

        private int _RegistrationRetryTimes;
        private IActorRef _SupervisorRegistry;
        private TimeSpan _registerTimeout;

        #endregion fields

        #region properties

        public static string ActorType
        {
            get { return _ActorType; }
        }

        public IStash Stash { get; set; }

        #endregion properties

        public ClientSupervisor()
        {

            _registerTimeout = TimeSpan.FromMilliseconds(1000);

            // Put actor in recovering state
            Become(Registering);
        }

        #region Actor States

        private void Registering()
        {
            _logger.Info("Is registering.");
            _logger.Info("Waiting for Registry Supervisor.");

            Receive<SupervisorRegistryReady>(e => {
                _SupervisorRegistry = e.RegistrySupervisorActorRef;
                AttemptSupervisorRegistrationHelper(); // now that we have the Supervisor Registry IActorRef
            });

            // If supervisor registered successfully...
            Receive<SupervisorRegistrationEvent>(e =>
            {
                if (e.Registered)
                {
                    // Once all client actors have been instantiated and we have registered with the supervisor register we are good to go
                    _logger.Info("Registered");
                    Become(Initializing);
                }
                else
                {
                    _logger.Error("Supervisor '{0}' failed to register. Something went really wrong - killing supervisor.", Self.Path.ToStringWithAddress());
                    // Retry the registration again
                    AttemptSupervisorRegistrationHelper();
                }
            });

            // If the registration process failed...
            Receive<RegisterSupervisorTimeout>(e => {
                // Try it again _RegistrationRetryTimes then give up
                if ((_RegistrationRetryTimes--) == 0)
                {
                    _logger.Error("Supervisor '{0}' registration took too long timing out. {1} retries left - killing supervisor.", Self.Path.ToStringWithAddress(), _RegistrationRetryTimes);
                    Context.Stop(Self);
                }
                _logger.Warning("Supervisor '{0}' registration took too long timing out. {1} retries left", Self.Path.ToStringWithAddress(), _RegistrationRetryTimes);

                AttemptSupervisorRegistrationHelper();

            });

            // This catch all will log if there are any weird unhandled messages.
            Receive<object>(o =>
            {
                Stash?.Stash();
                _logger.Debug("Received unhandled message from:{0} Unhandled Message:{1} - Stashing.", Sender.Path.ToStringWithAddress(), o.GetType().Name);
            });

        }

        /// <summary>
        /// This method sets the actor so that it can "boot" itself up.
        /// </summary>
        private void Initializing()
        {
            _logger.Debug("Initializing.");

            // Instantiate the ClientList Actor.  Get the list of Clients and then instantiate the client actors(each client actor manages one client)
            _logger.Info("Instantiating ClientList actor.");
            IActorRef cl = Context.ActorOf(Props.Create<ClientListActor>(), _ClientListPersistenceId);
            _logger.Info("Instantiated ClientList actor.");

            // Instantiate the ClientStateAccumulator Actor.  Keeps a list of all client states.
            _logger.Info("Instantiating ClientStateAccumulator actor.");
            Context.ActorOf(Props.Create<ClientStateAccumulator>(), _ClientStateAccumulator);
            _logger.Info("Instantiated ClientStateAccumulator actor.");

            Receive<ClientGetChildActorRefs>(r => {
                _logger.Info($"Received message {r.GetType().Name}. Stashing not ready to handle.");
                Stash?.Stash();
            });

            var timeout = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(1000,1000, Context.Child(_ClientListPersistenceId), new ClientIdGetListRequest(Self), Self);

            // During this state we will instantiate the Child actors and make sure the ClientList and ClientStateAccumulator actors are subscribed with the children for all updates.
            Receive<ClientIdGetListResponse>(r => {
                timeout.Cancel();
                InstantiateClientChildActors(r);
                Become(Ready);
            });

            Receive<DeadLetter>(d =>
            {
                if (d != null)
                {
                    _logger.Debug(d?.Sender?.Path.ToStringWithAddress());
                    _logger.Debug(d?.Recipient?.Path.ToStringWithAddress());
                    _logger.Debug(d?.Message?.ToString());
                }
            });


            Receive<string>(s => {
                if (s == "Send Message")
                    Context.Child(_ClientListPersistenceId).Tell(new ClientIdGetListRequest(Self,null));
                _logger.Debug($"Sending Message from:{Context.Child(_ClientListPersistenceId).Path.Name} Message:ClientIdGetListRequest");

            });

            // This catch all will log if there are any weird unhandled messages.
            Receive<object>(message =>
            {
                Stash?.Stash();
                _logger.Debug($"Received unhandled message from:{Sender.Path.ToStringWithAddress()} Unhandled Message of type:{message.GetType().Name} - Stashing.");
            });



            // Request the client list to instantiate all the client actors
            Context.System.Scheduler.ScheduleTellOnce(10000, Context.Child(_ClientListPersistenceId), new ClientIdGetListRequest(Self), Self);
            Context.System.Scheduler.ScheduleTellOnce(1000, Self, "Send Message", Self);
            Context.System.Scheduler.ScheduleTellOnce(2000, Self, "Send Message", Self);
            Context.System.Scheduler.ScheduleTellOnce(3000, Self, "Send Message", Self);
            Context.System.Scheduler.ScheduleTellOnce(4000, Self, "Send Message", Self);
            Context.System.Scheduler.ScheduleTellOnce(5000, Self, "Send Message", Self);
            Context.System.Scheduler.ScheduleTellOnce(6000, Self, "Send Message", Self);
            Context.System.Scheduler.ScheduleTellOnce(7000, Self, "Send Message", Self);
            Context.System.Scheduler.ScheduleTellOnce(8000, Self, "Send Message", Self);
            Context.System.Scheduler.ScheduleTellOnce(9000, Self, "Send Message", Self);

            _logger.Debug("Initialized.");

        }

        /// <summary>
        /// The command processing state sets this actor ready to receive any command from other actors.
        /// </summary>
        private void Ready()
        {
            _logger.Debug("Getting Ready.");

            // Handle a request to get the list of clients
            Receive<ClientGetListRequest>(r => {
                Context.Child(_ClientStateAccumulator).Forward(r);
            });

            // Forward ClientList actor commands so that the client list actor can do something about it.
            Receive<ClientListCommand>(c => { Context.Child(_ClientListPersistenceId).Forward(c); });

            // Forward Client Child actor commands so that the child can do something about it.
            Receive<ClientDeleteCommand>(c => {
                if (Context.Child(c.Id) == ActorRefs.Nobody)
                {
                    Sender.Tell(new ClientFailedDeleteEvent($"Client {c.Id} does not exist or has been deleted.", c.Id, c.User, c.ConnectionId));
                }
                else
                    Context.Child(c.Id).Forward(c);
            });
            Receive<ClientUnDeleteCommand>(c => {
                if (Context.Child(c.Id) == ActorRefs.Nobody)
                {
                    Sender.Tell(new ClientFailedUnDeleteEvent($"Client {c.Id} does not exist.", c.Id, c.User, c.ConnectionId));
                }
                else
                    Context.Child(c.Id).Forward(c);
            });
            Receive<ClientUpdateCommand>(c => {
                if (Context.Child(c.Id) == ActorRefs.Nobody)
                {
                    Sender.Tell(new ClientFailedDeleteEvent($"Client {c.ClientStateData.Name} does not exist or has been deleted.", c.ClientStateData, c.User, c.ConnectionId));
                }
                else
                    Context.Child(c.Id).Forward(c);
            });


            Receive<ClientInsertCommand>(c => {
                if (c.Id == null || c.Id == "")
                {
                    string id = Guid.NewGuid().ToString();
                    var newActor = InstantiateClientActor(id);
                    newActor.Forward(c);

                }
                else
                {
                    //Todo add reply error msg here
                }
            });

            // Provide to the requestor the list of children actor refs
            Receive<ClientGetChildActorRefs>(r => {
                r.Requestor.Tell(new ClientGetChildActorRefsResponse(r.Requestor, Context.GetChildren().ToImmutableArray(), r));
            });

            Receive<CommandEventMessage>(e => HandleCommandEventMessage(e));

            // String command handler
            Receive<string>(s => HandleStringCommand(s));

            // This catch all will log if there are any weird unhandled messages.
            Receive<object>(message =>
            {
                _logger.Debug("Received unhandled message from:{0} Unhandled Message of type:{1} - Stashing.", Sender.Path.ToStringWithAddress(), message.GetType().Name);
            });

            Stash?.UnstashAll();

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
                    case "PrintChildActors":
                        {
                            
                            foreach (IActorRef aRef in Context.GetChildren())
                            {
                                _logger.Debug("ActorName:{0} - Full path:{1}", aRef.Path.Name,aRef.Path.ToStringWithAddress());
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

        #region Event Handlers
        private bool HandleCommandEventMessage(CommandEventMessage e)
        {
            //If a client was inserted then we need to subscribe to the events
            return true;
        }

        #endregion Event Handlers

        #region RequestHandlers



        #endregion RequestHandlers

        #region Helper Methods
        /// <summary>
        /// This helper function instantiates the Client child actors
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private void InstantiateClientChildActors(ClientIdGetListResponse r)
        {
            foreach (string ActorPersistenceId in r.ListOfClientIds)
            {
                InstantiateClientActor(ActorPersistenceId);
            }
        }

        private IActorRef InstantiateClientActor(string ActorPersistenceId)
        {
            _logger.Info($"Instantiating Client actor id:'{ActorPersistenceId}'.");
            //Instantiate the Client List Actor
            IActorRef clientActor = Context.ActorOf(Props.Create<ClientActor>(), ActorPersistenceId); // Continue here change the client actor to use the name in the path as the ClientList
            _logger.Info($"Instantiated Client actor id:'{ActorPersistenceId}'.");

            // Make sure the actor that maintains the state for all clients is kept up-to-date as the client's state mutate
            clientActor.Tell(new SubscribeForCommandEvents(Context.Child(_ClientStateAccumulator), MicroServices.Area.Client));
            // also subscribe the ClientList actor since we need to maintain a list of actors across restarts
            clientActor.Tell(new SubscribeForCommandEvents(Context.Child(_ClientListPersistenceId), MicroServices.Area.Client));

            return clientActor;
        }

        private void AttemptSupervisorRegistrationHelper()
        {
            // Register this supervisor with the SupervisorRegister so that the Bridge(s) can find it
            RegisterSupervisor registration = new RegisterSupervisor(Self, _ActorType, General.MicroServices.Area.Client);
            _SupervisorRegistry.Ask<object>(registration, _registerTimeout).ContinueWith<SupervisorRegistrationEvent>(te =>
            {
                object o = te.Result;
                SupervisorRegistrationEvent rEvt=null;
                if (o.GetType() == typeof(SupervisorRegistrationEvent))
                    rEvt = te.Result as SupervisorRegistrationEvent;

                if (te.IsFaulted || te.IsCanceled)
                    return new SupervisorRegistrationEvent(rEvt.RegisteredSupervisor, false);
                return new SupervisorRegistrationEvent(rEvt.RegisteredSupervisor, true);
            }).PipeTo<SupervisorRegistrationEvent>(Self);

        }


        #endregion Helper Methods

        #region Subscribers
        #endregion Subscribers


    }
}
