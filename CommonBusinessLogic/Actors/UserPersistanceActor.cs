using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Configuration;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.Messages.Response;
using EY.SSA.CommonBusinessLogic.State;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace EY.SSA.CommonBusinessLogic.Actors
{
    public class UserPersistenceActor:ReceivePersistentActor
    {
        #region fields

        private ILoggingAdapter _logger = Context.GetLogger();

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> _EventSubscribers = new HashSet<IActorRef>();

        // This dictionary holds all the recovery command handlers that this actor will process
        Dictionary<string, Action<Command>> UseRecoveryCommandHandler;

        // _ActorState holds the current state of this actor.
        UserState _ActorState;

        private int _SnapshotTriggerCount = 5; // 5 commands triggers a snapshot

        private int _InactivityFlushSec = 2592000; // InactivityFlush 2592000 = 30 days

        private static string _ActorType = typeof(UserPersistenceActor).Name;

        private int _CommandActivityCounter;

        #endregion fields

        #region properties

        // This line of code sets the persistence Id for this actor.
        public override string PersistenceId { get; } = /*Context.Parent.Path.UserName + "/" + */Context.Parent.Path.Name;

        public static string ActorType
        {
            get { return _ActorType; }
        }

        #endregion properties

        /// <summary>
        /// Initializes the Persistence Actor
        /// </summary>
        /// <param name="UserPersistenceId">Required.Unique application wide id.</param>
        /// <param name="SnapshotTriggerCount">Number of events that will trigger a snapshot. Default is 5 command events.</param>
        /// <param name="InactivityFlushSec">Flushes the actor out of memory.  Default is 30 days (25920000 seconds).</param>
        public UserPersistenceActor() 
        {
            //Set up the command handlers
            UseRecoveryCommandHandler = new Dictionary<string, Action<Command>>();

            // Set up a state object
            _ActorState = new UserState(PersistenceId);


            // Put actor in recovering state
            Become(Recovering);
        }

        #region Actor States
        /// <summary>
        /// This method sets the recovering state.  It will remain active until the actor if fully recovered from the 
        /// Journal/Snashot stores.  It will switch to the CommandProcessing state once it receives the RecoveryCompleted message.
        /// </summary>
        private void Recovering()
        {
            _logger.Debug($"Recovering id:{PersistenceId}", PersistenceId);

            // ******** IMPORTANT ***********
            // For each command there should be a handler defined
            // ******************************
            UseRecoveryCommandHandler.Add("UserInsertCommand", command => InsertNewUserRecoveryCommand(command as UserInsertCommand));
            UseRecoveryCommandHandler.Add("UserUpsertCommand", command => UpsertNewUserRecoveryCommand(command as UserUpsertCommand));
            UseRecoveryCommandHandler.Add("UserUpdateCommand", command => UpdateUserRecoveryCommand(command as UserUpdateCommand));
            UseRecoveryCommandHandler.Add("UserDeleteCommand", command => DeleteUserRecoveryCommand(command as UserDeleteCommand));
            UseRecoveryCommandHandler.Add("UserUnDeleteCommand", command => UnDeleteUserRecoveryCommand(command as UserUnDeleteCommand));

            // Process any snapshots recovered from the data store
            Recover<SnapshotOffer>(snapshotOffer => ProcessSnapshot(snapshotOffer));

            // These will be commands restored from the data store.
            Recover<JObject>(jo =>
            {
                //Console.WriteLine("Received object: " + jo);
                try
                {
                    string commandName = jo["CommandClass"].Value<string>();
                    Type commandType = Type.GetType(typeof(Command).Namespace + "." + commandName);
                    Command cmd = jo.ToObject(commandType) as Command;
                    UseRecoveryCommandHandler[commandName](cmd);
                }
                catch (Exception e)
                {
                    _logger.Error("Error:{0}\nFailed to process journal entry:{1}", e.Message, jo);
                }
            });

            // Restores Configuration
            Command<SetSnapshotTriggerCount>(c=>{_SnapshotTriggerCount = c.Value;});
            Command<SetInactivityFlushSec>(c=>{_InactivityFlushSec = c.Value;});


            // Switch state to CommandProcessing to process other actor commands.
            Recover<RecoveryCompleted>(rc =>
            {

                _logger.Info($"Recovery Complete for Id:{_ActorState.Id} UserName:{_ActorState?.UserName??"Not Initialized"}", Self.Path.ToStringWithAddress());
                Become(CommandProcessing);
            });

            // This catch all will log if there are any weird unhandled messages.
            Recover<object>(message =>
            {
                // While recovering stash all messages for later.
                Stash.Stash();
                _logger.Debug($"During Recovery stashing unhandled message from:{Sender.Path.ToStringWithAddress()} Got Unhandled Message:{message.GetType().Name}");
            });
        }

        /// <summary>
        /// The command processing state sets this actor ready to receive any command from other actors.
        /// </summary>
        private void CommandProcessing()
        {
            _logger.Info($"{PersistenceId} Getting Ready.");

            // Commands
            Command<UserInsertCommand>(c => {

                _ActorState = new UserState(PersistenceId, c.UserStateData);

                AutoSaveSnapshot(true);

                _logger.Debug($"User's :{c.User} insert command recorded for User id:{_ActorState.Id}.");

                NotifyCommandEventSubscribers(new UserInsertRecordedEvent(Sender,c,c.User,c.ConnectionId));

            });

            Command<UserUpsertCommand>(c => {Persist<UserUpsertCommand>(c, PostUpsertHandler);});

            Command<UserUpdateCommand>(c => {Persist<UserUpdateCommand>(c, PostUpdateHandler);});

            Command<UserDeleteCommand>(c => {Persist<UserDeleteCommand>(c, PostDeleteHandler);});

            Command<UserUnDeleteCommand>(c => {Persist<UserUnDeleteCommand>(c, PostUnDeleteHandler);});
            
            //Persistence layer messages
            Command<SaveSnapshotSuccess>(c => HandleSuccessfulSnapshotSave(c));
            Command<SaveSnapshotFailure>(c => HandleUnSuccessfulSnapshotSave(c));

            // Requests
            Command<UserGetStateRequest>(r => {
                Sender.Tell(new UserGetStateResponse(Sender, _ActorState.Clone(), r));
            });
            Command<SubscribeForCommandEvents>(r => HandleCommandEventsSubscriptionRequest(r));
            Command<UnSubscribeForCommandEvents>(r => HandleCommandEventsUnSubscribeRequest(r));

            // Configuration
            Command<SetSnapshotTriggerCount>(c=>{Persist<SetSnapshotTriggerCount>(c,SetSnapshotTriggerConfigurationValue);});
            Command<SetInactivityFlushSec>(c=>{Persist<SetInactivityFlushSec>(c,SetInactivityFlushSecConfigurationValue);});

            // General String messages
            Command<string>(s=> HandleStringCommand(s));

            // This catch all will log if there are any weird unhandled messages.
            Command<object>(message =>
            {
                _logger.Debug($"In \"Command\" state ignoring message from:{Sender.Path.ToStringWithAddress()} Unhandled Message:{message.GetType().Name}");

            });

            // Pull all the messages that where not handled during recovery.
            Stash.UnstashAll();

            _logger.Info($"{PersistenceId} Ready.");

        }


        #endregion Actor States

        #region Configuration Handlers

        private void SetSnapshotTriggerConfigurationValue(SetSnapshotTriggerCount obj)
        {
 	        _SnapshotTriggerCount = obj.Value;
        }
        private void SetInactivityFlushSecConfigurationValue(SetInactivityFlushSec obj)
        {
 	        _InactivityFlushSec = obj.Value;
        }

        #endregion

        #region String Messages/Commands
        private void HandleStringCommand(string s)
        {
            if (s != null)
            {
                switch (s)
                {
                    case "ForceSnapshot":
                        {
                            AutoSaveSnapshot(true);
                            break;
                        }
                    default:
                        {
                            _logger.Debug("{2} got unhandled string message from:{0} Unhandled Message:{1}", Sender.Path.ToStringWithAddress(), s, _ActorType);
                            break;
                        }

                }
            }
        }
        #endregion String Messages/Commands
        
        #region Delete Command

        private bool DeleteUserRecoveryCommand(UserDeleteCommand c)
        {
            _ActorState.isActive = false;
            return true;
        }

        private void PostDeleteHandler(UserDeleteCommand c)
        {
            // Deleting a User is not permanently removing them from the datastore but rather a simple state change to inactive.
            _ActorState.isActive = false;

            // Once a User has been marked as inactive we want to save the state so that future incarnations of the actor will
            // be in a inactive state.
            AutoSaveSnapshot(true);

            _logger.Debug($"User:{c.User} delete command recorded for User id:{_ActorState.Id}.");

            NotifyCommandEventSubscribers(new UserDeleteRecordedEvent(Sender,c, c.User, c.ConnectionId));
        }

        #endregion Delete Command

        #region UnDelete Command

        private bool UnDeleteUserRecoveryCommand(UserUnDeleteCommand c)
        {
            _ActorState.isActive = true;
            return true;
        }

        private void PostUnDeleteHandler(UserUnDeleteCommand c)
        {
            _ActorState.isActive = true;

            // Once a User has been marked as active we want to save the state so that future incarnations of the actor will
            // be in a active state.
            AutoSaveSnapshot(true);

            _logger.Debug($"User:{c.User} un-delete command recorded for User id:{_ActorState.Id}.");

            NotifyCommandEventSubscribers(new UserUnDeleteRecordedEvent(Sender, c, c.User, c.ConnectionId));
        }

        #endregion Delete Command

        #region Update Command

        private void PostUpdateHandler(UserUpdateCommand c)
        {
            // Update updatable fields
            _ActorState.Update(c.UserStateData);

            AutoSaveSnapshot(false);
            
            _logger.Debug($"User's :{c.User} update command recorded for User id:{_ActorState.Id}.");
            
            // Notify interested actors on the update
            NotifyCommandEventSubscribers(new UserUpdateRecordedEvent(Sender,c,c.User,c.ConnectionId));
        }

        private bool UpdateUserRecoveryCommand(UserUpdateCommand c)
        {
            // Update updatable fields
            _ActorState.Update(c.UserStateData);
            return true;
        }

        #endregion Update Command

        #region Insert Command

        private void InsertNewUserRecoveryCommand(UserInsertCommand c)
        {
            // When recovering set the state of the actor
            _ActorState = c.UserStateData;
        }

        #endregion Insert Command

        #region Upsert Command 

        private void PostUpsertHandler(UserUpsertCommand c)
        {
            _ActorState = new UserState(PersistenceId, c.UserStateData);
            AutoSaveSnapshot(false);
            _logger.Info($"Updated/Inserted event recorded {_ActorState.DocumentType} for id:{_ActorState.Id}");
            NotifyCommandEventSubscribers(new UserUpsertRecordedEvent(Sender,c,c.User,c.ConnectionId));
        }

        private void UpsertNewUserRecoveryCommand(UserUpsertCommand c)
        {
            // When recovering set the state of the actor
            _ActorState = c.UserStateData;
        }
        
        #endregion Upsert Command

        #region Snapshots
        private void ProcessSnapshot(SnapshotOffer snapshotOffer)
        {
            try
            {
                Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)snapshotOffer.Snapshot;
                _ActorState = jo.ToObject<UserState>();
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to parse JSON snapshot for id:{PersistenceId} - exception message:{e.Message}");
            }
        }

        private void AutoSaveSnapshot(bool forceSave)
        {
            _CommandActivityCounter++;
            if (_CommandActivityCounter % _SnapshotTriggerCount == 0 || forceSave)
            {
                string prePhrase = forceSave?"Force":"Auto";
                SaveSnapshot(_ActorState);
                _logger.Debug("Attempting {0} save snapshot actor Type-ActorId -UserName:{1}-{2}-{3}",prePhrase, _ActorType,_ActorState.Id, _ActorState.UserName);
            }
        }

        private bool HandleSuccessfulSnapshotSave(SaveSnapshotSuccess c)
        {
            _logger.Debug("Successfully saved snapshot for {0} actor ActorId -UserName:{1}-{2}", _ActorType, _ActorState.Id, _ActorState.UserName);
            return true;
        }

        private bool HandleUnSuccessfulSnapshotSave(SaveSnapshotFailure c)
        {
            _logger.Debug("Unsuccessfully saved snapshot for {0} actor ActorId -UserName:{1}-{2}", _ActorType, _ActorState.Id, _ActorState.UserName);
            return true;
        }


        #endregion Snapshots

        #region Subscribers
        private void HandleCommandEventsSubscriptionRequest(SubscribeForCommandEvents subscriptionRequest)
        {
            _logger.Info($"Subscribed {subscriptionRequest.Requestor.Path.Name} for {PersistenceId} command events.");
            _EventSubscribers.Add(subscriptionRequest.Requestor);
            subscriptionRequest.Requestor.Tell(new SubscribedForCommandEvents(_ActorType,PersistenceId,Self));
        }
        private void HandleCommandEventsUnSubscribeRequest(UnSubscribeForCommandEvents subscriptionRequest)
        {
            _logger.Info($"Unsubscribed {subscriptionRequest.Requestor.Path.Name} for {PersistenceId} command events.");
            _EventSubscribers.Remove(subscriptionRequest.Requestor);
        }

        private void NotifyCommandEventSubscribers(CommandEventMessage sc )
        {
            foreach(IActorRef subscriber in _EventSubscribers)
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
