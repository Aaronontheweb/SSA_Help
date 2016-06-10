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
using System.Collections.Immutable;

namespace EY.SSA.CommonBusinessLogic.Actors
{

    /// <summary>
    /// This actor maintains a list of Users and child actors representing each User.
    /// </summary>
    public class UserListActor : ReceivePersistentActor, IWithUnboundedStash
    {
        #region Fields

        private ILoggingAdapter _logger = Context.GetLogger();

        // This line of code sets the persistence Id for this actor.
        public override string PersistenceId { get; } = /*Context.Parent.Path.UserName + "/" +*/ Context.Self.Path.Name;

        private int _SnapshotTriggerCount = 5; // 5 commands triggers a snapshot

        private int _InactivityFlushSec = 2592000; // InactivityFlush 2592000 = 30 days

        private static string _ActorType = typeof(UserListActor).Name;

        private int _CommandActivityCounter;

        // _ActorState holds the current state of this actor.
        UserListState _ActorState;

        // This dictionary holds all the recovery command handlers that this actor will process
        Dictionary<string, Action<Command>> UseRecoveryCommandHandler;

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> _EventSubscribers = new HashSet<IActorRef>();


        #endregion Fields

        #region properties

        public static string ActorType
        {
            get { return _ActorType; }
        }

        #endregion properties

        /// <summary>
        /// Initializes the Persistence Actor
        /// </summary>
        /// <param name="listPersistenceId">Required.Unique application wide id.</param>
        /// <param name="SnapshotTriggerCount">Number of events that will trigger a snapshot. Default is 5 command events.</param>
        /// <param name="InactivityFlushSec">Flushes the actor out of memory.  Default is 30 days (25920000 seconds).</param>
        public UserListActor()
        {
            //Set up the command handlers
            UseRecoveryCommandHandler = new Dictionary<string, Action<Command>>();

            // Set up a state object
            _ActorState = new UserListState(PersistenceId, _ActorType);

            // Put actor in recovering state
            Recovering();
        }

        #region Actor States
        /// <summary>
        /// This method sets the recovering state.  It will remain active until the actor if fully recovered from the 
        /// Journal/Snashot stores.  It will switch to the CommandProcessing state once it receives the RecoveryCompleted message.
        /// </summary>
        private void Recovering()
        {
            _logger.Debug("Recovering with persistence id: {0}", PersistenceId);


            // ******** IMPORTANT ***********
            // For each command there should be a handler defined
            // ******************************
            UseRecoveryCommandHandler.Add("UserListInsertCommand", command => InsertNewUserListItemRecoveryCommand(command as UserListInsertCommand));
            UseRecoveryCommandHandler.Add("UserListUpdateCommand", command => UpdateUserListRecoveryCommand(command as UserListUpdateCommand));
            UseRecoveryCommandHandler.Add("UserListDeleteCommand", command => DeleteUserListRecoveryCommand(command as UserListDeleteCommand));
            UseRecoveryCommandHandler.Add("UserListUnDeleteCommand", command => UnDeleteUserListRecoveryCommand(command as UserListUnDeleteCommand));

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
            Command<SetSnapshotTriggerCount>(c => { _SnapshotTriggerCount = c.Value; });
            Command<SetInactivityFlushSec>(c => { _InactivityFlushSec = c.Value; });

            // Switch state to CommandProcessing to process other actor commands.
            Recover<RecoveryCompleted>(rc =>
            {
                _logger.Info("Recovery Complete.", Self.Path.ToStringWithAddress());
                Become(CommandProcessing);
                // Now that we are fully recovered let me process all the messages that came in.
                Stash.UnstashAll();

            });

            // Handle User List Requests
            Command<UserIdGetListRequest>(r=> {
                _logger.Debug($"Got {r.RequestType} during Recovery. Not ready. Stashing message from:{Sender.Path.ToStringWithAddress()}");
                Stash.Stash();
            });

            // This catch all will log if there are any weird unhandled messages.
            Recover<object>(message =>
            {
                Stash.Stash();
                _logger.Debug($"During Recovery stashing unhandled message from:{Sender.Path.ToStringWithAddress()} Got Unhandled Message:{message.GetType().Name}");
            });
        }

