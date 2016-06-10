using Akka.Actor;
using Akka.Event;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.Messages.Response;
using System;
using System.Collections.Generic;
using EY.SSA.CommonBusinessLogic.State;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Events;

namespace EY.SSA.CommonBusinessLogic.Actors
{
    /// <summary>
    /// This actor keeps an User State
    /// </summary>
    public class UserActor : ReceiveActor, IWithUnboundedStash
    {
        #region fields

        private Akka.Event.ILoggingAdapter _logger = Context.GetLogger();

        private static string _ActorType = typeof(UserSupervisor).Name;

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> ActivitySubscribers = new HashSet<IActorRef>();


        // User's unique id == user's persistence Id
        private string _Id { get; } = Context.Self.Path.Name;

        // Prefix attached to _Id to make companion persistence actor's name
        private string _PersistanceActorName = $"{MicroServices.Area.User.ToString()}_Persistence_{Context.Self.Path.Name}";

        private UserState _ActorState;

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> _EventSubscribers = new HashSet<IActorRef>();
        
        #endregion fields

        #region properties

        public static string ActorType
        {
            get { return _ActorType; }
        }

        public IStash Stash { get; set; }

        #endregion properties

        public UserActor()
        {
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

            // Instantiate the UserPersistence Actor responsible for all the dangerous database operations
            _logger.Info($"Instantiating User's persistance layer actor id:'{_PersistanceActorName}'.");
            //Instantiate the User List Actor
            IActorRef UserActor = Context.ActorOf(Props.Create<UserPersistenceActor>(), $"{_PersistanceActorName}");
            _logger.Info($"Instantiated User's persistance layer actor id:'{_PersistanceActorName}'.");

            //Subscribe to all events
            Context.Child(_PersistanceActorName).Tell(new SubscribedForCommandEvents(_ActorType,_Id,Self));


            // Request the last recorded user state
            Context.Child(_PersistanceActorName).Tell(new UserGetStateRequest(Self));

            // Once the user State has been restored become ready for regular operations
            Receive<UserGetStateResponse>(r => {
                _ActorState = r.ReplyUserState;
                Become(Ready);
            });

            // Stay in Initializing state until the persistence layer reports back with a new state.

            // This catch all will log if there are any weird unhandled messages.
            Receive<object>(message =>
            {
                Stash.Stash();
                _logger.Debug($"Stashing unhandled message from:{Sender.Path.ToStringWithAddress()} Got Unhandled Message:{message.GetType().Name}");
            });

            _logger.Debug("Initialized.");

        }

