using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Akka.Persistence;
using Akka.Logger;
using Akka.Actor;
using Akka.Event;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.State;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.Messages.Configuration;
using EY.SSA.CommonBusinessLogic.Messages.Response;

namespace EY.SSA.CommonBusinessLogic.Actors
{
    public class ClientActor:ReceivePersistentActor
    {
        #region fields

        private ILoggingAdapter _logger = Context.GetLogger();

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> _EventSubscribers = new HashSet<IActorRef>();

        // This dictionary holds all the recovery command handlers that this actor will process
        Dictionary<string, Action<Command>> UseRecoveryCommandHandler;

        // _ActorState holds the current state of this actor.
        ClientState _ActorState;

        private int _SnapshotTriggerCount = 5; // 5 commands triggers a snapshot

        private int _InactivityFlushSec = 2592000; // InactivityFlush 2592000 = 30 days

        private static string _ActorType = typeof(ClientActor).Name;

        private int _CommandActivityCounter;

        #endregion fields

        #region properties

        // This line of code sets the persistence Id for this actor.
        public override string PersistenceId { get; } = /*Context.Parent.Path.UserName + "/" + */Context.Self.Path.Name;

        public static string ActorType
        {
            get { return _ActorType; }
        }

        #endregion properties

        /// <summary>
        /// Initializes the Persistence Actor
        /// </summary>
        /// <param name="clientPersistenceId">Required.Unique application wide id.</param>
        /// <param name="SnapshotTriggerCount">Number of events that will trigger a snapshot. Default is 5 command events.</param>
        /// <param name="InactivityFlushSec">Flushes the actor out of memory.  Default is 30 days (25920000 seconds).</param>
        public ClientActor() 
        {
            //Set the PersistenceId for this client

            //Set up the command handlers
            UseRecoveryCommandHandler = new Dictionary<string, Action<Command>>();

            // Set up a state object
            _ActorState = new ClientState(PersistenceId);


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
            UseRecoveryCommandHandler.Add("ClientInsertCommand", command => InsertNewClientRecoveryCommand(command as ClientInsertCommand));
            UseRecoveryCommandHandler.Add("ClientUpsertCommand", command => UpsertNewClientRecoveryCommand(command as ClientUpsertCommand));
            UseRecoveryCommandHandler.Add("ClientUpdateCommand", command => UpdateClientRecoveryCommand(command as ClientUpdateCommand));
            UseRecoveryCommandHandler.Add("ClientDeleteCommand", command => DeleteClientRecoveryCommand(command as ClientDeleteCommand));
            UseRecoveryCommandHandler.Add("ClientUnDeleteCommand", command => UnDeleteClientRecoveryCommand(command as ClientUnDeleteCommand));

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
                _logger.Info($"Recovery Complete for Id:{_ActorState.Id} UserName:{_ActorState?.Name??"Not Initialized"}", Self.Path.ToStringWithAddress());
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
            Command<ClientInsertCommand>(c => InsertNewClientCommand(c));
            Command<ClientUpsertCommand>(c => UpsertClientCommand(c));
            Command<ClientUpdateCommand>(c => UpdateClientCommand(c));
            Command<ClientDeleteCommand>(c => DeleteClientCommand(c));
            Command<ClientUnDeleteCommand>(c => UnDeleteClientCommand(c));
            
            //Persistence layer messages
            Command<SaveSnapshotSuccess>(c => HandleSuccessfulSnapshotSave(c));
            Command<SaveSnapshotFailure>(c => HandleUnSuccessfulSnapshotSave(c));

            // Requests
            Command<ClientGetStateRequest>(r => {
                Sender.Tell(new ClientGetStateResponse(Sender, _ActorState.Clone(), r));
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
                _logger.Debug($"In Command State Received unhandled message from:{Sender.Path.ToStringWithAddress()} Unhandled Message:{message.GetType().Name}");

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
                            AutoSaveSnashot(true);
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

        private bool DeleteClientRecoveryCommand(ClientDeleteCommand c)
        {
            _ActorState.isActive = false;
            return true;
        }

        private void DeleteClientCommand(ClientDeleteCommand c)
        {
            if(_ActorState.isActive == false)
            {
                var message = new ClientFailedDeleteEvent("Client has been already deleted.", c.Id, c.User, c.ConnectionId);
                Sender.Tell(message, Self);
            }
            else
            {
                // Journal the fact that the client was deleted
                Persist<ClientDeleteCommand>(c, PostDeleteHandler);
            }
        }

        private void PostDeleteHandler(ClientDeleteCommand c)
        {
            // Deleting a client is not permanently removing them from the datastore but rather a simple state change to inactive.
            _ActorState.isActive = false;

            // Once a client has been marked as inactive we want to save the state so that future incarnations of the actor will
            // be in a inactive state.
            AutoSaveSnashot(true);
            _logger.Debug($"User:{c.User} deleted client id:{_ActorState.Id}.");

            ClientDeletedEvent message = new ClientDeletedEvent(_ActorState.Clone(), c.User,c.ConnectionId);
            Sender.Tell(message, Self);
            NotifyCommandEventSubscribers(message);
        }

        #endregion Delete Command

        #region UnDelete Command

        private bool UnDeleteClientRecoveryCommand(ClientUnDeleteCommand c)
        {
            _ActorState.isActive = true;
            return true;
        }

        private void UnDeleteClientCommand(ClientUnDeleteCommand c)
        {
            if (_ActorState.isActive == true)
            {
                var message = new ClientFailedUnDeleteEvent("Client is already active.", c.Id, c.User, c.ConnectionId);
                Sender.Tell(message, Self);
            }
            else
            {
                // Journal the fact that the client was deleted
                Persist<ClientUnDeleteCommand>(c, PostUnDeleteHandler);
            }
        }

        private void PostUnDeleteHandler(ClientUnDeleteCommand c)
        {
            _ActorState.isActive = true;

            // Once a client has been marked as inactive we want to save the state so that future incarnations of the actor will
            // be in a inactive state.
            AutoSaveSnashot(true);
            _logger.Debug($"User:{c.User} undeleted client id:{_ActorState.Id}.");

            ClientUnDeletedEvent message = new ClientUnDeletedEvent(_ActorState.Clone(), c.User, c.ConnectionId);
            Sender.Tell(message, Self);
            NotifyCommandEventSubscribers(message);
        }

        #endregion Delete Command


        #region Update Command

        private bool UpdateClientCommand(ClientUpdateCommand c)
        {


            if (_ActorState.isActive == false)
            {
                ClientFailedUpdateEvent msg = new ClientFailedUpdateEvent(string.Format("Client is deleted(inactive)."), c.ClientStateData, c.User, c.ConnectionId);
                Sender.Tell(msg);
                return true;
            }

            // Enforce required fields
            if (c.ClientStateData.Name == null || c.ClientStateData.Name == "")
            {
                ClientFailedUpdateEvent msg = new ClientFailedUpdateEvent(string.Format("UserName field cannot be blank(or null)."), c.ClientStateData, c.User, c.ConnectionId);
                Sender.Tell(msg);
                return true;
            }


            Persist<ClientUpdateCommand>(c, PostUpdateHandler);
            return true;
        }

        private void PostUpdateHandler(ClientUpdateCommand c)
        {
            // Update updatable fields
            _ActorState.Update(c.ClientStateData);

            AutoSaveSnashot(false);
            
            _logger.Debug($"User:{c.User} updated client id:{_ActorState.Id}.");
            
            // Update whoever requested it with the new state
            ClientUpdatedEvent message = new ClientUpdatedEvent(_ActorState.Clone(), c.User,c.ConnectionId);
            Sender.Tell(message, Self);
            
            // Notify interested actors on the update
            NotifyCommandEventSubscribers(message);
        }

        private bool UpdateClientRecoveryCommand(ClientUpdateCommand c)
        {
            // Update updatable fields
            _ActorState.Update(c.ClientStateData);
            return true;
        }

        #endregion Update Command

        #region Insert Command

        private bool InsertNewClientCommand(ClientInsertCommand c)
        {
            if (c.ClientStateData.Name == null || c.ClientStateData.Name == "")
            {
                Sender.Tell(new ClientFailedInsertEvent("UserName field cannot be blank(or null).", c.ClientStateData, c.User,c.ConnectionId));
                _logger.Error($"While insert PersistenceId: {PersistenceId} client name is missing.");
            }
            if (c.ClientStateData.Id == null || c.ClientStateData.Id == "")
            {
                _ActorState = new ClientState(PersistenceId, c.ClientStateData);
                AutoSaveSnashot(true);
                _logger.Info($"Inserted {_ActorState.DocumentType} for id:{_ActorState.Id}");
                ClientInsertedEvent message = new ClientInsertedEvent(_ActorState.Clone(), c.User, c.ConnectionId);
                Sender.Tell(message, Self);
                NotifyCommandEventSubscribers(message);
            }
            return true;
        }


        private void InsertNewClientRecoveryCommand(ClientInsertCommand c)
        {
            // When recovering set the state of the actor
            _ActorState = c.ClientStateData;
        }

        #endregion Insert Command

        #region Upsert Command
        private bool UpsertClientCommand(ClientUpsertCommand c)
        {
            if (_ActorState.isActive == false && c.ClientStateData.isActive == true)
            {
                _logger.Error($"Reactivating client with Id: {PersistenceId}");
                return true;
            }
            if (c.ClientStateData.Name == null || c.ClientStateData.Name == "")
            {
                Sender.Tell(new ClientFailedInsertEvent("UserName field cannot be blank(or null).", c.ClientStateData, c.User, c.ConnectionId));
                _logger.Error($"While insert PersistenceId: {PersistenceId} client name is missing.");
                return true;
            }
            if (c.ClientStateData.Id == null || c.ClientStateData.Id == "")
            {
                Sender.Tell(new ClientFailedInsertEvent("Client Id cannot be empty.", c.ClientStateData, c.User, c.ConnectionId));
                _logger.Error($"While insert PersistenceId: {PersistenceId} client id is missing.");
                return true;
            }

            Persist<ClientUpsertCommand>(c, PostUpsertHandler);
            return true;
        }

        private void PostUpsertHandler(ClientUpsertCommand c)
        {
            ClientState cs = c.ClientStateData;
            _ActorState = cs;
            AutoSaveSnashot(false);
            _logger.Info($"Updated/Inserted {cs.DocumentType} for id:{_ActorState.Id}");
            ClientUpsertedEvent message = new ClientUpsertedEvent(_ActorState.Clone(), c.User, c.ConnectionId);
            Sender.Tell(message, Self);
            NotifyCommandEventSubscribers(message);
        }

        private void UpsertNewClientRecoveryCommand(ClientUpsertCommand c)
        {
            // When recovering set the state of the actor
            _ActorState = c.ClientStateData;
        }
        
        #endregion Upsert Command

        #region Snapshots
        private void ProcessSnapshot(SnapshotOffer snapshotOffer)
        {
            try
            {
                Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)snapshotOffer.Snapshot;
                _ActorState = jo.ToObject<ClientState>();
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to parse JSON snapshot for id:{PersistenceId} - exception message:{e.Message}");
            }
        }

        private void AutoSaveSnashot(bool forceSave)
        {
            _CommandActivityCounter++;
            if (_CommandActivityCounter % _SnapshotTriggerCount == 0 || forceSave)
            {
                string prePhrase = forceSave?"Force":"Auto";
                SaveSnapshot(_ActorState);
                _logger.Debug("Attempting {0} save snapshot actor Type-ActorId -UserName:{1}-{2}-{3}",prePhrase, _ActorType,_ActorState.Id, _ActorState.Name);
            }
        }

        private bool HandleSuccessfulSnapshotSave(SaveSnapshotSuccess c)
        {
            _logger.Debug("Successfully saved snapshot for {0} actor ActorId -UserName:{1}-{2}", _ActorType, _ActorState.Id, _ActorState.Name);
            return true;
        }

        private bool HandleUnSuccessfulSnapshotSave(SaveSnapshotFailure c)
        {
            _logger.Debug("Unsuccessfully saved snapshot for {0} actor ActorId -UserName:{1}-{2}", _ActorType, _ActorState.Id, _ActorState.Name);
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
