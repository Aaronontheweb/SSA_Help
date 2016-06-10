using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Response;
using EY.SSA.CommonBusinessLogic.State;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Actors
{
    /// <summary>
    /// This actor gathers and maintains a list of the state of client actors.  Eventthough it has state (the list of client state) if the actor is restarted it looses its state and must be reconstructed.
    /// </summary>
    public class ClientStateAccumulator: ReceiveActor, IWithUnboundedStash
    {
        #region fields

        private Akka.Event.ILoggingAdapter _logger = Context.GetLogger();

        private static string _ActorType = typeof(ClientSupervisor).Name;

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> Subscribers = new HashSet<IActorRef>();

        private HashSet<ClientGetStateRequest> _PendingRequests = new HashSet<ClientGetStateRequest>();

        Dictionary<string, ClientState> _ActorState;

        #endregion fields

        #region properties

        public static string ActorType
        {
            get { return _ActorType; }
        }

        public IStash Stash { get; set; }

        #endregion properties

        public ClientStateAccumulator()
        {
            // Initialize the Actor's state
            _ActorState = new Dictionary<string, ClientState>();

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

            //Request from parent the list of child actor IActorRefs to request the state from them
            Context.Parent.Tell(new ClientGetChildActorRefs(Self,null));


            //
            // Handle Responses
            //

            Receive<ClientGetChildActorRefsResponse>(r => {
                _logger.Debug("Received child actor references.");
                // Go get the child actor states
                foreach (IActorRef childActorRef in r.ListOfChildActorRefs)
                {
                    _logger.Debug($"Requesting child state from:{childActorRef.Path.Name}");
                    ClientGetStateRequest newRequest = new ClientGetStateRequest(Self);
                    childActorRef.Tell(newRequest);
                    _PendingRequests.Add(newRequest);
                }
            });

            // Save the client state
            Receive<ClientGetStateResponse>(r => {
                _logger.Debug($"Received child actor state from:{Sender.Path.Name}");
                if(r.ReplyClientState != null)
                    _ActorState.Add(r.ReplyClientState.Id, r.ReplyClientState);
                _PendingRequests.Remove(r.OriginalRequest as ClientGetStateRequest);
                if (_PendingRequests.Count == 0)
                {
                    _logger.Info("Received all Client actor states.");
                    Become(Ready);
                }
            });

            // Reply to self with a null client state
            Receive<ClientGetStateRequest>(r=> { Sender.Tell(new ClientGetStateResponse(r.Requestor, null, r));});

            Receive<SubscribedForCommandEvents>(r => {
            _logger.Info($"Subscribed for '{r.Id}' command events");
            });


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
            // Handle Requests
            //

            // Replies to sender with a list of actor states when requested
            Receive<ClientGetListRequest>(r => {
                _logger.Debug($"Sending list of clients to {Sender.Path.ToStringWithAddress()}");
                Sender.Tell(new ClientGetListResponse(Sender, _ActorState.Values.ToImmutableList(), r));
            });

            //
            // Handle Events
            //
            Receive<SubscribedForCommandEvents>(e => { _logger.Info("Now listening to:{0}", e.Id); });

            Receive<ClientInsertedEvent>(e => {
                if (_ActorState.ContainsKey(e.Id))
                    _ActorState[e.Id] = e.ResultClientState.Clone();
                else
                    _ActorState.Add(e.Id, e.ResultClientState.Clone());
            });
            Receive<ClientUpdatedEvent>(e => {
                if (_ActorState.ContainsKey(e.Id))
                    _ActorState[e.Id] = e.ResultClientState.Clone();
                else
                    _ActorState.Add(e.Id, e.ResultClientState.Clone());
            });
            Receive<ClientUpsertedEvent>(e => {
                if (_ActorState.ContainsKey(e.Id))
                    _ActorState[e.Id] = e.ResultClientState.Clone();
                else
                    _ActorState.Add(e.Id, e.ResultClientState.Clone());
            });
            Receive<ClientDeletedEvent>(e => {
                if (_ActorState.ContainsKey(e.Id))
                    _ActorState[e.Id] = e.ResultClientState.Clone();
                else
                    _ActorState.Add(e.Id, e.ResultClientState.Clone());
            });
            Receive<ClientUnDeletedEvent>(e => {
                if (_ActorState.ContainsKey(e.Id))
                    _ActorState[e.Id] = e.ResultClientState.Clone();
                else
                    _ActorState.Add(e.Id, e.ResultClientState.Clone());
            });

            // String command handler
            Receive<string>(s => HandleStringCommand(s));

            // This catch all will log if there are any weird unhandled messages.
            Receive<object>(message =>
            {
                _logger.Debug($"Ignoring message from:{Sender.Path.ToStringWithAddress()} Unhandled Message:{message.GetType().Name}");
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
                    case "PrintChildActorStates":
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

        #region RequestHandlers
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
}