using Akka.Actor;
using Akka.Event;
using EY.SSA.CommonBusinessLogic.Actors.Interfaces;
using EY.SSA.CommonBusinessLogic.BridgeInterfaces;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Actions;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.Messages.Response;
using System;
using System.Collections.Generic;


namespace EY.SSA.CommonBusinessLogic.Actors
{
    public class HTTPSourceBridgeActor:ReceiveActor, IWithUnboundedStash
    {
        #region fields
        private ILoggingAdapter _logger = Context.GetLogger();

        private static string _ActorType = typeof(HTTPSourceBridgeActor).Name;

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> _EventSubscribers = new HashSet<IActorRef>();

        // This is how we call methods on the http client
        private IHTTPExternalInterface _HTTPClientInterface;

        // Use this to maintain the state of connections and areas
        private Dictionary<string, List<MicroServices.Area>> _ConnectionAreas;

        // Supervisor Registry - Knows of all other supervisor areas
        private IActorRef _SupervisorRegistry;

        private int _FetchSupervisorListRetryCount; // Number of times to retrying to get the supervisor list.

        // Keeps a dictionary of areas to supervisor actor references.
        private Dictionary<MicroServices.Area, IActorRef> _AreaToSupervisorActors;

        private object _IncomingMessageProcessorName = "IncomingMessageProcessor";
        private object _OutgoingMessageProcessorName = "OutgoingMessageProcessor";

        #endregion fields

        #region Properties
        public static string ActorType
        {
            get { return _ActorType; }
        }

        public IStash Stash { get; set; }


        #endregion Properties

        #region Constructor(s)
        public HTTPSourceBridgeActor(IHTTPExternalInterface httpClientEventInterface, IActorRef SupervisorRegistry) 
        {

            _FetchSupervisorListRetryCount = 1;

            _SupervisorRegistry = SupervisorRegistry;

            _HTTPClientInterface = httpClientEventInterface;
            
            // Put actor in recovering state
            Initializing();
        }
        #endregion Constructor(s)

        #region Actor States

        /// <summary>
        /// This method sets the actor so that it can "boot" itself up.
        /// </summary>
        private void Initializing()
        {
            _logger.Debug("Initializing.");

            //Attempt to get a list of supervisors from the SupervisorRegistry
            _logger.Info("Requesting supervisor list from:{0}", _SupervisorRegistry.Path.ToStringWithAddress());
            SupervisorRegistryGetListRequest request = new SupervisorRegistryGetListRequest(Self);
            _SupervisorRegistry.Tell(request);

            var timeout = Context.System.Scheduler.ScheduleTellOnceCancelable(1000, Self, new SupervisorRegistryGetListEvent(request, null, false), Self);

            Receive<SupervisorRegistryGetListResponse>(r => {
                timeout.Cancel();
                _logger.Info("Received supervisor list from:{0}", _SupervisorRegistry.Path.ToStringWithAddress());
                Self.Tell(new SupervisorRegistryGetListEvent(request,r,true));

            });

            //Context.System.EventStream.Subscribe(Self, typeof(DeadLetter));

            //Receive<DeadLetter>(d => {
            //    if (d != null)
            //    {
            //        _logger.Debug(d?.Sender?.Path.ToStringWithAddress());
            //        _logger.Debug(d?.Recipient?.Path.ToStringWithAddress());
            //        _logger.Debug(d?.Message?.ToString());
            //    }
            //});

            Receive<SupervisorRegistryGetListEvent>(e =>
            {

                if (e.Success)
                {
                    // Save the list for internal use
                    _AreaToSupervisorActors = e.ResponseGetList.AreaToSupervisorActorRef;

                    // Instantiate helper actors that will process messages for each area
                    InstantiateMessageProcessingHelperActors();

                    Become(Ready);
                }
                else
                {
                    _logger.Warning("Cannot retrieve list of supervisors. Unable to initialize.  {0} retries.", _FetchSupervisorListRetryCount);

                    // retry the request and increase the timeout
                    _FetchSupervisorListRetryCount++;

                    // Set up the timeout adding a second each time it fails capping at 60 secs
                    int currentTimeoutTimeMilliSeconds = 1000 * _FetchSupervisorListRetryCount <= 1000 * 60 ? 1000 * _FetchSupervisorListRetryCount : 60000;
                    timeout = Context.System.Scheduler.ScheduleTellOnceCancelable(currentTimeoutTimeMilliSeconds, Self, new SupervisorRegistryGetListEvent(request, null, false), Self);

                    // Send the request again
                    _SupervisorRegistry.Tell(request);
                }
            });

            // This catch all will log if there are any weird unhandled messages.
            ReceiveAny(o =>
            {
                Stash.Stash();

                _logger.Debug("{1} Got unhandled message From:{0}", Sender.Path.ToStringWithAddress(),_ActorType);
            });
            _logger.Debug("Initialized.");
        }


