using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Newtonsoft.Json.Linq;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.State;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.BridgeInterfaces;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Response;
using System.Collections.Immutable;
using EY.SSA.CommonBusinessLogic.Actors.Interfaces;

namespace EY.SSA.CommonBusinessLogic.Actors
{
    public class AreaExternaMessageIncomingProcessor : ReceiveActor, IWithUnboundedStash
    {
        #region fields
        private ILoggingAdapter _logger = Context.GetLogger();

        private static string _ActorType = typeof(HTTPSourceBridgeActor).Name;

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> _EventSubscribers = new HashSet<IActorRef>();

        private IExternalActionMessageToAkkaMessageHandler _ExternalCommandMessageHandler;

        #endregion fields

        #region Properties
        public static string ActorType
        {
            get { return _ActorType; }
        }

        public IStash Stash { get; set; }


        #endregion Properties

        #region Constructor(s)
        public AreaExternaMessageIncomingProcessor(IExternalActionMessageToAkkaMessageHandler externalCommandMessageHandler)
        {

            _ExternalCommandMessageHandler = externalCommandMessageHandler;

            Initializing();
        }
        #endregion Constructor(s)

        #region Actor States

        /// <summary>
        /// This method sets the actor so that it can "boot" itself up.
        /// </summary>
        private void Initializing()
        {
            _logger.Debug($"Area:{_ExternalCommandMessageHandler.Area.ToString()} - Initializing.");


            // This catch all will log if there are any weird unhandled messages.
            ReceiveAny(message =>
            {
                Stash.Stash();
                _logger.Debug($"Stashing unhandled message from:{Sender.Path.ToStringWithAddress()} Unhandled Message of type:{message.GetType().Name} - Stashing.");
            });

            Become(Ready);

            _logger.Debug($"Area:{_ExternalCommandMessageHandler.Area.ToString()} - Initialized.");
        }

        /// <summary>
        /// The command processing state sets this actor ready to receive any command from other actors.
        /// </summary>
        private void Ready()
        {
            _logger.Debug($"Area:{_ExternalCommandMessageHandler.Area.ToString()} - Actor getting Ready.");

            // Process Commands that are coming from external clients
            Receive<HTTPSourcedCommand>(c => _ExternalCommandMessageHandler.ProcessCommand(c));

            Receive<HTTPSourcedRequest>(r => _ExternalCommandMessageHandler.ProcessRequest(r));

            Receive<RemoveHTTPClient>(e => _ExternalCommandMessageHandler.ProcessHTTPClientRemoval(e));

            // String command handler
            Receive<string>(s => HandleStringCommand(s));

            // This catch all will log if there are any weird unhandled messages.
            ReceiveAny(o =>
            {
                _logger.Debug("{1} got unhandled message from:{0} Unhandled Message:{1}", Sender.Path.ToStringWithAddress(), o.GetType().Name, _ActorType);
            });

            Stash?.UnstashAll();

            _logger.Debug($"Area:{_ExternalCommandMessageHandler.Area.ToString()} - Actor Ready.");

        }

        #endregion Actor States

        #region Internal Message Handlers

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

        #endregion Internal Message Handlers

        #region External Message Handlers
        #endregion External Message Handlers

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