        /// <summary>
        /// The command processing state sets this actor ready to receive any command from other actors.
        /// </summary>
        private void CommandProcessing()
        {
            _logger.Info("Getting Ready.");
            // Commands
            Command<SaveSnapshotSuccess>(c => HandleSuccessfulSnapshotSave(c));
            Command<SaveSnapshotFailure>(c => HandleUnSuccessfulSnapshotSave(c));

            // Events
            Command<UserInsertedEvent>(e => HandleUserInsertedEvent(e));
            Command<UserDeletedEvent>(e => HandleUserDeletedEvent(e));
            Command<UserUnDeletedEvent>(e => HandleUserUnDeletedEvent(e));
            Command<UserUpdatedEvent>(e => HandleUserUpdatedEvent(e));
            Command<SubscribedForCommandEvents>(e => { _logger.Info($"Now listening to:{e.Id}"); });

            // Requests
            Command<UserGetStateRequest>(r => { Sender.Tell(new UserGetStateResponse(Sender, null, r)); });
            Command<UserIdGetListRequest>(r => HandleUserIdGetListRequest(r));

            // Configuration
            Command<SetSnapshotTriggerCount>(c => { Persist<SetSnapshotTriggerCount>(c, SetSnapshotTriggerConfigurationValue); });
            Command<SetInactivityFlushSec>(c => { Persist<SetInactivityFlushSec>(c, SetInactivityFlushSecConfigurationValue); });

            // Handle any string commands
            Command<string>(s => HandleStringCommand(s));

            // This catch all will log if there are any weird unhandled messages.
            Command<object>(message =>
            {
                _logger.Debug($"In Command State Received unhandled message from:{Sender.Path.ToStringWithAddress()} Unhandled Message:{message.GetType().Name}");
            });

            _logger.Debug("Ready.");
        }

        #endregion Actor States

        #region Handle Requests

        /// <summary>
        /// Replies back with a list of active User persistenceId's
        /// </summary>
        /// <param name="r"></param>
        private void HandleUserIdGetListRequest(UserIdGetListRequest r)
        {
            Sender.Tell(new UserIdGetListResponse(Sender, _ActorState.UserList.Keys.ToImmutableArray(), r));
        }



