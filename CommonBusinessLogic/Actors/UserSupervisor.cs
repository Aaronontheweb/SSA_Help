using Akka.Actor;
using Akka.Event;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.Messages.Response;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EY.SSA.CommonBusinessLogic.Actors
{


    public class UserSupervisor : ReceiveActor, IWithUnboundedStash
    {
        #region fields

        private ILoggingAdapter _logger = Context.GetLogger();

        private static string _ActorType = typeof(UserSupervisor).Name;

        // This is the unique Id that will be used to find all users
        private static string _UserListPersistenceId = "UserList";

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> Subscribers = new HashSet<IActorRef>();

        private string _UserStateAccumulator = "UserStateAccumulator"; // Maintains the list of Users up-to-date.

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

        public UserSupervisor()
        {

            _registerTimeout = TimeSpan.FromMilliseconds(1000);

            // Put actor in recovering state
            Registering();
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
                    // Once all User actors have been instantiated and we have registered with the supervisor register we are good to go
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
                _logger.Debug($"In \"Registering\" state, stashing message from:{Sender.Path.ToStringWithAddress()} Deferred Message:{o.GetType().Name}.");
            });

        }

        /// <summary>
        /// This method sets the actor so that it can "boot" itself up.
        /// </summary>
        private void Initializing()
        {
            _logger.Debug("Initializing.");

            // Instantiate the UserList Actor.  Get the list of Users and then instantiate the User actors(each User actor manages one User)
            _logger.Info("Instantiating UserList actor.");
            Context.ActorOf(Props.Create<UserListActor>(), _UserListPersistenceId);
            _logger.Info("Instantiated UserList actor.");

            // Instantiate the UserStateAccumulator Actor.  Keeps a list of all User states.
            _logger.Info("Instantiating UserStateAccumulator actor.");
            Context.ActorOf(Props.Create<UserStateAccumulator>(), _UserStateAccumulator);
            _logger.Info("Instantiated UserStateAccumulator actor.");

            Receive<UserGetChildActorRefs>(r => {
                _logger.Info($"Received message {r.GetType().Name}. Stashing not ready to handle.");
                Stash?.Stash();
            });


            // Request the User list to instantiate all the User actors
            Context.Child(_UserListPersistenceId).Tell(new UserIdGetListRequest(Self));

            var timeout = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(1000,5000, Context.Child(_UserListPersistenceId), new UserIdGetListRequest(Self), Self);


            // During this state we will instantiate the Child actors and make sure the UserList and UserStateAccumulator actors are subscribed with the children for all updates.
            Receive<UserIdGetListResponse>(r => {
                timeout.Cancel();
                InstantiateUserChildActors(r);
                Become(Ready);
            });
            
            // This catch all will log if there are any weird unhandled messages.
            Receive<object>(message =>
            {
                Stash?.Stash();
                _logger.Debug($"Received unhandled message from:{Sender.Path.ToStringWithAddress()} Unhandled Message of type:{message.GetType().Name} - Stashing.");
            });

            _logger.Debug("Initialized.");

        }

        /// <summary>
        /// The command processing state sets this actor ready to receive any command from other actors.
        /// </summary>
        private void Ready()
        {
            _logger.Debug("Getting Ready.");

            //
            // Handle User State Accumulator messages
            //
            // Handle a request to get the list of Users
            Receive<UserGetListRequest>(r => {
                Context.Child(_UserStateAccumulator).Forward(r);
            });

            //
            // Handle UserList destined messages
            //
            // Forward UserList actor commands so that the User list actor can do something about it.
            Receive<UserListCommand>(c => { Context.Child(_UserListPersistenceId).Forward(c); });

            //
            // Handle child User Actor destined messages.
            //
            Receive<UserDeleteCommand>(c => {
                if (Context.Child(c.Id) == ActorRefs.Nobody)
                {
                    Sender.Tell(new UserFailedDeleteEvent($"User {c.Id} does not exist or has been deleted.", c.Id, c.User, c.ConnectionId));
                }
                else
                    Context.Child(c.Id).Forward(c);
            });
            Receive<UserUnDeleteCommand>(c => {
                if (Context.Child(c.Id) == ActorRefs.Nobody)
                {
                    Sender.Tell(new UserFailedUnDeleteEvent($"User {c.Id} does not exist.", c.Id, c.User, c.ConnectionId));
                }
                else
                    Context.Child(c.Id).Forward(c);
            });
            Receive<UserUpdateCommand>(c => {
                if (Context.Child(c.Id) == ActorRefs.Nobody)
                {
                    Sender.Tell(new UserFailedDeleteEvent($"User {c.UserStateData.UserName} does not exist or has been deleted.", c.UserStateData, c.User, c.ConnectionId));
                }
                else
                    Context.Child(c.Id).Forward(c);
            });
            Receive<UserInsertCommand>(c => {
                if (c.UserStateData.UserName == null || c.UserStateData.UserName == "")
                {
                    Sender.Tell(new UserFailedInsertEvent("UserName field cannot be blank(or null).", c.UserStateData, c.User, c.ConnectionId));
                    _logger.Error($"While inserting user, Username is missing.");
                }

                if (c.Id == null || c.Id == "")
                {
                    string id = Guid.NewGuid().ToString();
                    var newActor = InstantiateUserActor(id);
                    newActor.Forward(c);

                }
                else
                {
                    Sender.Tell(new UserFailedInsertEvent("Id field must be blank(or null) during insert.", c.UserStateData, c.User, c.ConnectionId));
                    _logger.Error($"While inserting user, Id not blank.");
                }
                Context.Child(c.Id).Forward(c);

            });
            Receive<UserUpsertCommand>(c => {
                Context.Child(c.Id).Forward(c);
            });

            //
            // Handle User Admin Messages
            //

            // Provide to the requestor the list of children actor refs
            Receive<UserGetChildActorRefs>(r => {
                r.Requestor.Tell(new UserGetChildActorRefsResponse(r.Requestor, Context.GetChildren().ToImmutableArray(), r));
            });

            // String command handler
            Receive<string>(s => HandleStringCommand(s));

            // This catch all will log if there are any weird unhandled messages.
            Receive<object>(message =>
            {
                _logger.Debug($"In \"Ready\" state, ignoring message from:{Sender.Path.ToStringWithAddress()} Unhandled Message of type:{message.GetType().Name}." );
            });

            _logger.Debug($"Processing deffered messages." );
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
        #endregion Event Handlers

        #region RequestHandlers



        #endregion RequestHandlers

        #region Helper Methods
        /// <summary>
        /// This helper function instantiates the User child actors
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private void InstantiateUserChildActors(UserIdGetListResponse r)
        {
            foreach (string ActorPersistenceId in r.ListOfUserIds)
            {
                InstantiateUserActor(ActorPersistenceId);
            }
        }

        private IActorRef InstantiateUserActor(string ActorPersistenceId)
        {
            _logger.Info($"Instantiating User actor id:'{ActorPersistenceId}'.");
            //Instantiate the User List Actor
            IActorRef userActor = Context.ActorOf(Props.Create<UserActor>(), ActorPersistenceId); // Continue here change the User actor to use the name in the path as the UserList
            _logger.Info($"Instantiated User actor id:'{ActorPersistenceId}'.");

            // Make sure the actor that maintains the state for all Users is kept up-to-date as the User's state mutate
            userActor.Tell(new SubscribeForCommandEvents(Context.Child(_UserStateAccumulator), MicroServices.Area.User));
            // also subscribe the UserList actor since we need to maintain a list of actors across restarts
            userActor.Tell(new SubscribeForCommandEvents(Context.Child(_UserListPersistenceId), MicroServices.Area.User));

            return userActor;
        }

        private void AttemptSupervisorRegistrationHelper()
        {
            // Register this supervisor with the SupervisorRegister so that the Bridge(s) can find it
            RegisterSupervisor registration = new RegisterSupervisor(Self, _ActorType, General.MicroServices.Area.User);
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

}