        /// <summary>
        /// The command processing state sets this actor ready to receive any command from other actors.
        /// </summary>
        private void Ready()
        {
            _logger.Debug("Actor {0} is getting Ready.", _ActorType);
            
            // Process external commands that are coming from external (http) clients
            Receive<HTTPSourcedCommand>(c => ProcessExternalHTTPCommandHandler(c));

            // Process external requests that are coming from external (http) clients
            Receive<HTTPSourcedRequest>(r => ProcessExternalHTTPRequestHandler(r));

            Receive<RemoveHTTPClient>(e => ProcessHTTPClientRemoval(e));

            // String command handler
            Receive<string>(s=> HandleStringCommand(s));

            // This catch all will log if there are any weird unhandled messages.
            ReceiveAny(o =>
            {
                _logger.Debug("{1} got unhandled message from:{0} Unhandled Message:{1}", Sender.Path.ToStringWithAddress(), o.GetType().Name, _ActorType);
            });

            Stash?.UnstashAll();

            _logger.Debug("Actor {0} is Ready.", _ActorType);

        }

        #endregion Actor States

        #region Message Handlers


        #region String Messages/Commands
        private void HandleStringCommand(string s)
        {
            if (s != null)
            {
                switch (s)
                {
                    default:
                        {
                            _logger.Debug("{2} got unhandled string message from:{0} Unhandled Message:{1}", Sender.Path.ToStringWithAddress(), s, _ActorType);
                            break;
                        }

                }
            }
        }
        #endregion String Messages/Commands

        #endregion MessageHandlers

        #region External Message Handlers

        private bool ProcessHTTPClientRemoval(RemoveHTTPClient e)
        {
            _ConnectionAreas.Remove(e.ConnectionId);
            return true;
        }

        /// <summary>
        /// This external command message handler parses the JSON message and attempts to determine the area to which it belongs.
        /// Once it has done that it will forward the message to the actor responsible for handling the discovered area.
        /// </summary>
        /// <param name="c"></param>
        private void ProcessExternalHTTPCommandHandler(HTTPSourcedCommand c)
        {
            MicroServices.CommandType cType;
            MicroServices.Area area;
            bool ableToParseCommand = MicroServices.ParseCommandType(c.CommandType,out cType);
            bool ableToParseArea = MicroServices.ParseArea(c.Area,out area);
            
            // Handle Area
            if (ableToParseArea)
            {
                _logger.Debug("HTTP Client Bridge Converted Area'{0}' to internal value{1}.", c.Area, area.ToString());
                _logger.Debug("Adding user '{0}' to group '{1}'", c.User, area.ToString());

                TrackConnection(c, area);

                // Now the we know the area forward it to the Area handler.  Nontice how the actor's name is dynamically constructed.
                Context.Child($"{area}{_IncomingMessageProcessorName}").Tell(c);
            }
            else
            {
                _logger.Debug("HTTP Client Bridge received unknown string area value'{0}'.", c.Area);
                _HTTPClientInterface.HandleFailedStateMessage(new HTTPDestinedCommandStateEvent(MicroServices.ProcessingStatus.Failed, "Unknown area:" + c.Area, c), true);

            }
        }

        /// <summary>
        /// This external request message handler parses the JSON message and attempts to determine the area to which it belongs.
        /// Once it has done that it will forward the message to the actor responsible for handling the discovered area.
        /// </summary>
        /// <param name="r"></param>
        private void ProcessExternalHTTPRequestHandler(HTTPSourcedRequest r)
        {
            MicroServices.RequestType rType;
            MicroServices.Area area;
            bool ableToParseRequest = MicroServices.ParseRequestType(r.RequestType, out rType);
            bool ableToParseArea = MicroServices.ParseArea(r.Area, out area);

            // Handle Area
            if (ableToParseArea)
            {
                _logger.Debug("HTTP Client Bridge Converted Area'{0}' to internal value{1}.", r.Area, area.ToString());
                _logger.Debug("Adding user '{0}' to group '{1}'", r.User, area.ToString());

                TrackConnection(r, area);

                // Now that we know the area we can forward it to the Area request handler
                Context.Child($"{area}{_IncomingMessageProcessorName}").Tell(r);
            }
            else
            {
                _logger.Debug("HTTP Client Bridge received unknown string area value'{0}'.", r.Area);
                _HTTPClientInterface.HandleFailedStateMessage(new HTTPDestinedRequestStateEvent(MicroServices.ProcessingStatus.Failed, "Unknown area:" + r.Area, r), true);
            }

        }

        #endregion External Message Handlers

        #region Helper Methods