        #endregion Handle Requests

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
                            AutoSaveSnashot(true);
                            break;
                        }
                    case "PrintUserList":
                        {
                            LogUserList();
                            break;
                        }
                    case "ProvideUserList":
                        {
                            GenerateAndProvideCurrentUserList();
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

        private void GenerateAndProvideCurrentUserList()
        {
            Sender.Tell(_ActorState.UserList.ToImmutableDictionary<string, UserListItem>());
        }

        private void LogUserList()
        {
            IDictionary<string, UserListItem> Users = _ActorState.UserList;
            foreach(string UserId in Users.Keys)
            {
                _logger.Debug("UserId-UserName:{0}-{1}", UserId,Users[UserId].UserName);
            }
        }

        #endregion String Messages/Commands

        #region Delete Command/Event Handlers

        /// <summary>
        /// Removes User(item) from the list.  If an instance of the UserActor is in memory it unloads it.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private void HandleUserDeletedEvent(UserDeletedEvent e)
        {
            // Is the item still in the list?
            if (_ActorState[e.Id] == null)
            {
                _logger.Info($"{e.User} tried deleting from User list id:{e.Id} but was not found.");
            }
            else
            {
                UserListItem cliNewState = _ActorState[e.Id].Copy();
                cliNewState.IsActive = false; 

                // Memorialize that we are removing the item from the list in the journal
                UserListDeleteCommand UserListDeleteCommand = new UserListDeleteCommand(
                    cliNewState,
                    e.User,
                    e.ConnectionId);

                _logger.Info($"Deleting User list item Id{e.Id} UserName:{e.ResultUserState.UserName}.");
                Persist<UserListDeleteCommand>(UserListDeleteCommand, PostUserListDeleteHandler);

            }
        }

        private void PostUserListDeleteHandler(UserListDeleteCommand c)
        {
            _logger.Info($"Deleted User list item Id{c.Id} UserName:{c.UserListItemData.UserName}.");
            _ActorState[c.UserId] = c.UserListItemData;
            UserListItemDeletedEvent message = new UserListItemDeletedEvent(c.UserListItemData.Copy(), c.User, c.ConnectionId);
            NotifySubscribers(message);
            AutoSaveSnashot(false);

        }

        private void DeleteUserListRecoveryCommand(UserListDeleteCommand c)
        {
                _logger.Info($"Recovery Deleting User list item Id{c.Id} UserName:{c.UserListItemData.UserName}.");
                _ActorState[c.UserId] = c.UserListItemData;
                _logger.Info($"Recovery Deleted User list item Id{c.Id} UserName:{c.UserListItemData.UserName}.");
        }

        #endregion Delete Command/Event Handlers

        #region UnDelete Command/Event Handlers

        /// <summary>
        /// Removes User(item) from the list.  If an instance of the UserActor is in memory it unloads it.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private void HandleUserUnDeletedEvent(UserUnDeletedEvent e)
        {
            // Is the item still in the list?
            if (_ActorState[e.Id] == null)
            {
                _logger.Info($"{e.User} tried undeleting from User list id:{e.Id} but was not found.");
            }
            else
            {

                UserListItem cliNewState = _ActorState[e.Id].Copy();
                cliNewState.IsActive = true;

                // Memorialize that we are recovering the item from the list in the journal
                UserListUnDeleteCommand UserListUnDeleteCommand = new UserListUnDeleteCommand(
                    cliNewState,
                    e.User,
                    e.ConnectionId);

                _logger.Info($"Undeleting User list item Id{e.Id} UserName:{e.ResultUserState.UserName}.");
                Persist<UserListUnDeleteCommand>(UserListUnDeleteCommand, PostUserListUnDeleteHandler);
            }
        }

        private void PostUserListUnDeleteHandler(UserListUnDeleteCommand c)
        {
            _logger.Info($"UnDeleting from User list Id:{c.UserId} UserName:{c.UserListItemData.UserName}.");
            _ActorState[c.UserId] = c.UserListItemData;
            UserListItemUnDeletedEvent message = new UserListItemUnDeletedEvent(c.UserListItemData.Copy(), c.User, c.ConnectionId);
            NotifySubscribers(message);
            AutoSaveSnashot(false);
        }

        private void UnDeleteUserListRecoveryCommand(UserListUnDeleteCommand c)
        {
            _logger.Info($"Recovering Undeleting User list item Id:{c.Id} UserName:{c.UserListItemData?.UserName??"No UserName"}.");
            _ActorState[c.UserId] = c.UserListItemData;
            _logger.Info($"Recovered Undeleted from User list Id:{c.Id} UserName:{c.UserListItemData?.UserName ?? "No UserName"}.");
        }

        #endregion UnDelete Command/Event Handlers

        #region Update Command/Event Handlers

        private void HandleUserUpdatedEvent(UserUpdatedEvent e)
        {
            UserState cs = e.ResultUserState;
            UserListItem newUserListItem = UserListItem.GenerateUserListItemFromUserState(cs);

            UserListUpdateCommand newCLUC = new UserListUpdateCommand(newUserListItem,e.User,e.ConnectionId);
            _logger.Info($"Updating User list Id:{cs.Id} UserName:{cs.UserName}.");
            Persist<UserListUpdateCommand>(newCLUC, postUserListItemUpdateHandler);
        }

        private void postUserListItemUpdateHandler(UserListUpdateCommand c)
        {
            _ActorState[c.Id] = c.UserListItemData;
            _logger.Info($"Updated User list item Id{c.Id} UserName:{c.UserListItemData.UserName}.");
            UserListUpdatedEvent message = new UserListUpdatedEvent(c.UserListItemData.Copy(),c.User,c.ConnectionId);
            NotifySubscribers(message);
            AutoSaveSnashot(false);

        }

        private void UpdateUserListRecoveryCommand(UserListUpdateCommand c)
        {
            _logger.Info($"Recovering update User list item Id:{c.Id} UserName:{c.UserListItemData.UserName}.");
            _ActorState[c.Id] = c.UserListItemData;
            _logger.Info($"Recovered update User list item Id:{c.Id} UserName:{c.UserListItemData.UserName}.");
        }

        #endregion Update Command/Event Handlers

        #region Insert Command/Event Handlers


        /// <summary>
        /// This method handles User insert events by updating the Admin's User list to the latest information.
        /// </summary>
        /// <param name="e">UserInserted event.</param>
        private void HandleUserInsertedEvent(UserInsertedEvent e)
        {
            if(_ActorState[e.Id] == null)
            {
                UserState cs = e.ResultUserState;
                UserListItem newUserListItem = UserListItem.GenerateUserListItemFromUserState(cs);

                // It does not matter if the item exists or not we will stomp for that particular User the existing data
                // with the new data - see post insert handler.
                // Persist the event that a new User was added to the User list
                UserListInsertCommand newCLIC = new UserListInsertCommand(newUserListItem,e.User,e.ConnectionId);
                _logger.Info($"Inserting new User list item Id:{cs.Id} UserName:{cs.UserName}.");
                Persist<UserListInsertCommand>(newCLIC, postUserListItemInsertHandler);
            }
        }

        /// <summary>
        /// Helper method to update the state of the Admin Actor once the event has be journaled.
        /// </summary>
        /// <param name="c"></param>
        private void postUserListItemInsertHandler(UserListInsertCommand c)
        {
            _ActorState[c.Id] = c.UserListItemData;
            _logger.Info($"Inserting new User list item Id:{c.Id} UserName:{c.UserListItemData.UserName}.");
            UserListInsertedEvent message = new UserListInsertedEvent(c.UserListItemData.Copy(),c.User,c.ConnectionId);
            NotifySubscribers(message);
            AutoSaveSnashot(false);
        }

        /// <summary>
        /// This recovery command handler will instantiate the child UserActor and add/update the UserItem to the UserAdminActor's User list.  It will also maintain a reference to the child UserActor.
        /// </summary>
        /// <param name="c"></param>
        private void InsertNewUserListItemRecoveryCommand(UserListInsertCommand c)
        {
            _logger.Info($"Recovering Insert new User list item Id:{c.Id} UserName:{c.UserListItemData.UserName}.");
            _ActorState[c.Id] = c.UserListItemData;
            _logger.Info($"Recovered Insert new User list item Id:{c.Id} UserName:{c.UserListItemData.UserName}.");
        }
        /// <summary>
        /// This command handler attempts to instantiate the child UserActor and add it to the Admin's UserItem list. 
        /// </summary>
        /// <param name="cLIC">User list item command. Contains the UserItem</param>
        /// <returns>True if ok. False otherwise.</returns>

        #endregion Insert Command

        #region Helper Methods
        #endregion Helper Methods

        #region Snapshots
        private void ProcessSnapshot(SnapshotOffer snapshotOffer)
        {
            _logger.Debug($"Recovering snapshot for ActorId:{_ActorState.Id} UserName:{_ActorState.Name}");

            try
            {
                Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)snapshotOffer.Snapshot;
                var temp = jo.ToObject<UserListState>();
                foreach(KeyValuePair<string, Object> o in temp.Items)
                {
                    JObject itemJO = o.Value as JObject;
                    _ActorState[o.Key] = itemJO.ToObject<UserListItem>();

                }
            }
            catch (Exception e)
            {
                _logger.Debug($"Recovering snapshot failed for ActorId:{_ActorState.Id} UserName:{_ActorState?.Name??"No name"} exception:{e.Message}");
            }
        }

