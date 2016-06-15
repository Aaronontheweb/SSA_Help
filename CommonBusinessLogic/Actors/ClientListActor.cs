using System;
using System.Collections.Generic;
using Akka.Persistence;
using Akka.Actor;
using Akka.Event;
using Newtonsoft.Json.Linq;
using System.Collections.Immutable;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.State;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.Messages.Configuration;
using EY.SSA.CommonBusinessLogic.Messages.Response;

namespace EY.SSA.CommonBusinessLogic.Actors
{



    /// <summary>
    /// This actor maintains a list of clients and child actors representing each client.
    /// </summary>
    public class ClientListActor : ReceivePersistentActor
    {
        #region Fields
        private ILoggingAdapter _logger = Context.GetLogger();


        // This line of code sets the persistence Id for this actor.
        public override string PersistenceId { get; } =  Context.Self.Path.Name;
        //public string PersistenceId { get; } = Context.Self.Path.Name;

        private int _SnapshotTriggerCount = 5; // 5 commands triggers a snapshot

        private int _InactivityFlushSec = 2592000; // InactivityFlush 2592000 = 30 days

        private static string _ActorType = typeof(ClientListActor).Name;

        private int _CommandActivityCounter;

        // _ActorState holds the current state of this actor.
        ClientListState _ActorState;

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
        public ClientListActor()
        {
            //Set up the command handlers
            UseRecoveryCommandHandler = new Dictionary<string, Action<Command>>();

            // Set up a state object
            _ActorState = new ClientListState(PersistenceId, _ActorType);

            Recovering();
        }


        #region Actor States
        /// <summary>
        /// This method sets the recovering state.It will remain active until the actor if fully recovered from the 
        /// Journal/Snashot stores.  It will switch to the CommandProcessing state once it receives the RecoveryCompleted message.
        /// </summary>
        private void Recovering()
        {
            try
            {
                _logger.Debug($"Setting Up Persistence Actor with persistence id: {PersistenceId}");

                // ******** IMPORTANT ***********
                // For each command there should be a handler defined
                // ******************************
                UseRecoveryCommandHandler.Add("ClientListInsertCommand", command => InsertNewClientListItemRecoveryCommand(command as ClientListInsertCommand));
                UseRecoveryCommandHandler.Add("ClientListUpdateCommand", command => UpdateClientListRecoveryCommand(command as ClientListUpdateCommand));
                UseRecoveryCommandHandler.Add("ClientListDeleteCommand", command => DeleteClientListRecoveryCommand(command as ClientListDeleteCommand));
                UseRecoveryCommandHandler.Add("ClientListUnDeleteCommand", command => UnDeleteClientListRecoveryCommand(command as ClientListUnDeleteCommand));

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

                Recover<RecoveryCompleted>(rc =>
                {
                    _logger.Info("Recovery Complete.", Self.Path.ToStringWithAddress());
                });

                _logger.Info("Setting Up Command Handlers.");
                // Commands
                Command<SaveSnapshotSuccess>(c => HandleSuccessfulSnapshotSave(c));
                Command<SaveSnapshotFailure>(c => HandleUnSuccessfulSnapshotSave(c));

                // Events
                Command<ClientInsertedEvent>(e => HandleClientInsertedEvent(e));
                Command<ClientDeletedEvent>(e => HandleClientDeletedEvent(e));
                Command<ClientUnDeletedEvent>(e => HandleClientUnDeletedEvent(e));
                Command<ClientUpdatedEvent>(e => HandleClientUpdatedEvent(e));
                Command<SubscribedForCommandEvents>(e => { _logger.Info("Now listening to:{0}", e.Id); });

                // Requests
                Command<ClientGetStateRequest>(r => { Sender.Tell(new ClientGetStateResponse(Sender, null, r)); });
                Command<ClientIdGetListRequest>(r => HandleClientIdGetListRequest(r));

                // Configuration
                Command<SetSnapshotTriggerCount>(c => { Persist<SetSnapshotTriggerCount>(c, SetSnapshotTriggerConfigurationValue); });
                Command<SetInactivityFlushSec>(c => { Persist<SetInactivityFlushSec>(c, SetInactivityFlushSecConfigurationValue); });

                // Handle any string commands
                Command<string>(s => HandleStringCommand(s));

                CommandAny(o =>
                {
                    _logger.Debug(o.ToString());
                });
                //This catch all will log if there are any weird unhandled messages.
                Command<object>(message =>
                {
                    _logger.Debug($"In Command State Received unhandled message from:{Sender.Path.ToStringWithAddress()} Unhandled Message:{message.GetType().Name}");
                });

                _logger.Debug("Command Handlers Set Up.");

                _logger.Debug($"Completed set-up of persistence actor persistence id: {PersistenceId}");

            }
            catch (Exception ex)
            {
                _logger.Error("Something went really bad during recovery.");
            }

        }