        /// <summary>
        /// The command processing state sets this actor ready to receive any command from other actors.
        /// </summary>
        private void Ready()
        {
            _logger.Debug($"{_ActorType} getting Ready.");

            //
            // Handle Delete
            //

            // Forward User Child actor commands so that the persistence layer can do something about it and
            // handle the resulting events when they come back.
            Receive<UserDeleteCommand>(c => {
                if (_ActorState.isActive == false)
                {
                    var message = new UserFailedDeleteEvent("User has been already deleted.", c.Id, c.User, c.ConnectionId);
                    Sender.Tell(message, Self);
                }
                else
                {
                    Context.Child(_PersistanceActorName).Forward(c);
                }

            });

            // If delete event was recorded then notify the orignal sender.
            Receive<UserDeleteRecordedEvent>(e => {

                _ActorState.isActive = false;
                _logger.Debug($"User:{e.User} deleted User id:{_ActorState.Id}.");

                //Message everyone else who is interested
                UserDeletedEvent message = new UserDeletedEvent(_ActorState.Clone(), e.User, e.ConnectionId);
                e.Sender.Tell(message, Self);
                NotifyCommandEventSubscribers(message);

            });

            //
            // Handle UnDelete
            //
            Receive<UserUnDeleteCommand>(c => {
                if (_ActorState.isActive == true)
                {
                    var message = new UserFailedUnDeleteEvent("User is already active.", c.Id, c.User, c.ConnectionId);
                    Sender.Tell(message, Self);
                }
                else
                {
                    // Journal the fact that the User was deleted
                    Context.Child(_PersistanceActorName).Forward(c);
                }

            });
            Receive<UserUnDeleteRecordedEvent>(e =>
            {
                _logger.Debug($"User:{e.User} undeleted User id:{_ActorState.Id}.");

                UserUnDeletedEvent message = new UserUnDeletedEvent(_ActorState.Clone(), e.User, e.ConnectionId);
                e.Sender.Tell(message, Self);

                NotifyCommandEventSubscribers(message);

            });

            //
            // Handle Update
            //
            Receive<UserUpdateCommand>(c => {

                if (_ActorState.isActive == false)
                {
                    UserFailedUpdateEvent msg = new UserFailedUpdateEvent(string.Format("User is deleted(inactive)."), c.UserStateData, c.User, c.ConnectionId);
                    Sender.Tell(msg);
                    return;
                }

                // Enforce required fields
                if (c.UserStateData.UserName == null || c.UserStateData.UserName == "")
                {
                    UserFailedUpdateEvent msg = new UserFailedUpdateEvent(string.Format("UserName field cannot be blank(or null)."), c.UserStateData, c.User, c.ConnectionId);
                    Sender.Tell(msg);
                    return;
                }

                Context.Child(_PersistanceActorName).Forward(c);
            });
            Receive<UserUpdateRecordedEvent>(e => {
                _logger.Debug($"User:{e.User} updated User id:{_ActorState.Id} state.");

                UserUpdatedEvent message = new UserUpdatedEvent(_ActorState.Clone(), e.User, e.ConnectionId);
                e.Sender.Tell(message, Self);

                NotifyCommandEventSubscribers(message);

            });

            //
            // Handle Insert
            //
            Receive<UserInsertCommand>(c => {
                if (c.UserStateData.UserName == null || c.UserStateData.UserName == "")
                {
                    Sender.Tell(new UserFailedInsertEvent("UserName field cannot be blank(or null).", c.UserStateData, c.User, c.ConnectionId));
                    _logger.Error($"While inserting Id: {_Id} User name is missing.");
                }
                if (c.UserStateData.Id == null || c.UserStateData.Id == "")
                {
                    Context.Child(_PersistanceActorName).Forward(c);
                }

                _ActorState = new UserState(_Id, c.UserStateData);
                _logger.Info($"Inserted {_ActorState.DocumentType} for id:{_Id}");

            });
            Receive<UserInsertRecordedEvent>(e => {
                _logger.Debug($"User:{e.User} inserted User id:{_ActorState.Id}.");

                UserInsertedEvent message = new UserInsertedEvent(_ActorState.Clone(), e.User, e.ConnectionId);
                e.Sender.Tell(message, Self);

                NotifyCommandEventSubscribers(message);
            });

            //
            // Handle Upsert
            //
            Receive<UserUpsertCommand>(c => {
                if (_ActorState.isActive == false && c.UserStateData.isActive == true)
                {
                    _logger.Error($"Reactivating User with Id: {_Id}");
                    return;
                }
                if (c.UserStateData.UserName == null || c.UserStateData.UserName == "")
                {
                    Sender.Tell(new UserFailedInsertEvent("UserName field cannot be blank(or null).", c.UserStateData, c.User, c.ConnectionId));
                    _logger.Error($"While insert PersistenceId: {_Id} User name is missing.");
                    return;
                }
                if (c.UserStateData.Id == null || c.UserStateData.Id == "")
                {
                    Sender.Tell(new UserFailedInsertEvent("User Id cannot be empty.", c.UserStateData, c.User, c.ConnectionId));
                    _logger.Error($"While insert PersistenceId: {_Id} User id is missing.");
                    return;
                }

                Context.Child(_PersistanceActorName).Forward(c);
            });
            Receive<UserUpsertRecordedEvent>(e => {
                _logger.Debug($"User:{e.User} upserted User id:{_ActorState.Id}.");

                UserUpsertedEvent message = new UserUpsertedEvent(_ActorState.Clone(), e.User, e.ConnectionId);
                e.Sender.Tell(message, Self);

                NotifyCommandEventSubscribers(message);
            });

            //
            // Command catch all
            //
            Receive<CommandEventMessage>(e => HandleUnplannedCommandEventMessage(e));

            //
            // String command handler
            //
            Receive<string>(s => HandleStringCommand(s));

            //
            // This catch all will log if there are any weird unhandled messages.
            //
            Receive<object>(message =>
            {
                _logger.Debug($"Ignoring message from:{Sender.Path.ToStringWithAddress()} Got Unhandled Message:{message.GetType().Name}");
            });

            Stash?.UnstashAll();

            _logger.Debug($"{_ActorType} ready.");
        }

        #endregion Actor States

        #region String Messages/Commands
        private void HandleStringCommand(string s)
        {
            if (s != null)
            {

                switch (s)
                {
                    case "PrintUserState":
                        {
                            //Todo define the ability to do this.
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

        #region EventHandlers
        private bool HandleUnplannedCommandEventMessage(CommandEventMessage e)
        {
            _logger.Debug($"User:{e.User} requested {e.CommandType} but was unhandled by User id:{_ActorState.Id} state.");

            //If a User was inserted then we need to subscribe to the events
            return true;
        }
        #endregion EventHandlers

        #region RequestHandlers
        #endregion RequestHandlers

        #region Helper Methods
        #endregion Helper Methods

        #region Subscribers
        private void HandleCommandEventsSubscriptionRequest(SubscribeForCommandEvents subscriptionRequest)
        {
            _logger.Info($"Subscribed {subscriptionRequest.Requestor.Path.Name} for {_Id} command events.");
            _EventSubscribers.Add(subscriptionRequest.Requestor);
            subscriptionRequest.Requestor.Tell(new SubscribedForCommandEvents(_ActorType, _Id, Self));
        }
        private void HandleCommandEventsUnSubscribeRequest(UnSubscribeForCommandEvents subscriptionRequest)
        {
            _logger.Info($"Unsubscribed {subscriptionRequest.Requestor.Path.Name} for {_Id} command events.");
            _EventSubscribers.Remove(subscriptionRequest.Requestor);
        }

        private void NotifyCommandEventSubscribers(CommandEventMessage sc)
        {
            foreach (IActorRef subscriber in _EventSubscribers)
            {
                subscriber.Tell(sc);
            }
        }


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