        private void AutoSaveSnashot(bool forceSave)
        {
            _CommandActivityCounter++;
            if (_CommandActivityCounter % _SnapshotTriggerCount == 0 || forceSave)
            {
                string prePhrase = forceSave ? "Forced" : "Auto";
                SaveSnapshot(_ActorState);
                _logger.Debug($"Attempting {prePhrase} save snapshot actor Id:{_ActorState.Id} UserName:{_ActorState.Name}");
            }
        }

        private bool HandleSuccessfulSnapshotSave(SaveSnapshotSuccess c)
        {
            _logger.Debug($"Saved snapshot for ActorId:{_ActorState.Id} UserName:{_ActorState.Name}");
            return true;
        }

        private bool HandleUnSuccessfulSnapshotSave(SaveSnapshotFailure c)
        {
            _logger.Debug($"Save failed for snapshot for ActorId:{_ActorState.Id} UserName:{_ActorState.Name}");
            return true;
        }


        #endregion Snapshots

        #region Subscribers
        private void HandleSubscriptionRequest(SubscribeForCommandEvents subscriptionRequest)
        {
            _logger.Info($"Subscribed {subscriptionRequest.Requestor.Path.Name} for {PersistenceId} command events.");
            _EventSubscribers.Add(subscriptionRequest.Requestor);
        }
        private void HandleUnSubscribeRequest(UnSubscribeForCommandEvents subscriptionRequest)
        {
            _logger.Info($"Unsubscribed {subscriptionRequest.Requestor.Path.Name} for {PersistenceId} command events.");
            _EventSubscribers.Remove(subscriptionRequest.Requestor);
        }

        private void NotifySubscribers(CommandEventMessage sc)
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