        #endregion Actor States

        #region Handle Requests

        /// <summary>
        /// Replies back with a list of active Client persistenceId's
        /// </summary>
        /// <param name="r"></param>
        private void HandleClientIdGetListRequest(ClientIdGetListRequest r)
        {
            Sender.Tell(new ClientIdGetListResponse(Sender, _ActorState.ClientList.Keys.ToImmutableArray(), r));
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
                    case "PrintClientList":
                        {
                            LogClientList();
                            break;
                        }
                    case "ProvideClientList":
                        {
                            GenerateAndProvideCurrentClientList();
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

        private void GenerateAndProvideCurrentClientList()
        {
            Sender.Tell(_ActorState.ClientList.ToImmutableDictionary<string, ClientListItem>());
        }

        private void LogClientList()
        {
            IDictionary<string, ClientListItem> Clients = _ActorState.ClientList;
            foreach (string clientId in Clients.Keys)
            {
                _logger.Debug("ClientId-ClientName:{0}-{1}", clientId, Clients[clientId].Name);
            }
        }

        #endregion String Messages/Commands

        #region Delete Command/Event Handlers

        /// <summary>
        /// Removes client(item) from the list.  If an instance of the ClientActor is in memory it unloads it.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private void HandleClientDeletedEvent(ClientDeletedEvent e)
        {
            // Is the item still in the list?
            if (_ActorState[e.Id] == null)
            {
                _logger.Info($"{e.User} tried deleting from client list id:{e.Id} but was not found.");
            }
            else
            {
                ClientListItem cliNewState = _ActorState[e.Id].Copy();
                cliNewState.IsActive = false;

                // Memorialize that we are removing the item from the list in the journal
                ClientListDeleteCommand clientListDeleteCommand = new ClientListDeleteCommand(
                    cliNewState,
                    e.User,
                    e.ConnectionId);

                _logger.Info($"Deleting client list item Id{e.Id} UserName:{e.ResultClientState.Name}.");
                Persist<ClientListDeleteCommand>(clientListDeleteCommand, PostClientListDeleteHandler);

            }
        }

        private void PostClientListDeleteHandler(ClientListDeleteCommand c)
        {
            _logger.Info($"Deleted client list item Id{c.Id} UserName:{c.ClientListItemData.Name}.");
            _ActorState[c.ClientId] = c.ClientListItemData;
            ClientListItemDeletedEvent message = new ClientListItemDeletedEvent(c.ClientListItemData.Copy(), c.User, c.ConnectionId);
            NotifySubscribers(message);
            AutoSaveSnashot(false);

        }

        private void DeleteClientListRecoveryCommand(ClientListDeleteCommand c)
        {
            _logger.Info($"Recovery Deleting client list item Id{c.Id} UserName:{c.ClientListItemData.Name}.");
            _ActorState[c.ClientId] = c.ClientListItemData;
            _logger.Info($"Recovery Deleted client list item Id{c.Id} UserName:{c.ClientListItemData.Name}.");
        }

        #endregion Delete Command/Event Handlers

        #region Delete Command/Event Handlers

        /// <summary>
        /// Removes client(item) from the list.  If an instance of the ClientActor is in memory it unloads it.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private void HandleClientUnDeletedEvent(ClientUnDeletedEvent e)
        {
            // Is the item still in the list?
            if (_ActorState[e.Id] == null)
            {
                _logger.Info($"{e.User} tried undeleting from client list id:{e.Id} but was not found.");
            }
            else
            {

                ClientListItem cliNewState = _ActorState[e.Id].Copy();
                cliNewState.IsActive = true;

                // Memorialize that we are recovering the item from the list in the journal
                ClientListUnDeleteCommand clientListUnDeleteCommand = new ClientListUnDeleteCommand(
                    cliNewState,
                    e.User,
                    e.ConnectionId);

                _logger.Info($"Undeleting client list item Id{e.Id} UserName:{e.ResultClientState.Name}.");
                Persist<ClientListUnDeleteCommand>(clientListUnDeleteCommand, PostClientListUnDeleteHandler);
            }
        }

        private void PostClientListUnDeleteHandler(ClientListUnDeleteCommand c)
        {
            _logger.Info($"UnDeleting from client list Id:{c.ClientId} UserName:{c.ClientListItemData.Name}.");
            _ActorState[c.ClientId] = c.ClientListItemData;
            ClientListItemUnDeletedEvent message = new ClientListItemUnDeletedEvent(c.ClientListItemData.Copy(), c.User, c.ConnectionId);
            NotifySubscribers(message);
            AutoSaveSnashot(false);
        }

        private void UnDeleteClientListRecoveryCommand(ClientListUnDeleteCommand c)
        {
            _logger.Info($"Recovering Undeleting client list item Id:{c.Id} UserName:{c.ClientListItemData?.Name ?? "No UserName"}.");
            _ActorState[c.ClientId] = c.ClientListItemData;
            _logger.Info($"Recovered Undeleted from client list Id:{c.Id} UserName:{c.ClientListItemData?.Name ?? "No UserName"}.");
        }

        #endregion Delete Command/Event Handlers


        #region Update Command/Event Handlers

        private void HandleClientUpdatedEvent(ClientUpdatedEvent e)
        {
            ClientState cs = e.ResultClientState;
            ClientListItem newClientListItem = ClientListItem.GenerateClientListItemFromClientState(cs);

            ClientListUpdateCommand newCLUC = new ClientListUpdateCommand(newClientListItem, e.User, e.ConnectionId);
            _logger.Info($"Updating client list Id:{cs.Id} UserName:{cs.Name}.");
            Persist<ClientListUpdateCommand>(newCLUC, postClientListItemUpdateHandler);
        }

        private void postClientListItemUpdateHandler(ClientListUpdateCommand c)
        {
            _ActorState[c.Id] = c.ClientListItemData;
            _logger.Info($"Updated client list item Id{c.Id} UserName:{c.ClientListItemData.Name}.");
            ClientListUpdatedEvent message = new ClientListUpdatedEvent(c.ClientListItemData.Copy(), c.User, c.ConnectionId);
            NotifySubscribers(message);
            AutoSaveSnashot(false);

        }

        private void UpdateClientListRecoveryCommand(ClientListUpdateCommand c)
        {
            _logger.Info($"Recovering update client list item Id:{c.Id} UserName:{c.ClientListItemData.Name}.");
            _ActorState[c.Id] = c.ClientListItemData;
            _logger.Info($"Recovered update client list item Id:{c.Id} UserName:{c.ClientListItemData.Name}.");
        }

        #endregion Update Command/Event Handlers

        #region Insert Command/Event Handlers


        /// <summary>
        /// This method handles client insert events by updating the Admin's client list to the latest information.
        /// </summary>
        /// <param name="e">ClientInserted event.</param>
        private void HandleClientInsertedEvent(ClientInsertedEvent e)
        {
            if (_ActorState[e.Id] == null)
            {
                ClientState cs = e.ResultClientState;
                ClientListItem newClientListItem = ClientListItem.GenerateClientListItemFromClientState(cs);

                // It does not matter if the item exists or not we will stomp for that particular client the existing data
                // with the new data - see post insert handler.
                // Persist the event that a new client was added to the client list
                ClientListInsertCommand newCLIC = new ClientListInsertCommand(newClientListItem, e.User, e.ConnectionId);
                _logger.Info($"Inserting new client list item Id:{cs.Id} UserName:{cs.Name}.");
                Persist<ClientListInsertCommand>(newCLIC, postClientListItemInsertHandler);
            }
        }

        /// <summary>
        /// Helper method to update the state of the Admin Actor once the event has be journaled.
        /// </summary>
        /// <param name="c"></param>
        private void postClientListItemInsertHandler(ClientListInsertCommand c)
        {
            _ActorState[c.Id] = c.ClientListItemData;
            _logger.Info($"Inserting new client list item Id:{c.Id} UserName:{c.ClientListItemData.Name}.");
            ClientListInsertedEvent message = new ClientListInsertedEvent(c.ClientListItemData.Copy(), c.User, c.ConnectionId);
            NotifySubscribers(message);
            AutoSaveSnashot(false);
        }

        /// <summary>
        /// This recovery command handler will instantiate the child ClientActor and add/update the ClientItem to the ClientAdminActor's client list.  It will also maintain a reference to the child ClientActor.
        /// </summary>
        /// <param name="c"></param>
        private void InsertNewClientListItemRecoveryCommand(ClientListInsertCommand c)
        {
            _logger.Info($"Recovering Insert new client list item Id:{c.Id} UserName:{c.ClientListItemData.Name}.");
            _ActorState[c.Id] = c.ClientListItemData;
            _logger.Info($"Recovered Insert new client list item Id:{c.Id} UserName:{c.ClientListItemData.Name}.");
        }
        /// <summary>
        /// This command handler attempts to instantiate the child ClientActor and add it to the Admin's ClientItem list. 
        /// </summary>
        /// <param name="cLIC">Client list item command. Contains the ClientItem</param>
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
                var temp = jo.ToObject<ClientListState>();
                foreach (KeyValuePair<string, Object> o in temp.Items)
                {
                    JObject itemJO = o.Value as JObject;
                    _ActorState[o.Key] = itemJO.ToObject<ClientListItem>();

                }
            }
            catch (Exception e)
            {
                _logger.Debug($"Recovering snapshot failed for ActorId:{_ActorState.Id} UserName:{_ActorState?.Name ?? "No name"} exception:{e.Message}");
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

    }
}