        /// <summary>
        /// This helper method instantiates the for each AREA (Clients, Projects, etc.) the incoming and outgoing message handler actors.
        /// It utilizes the _AreaToSupervisorActors dictionary as a source of the AREAS for which actors must be instantiated.
        /// </summary>
        private void InstantiateMessageProcessingHelperActors()
        {
            foreach (KeyValuePair<MicroServices.Area, IActorRef> kvp in _AreaToSupervisorActors)
            {
                string area = kvp.Key.ToString();

                // FYI the kvp.Key =>is=> area and kvp.Value =>is=> supervisor 

                // Within each case clause order is important because:
                // - The actor responsible for sending messages out must be declared first to that we have a reference.
                // - The reference is needed by the incoming external message processing actor so that it can embed the
                //   outgoing's external message processing actor's reference.
                // In this way, when all internal processing is done the messages can be sent out to the external http client
                // via the area's outgoing message processor.

                //NOTE: 
                // AreaExternaMessageOutgoingProcessor and AreaExternaMessageIncomingProcessor are generic actors.
                // The identity of how they process outgoing or incomming messages is defined by the:
                // - [AREA]AkkaMessageToExternalMessageTranslator class or
                // - [AREA]ExternalMessageToAkkaMessageTranslator
                // Where [AREA] is the area for which the translation will be done.

                switch (kvp.Key)
                {
                    case MicroServices.Area.Client:
                        {

                            // Instantiate the actor that translates messages going out of the akka system to the http (external) client.
                            IActorRef sender = Context.ActorOf(Props.Create<AreaExternaMessageOutgoingProcessor>(
                                new ClientAkkaMessageToExternalMessageTranslator(_HTTPClientInterface, _logger)), // Thing that will handle akka msg -> http msg
                                $"{area}{_OutgoingMessageProcessorName}"// UserName of the external message receiving actor
                            );

                            // Instantiate the actor that translates messages coming into the akka system from the http (external) client.
                            IActorRef Receiver = Context.ActorOf(Props.Create<AreaExternaMessageIncomingProcessor>(
                                new ClientExternalMessageToAkkaMessageTranslator(_HTTPClientInterface, kvp.Value,sender,_logger)), // Thing that will xlate http msg -> akka msg
                                $"{area}{_IncomingMessageProcessorName}"// UserName of the external message receiving actor
                            );
                            break;
                        }
                    case MicroServices.Area.Project:
                        {
                            _logger.Error("Project processing not implemented");
                            break;
                        }
                    case MicroServices.Area.User:
                        {
                            //// Instantiate the actor that translates messages going out of the akka system to the http (external) client.
                            //IActorRef sender = Context.ActorOf(Props.Create<AreaExternaMessageOutgoingProcessor>(
                            //    new UserAkkaMessageToExternalMessageTranslator(_HTTPClientInterface, _logger)), // Thing that will handle akka msg -> http msg
                            //    $"{area}{_OutgoingMessageProcessorName}"// UserName of the external message receiving actor
                            //);

                            //// Instantiate the actor that translates messages coming into the akka system from the http (external) client.
                            //IActorRef Receiver = Context.ActorOf(Props.Create<AreaExternaMessageIncomingProcessor>(
                            //    new UserExternalMessageToAkkaMessageTranslator(_HTTPClientInterface, kvp.Value, sender, _logger)), // Thing that will xlate http msg -> akka msg
                            //    $"{area}{_IncomingMessageProcessorName}"// UserName of the external message receiving actor
                            //);
                            break;
                        }
                    case MicroServices.Area.Config:
                        {
                            _logger.Error("Project processing not implemented");
                            break;
                        }
                    case MicroServices.Area.Entity:
                        {
                            _logger.Error("Project processing not implemented");
                            break;
                        }
                    case MicroServices.Area.CostPool:
                        {
                            _logger.Error("Project processing not implemented");
                            break;
                        }
                    default:
                        {
                            _logger.Error("Unknown area. Processing not implemented");
                            break;
                        }

                }


            }
        }
        /// <summary>
        /// Adds connection to internal dictionary and also ensures the connection joins the appropriate SignalR group.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="area"></param>
        private void TrackConnection(HTTPSourcedAction a, MicroServices.Area area)
        {
            _ConnectionAreas = _ConnectionAreas??new Dictionary<string, List<MicroServices.Area>>();

            // Add the connection to the dict if not there
            if (!_ConnectionAreas.ContainsKey(a.ConnectionId))
            {
                _ConnectionAreas.Add(a.ConnectionId, new List<MicroServices.Area>());
            }
            // Add the area and add the user to the signal r group if not there
            if (!_ConnectionAreas[a.ConnectionId].Contains(MicroServices.Area.Client))
            {
                _logger.Debug("Adding user '{0}' to group '{1}'", a.User, area.ToString());
                _ConnectionAreas[a.ConnectionId].Add(area);
                _HTTPClientInterface.JoinGroup(area.ToString(), a.ConnectionId);
            }
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
