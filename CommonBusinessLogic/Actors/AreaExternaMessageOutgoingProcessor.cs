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
    public class AreaExternaMessageOutgoingProcessor : ReceiveActor, IWithUnboundedStash
    {
        #region fields
        private ILoggingAdapter _logger = Context.GetLogger();

        private static string _ActorType = typeof(HTTPSourceBridgeActor).Name;

        // This HashSet is used to track other actors which are interested in receiving state change events from this actor.
        protected HashSet<IActorRef> _EventSubscribers = new HashSet<IActorRef>();

        private IAkkaMessagetoExternalActionMessageHandler _InternalToExternalMessageHandler;

        #endregion fields

        #region Properties
        public static string ActorType
        {
            get { return _ActorType; }
        }

        public IStash Stash { get; set; }


        #endregion Properties

        #region Constructor(s)
        public AreaExternaMessageOutgoingProcessor(IAkkaMessagetoExternalActionMessageHandler internalToExternalMessageHandler)
        {

            _InternalToExternalMessageHandler = internalToExternalMessageHandler;

            Initializing();
        }
        #endregion Constructor(s)

        #region Actor States

        /// <summary>
        /// This method sets the actor so that it can "boot" itself up.
        /// </summary>
        private void Initializing()
        {
            _logger.Debug($"Area:{_InternalToExternalMessageHandler.Area.ToString()} - Initializing.");


            // This catch all will log if there are any weird unhandled messages.
            ReceiveAny(message =>
            {
                Stash.Stash();
                _logger.Debug("Received unhandled message from:{0} Unhandled Message of type:{1} - Stashing.", Sender.Path.ToStringWithAddress(), message.GetType().Name);
            });

            Become(Ready);

            _logger.Debug($"Area:{_InternalToExternalMessageHandler.Area.ToString()} - Initialized.");
        }

        /// <summary>
        /// The command processing state sets this actor ready to receive any command from other actors.
        /// </summary>
        private void Ready()
        {
            _logger.Debug($"Area:{_InternalToExternalMessageHandler.Area.ToString()} - Actor getting Ready.");

            // String command handler
            Receive<string>(s => HandleStringCommand(s));

            // Handle a known commands (insert, update, delete, upsert)
            Receive<InsertCommandEventMessage>(e => _InternalToExternalMessageHandler.TranslateAkkaInsertEventToExternalMessage(e));
            Receive<FailedInsertCommandEventMessage>(e => _InternalToExternalMessageHandler.TranslateAkkaFailedInsertEventToExternalMessage(e));

            Receive<UpdateCommandEventMessage>(e => _InternalToExternalMessageHandler.TranslateAkkaUpdateEventToExternalMessage(e));
            Receive<FailedUpdateCommandEventMessage>(e => _InternalToExternalMessageHandler.TranslateAkkaFailedUpdateEventToExternalMessage(e));

            Receive<DeleteCommandEventMessage>(e => _InternalToExternalMessageHandler.TranslateAkkaDeleteEventToExternalMessage(e));
            Receive<FailedDeleteCommandEventMessage>(e => _InternalToExternalMessageHandler.TranslateAkkaFailedDeleteEventToExternalMessage(e));

            Receive<UnDeleteCommandEventMessage>(e => _InternalToExternalMessageHandler.TranslateAkkaUnDeleteEventToExternalMessage(e));
            Receive<FailedUnDeleteCommandEventMessage>(e => _InternalToExternalMessageHandler.TranslateAkkaFailedUnDeleteEventToExternalMessage(e));

            // Handle Area Unique Commands
            Receive<CommandEventMessage>(e => _InternalToExternalMessageHandler.ProcessCommandEvent(e));

            Receive<Response>(r => {
                _InternalToExternalMessageHandler.ProcessRequestResponse(r);
            });

            // This catch all will log if there are any weird unhandled messages.
            ReceiveAny(o =>
            {
                _logger.Debug($"Unhandled message from:{Sender.Path.ToStringWithAddress()} Unhandled Message:{o.GetType().Name}");
            });

            Stash?.UnstashAll();
            _logger.Debug($"Area:{_InternalToExternalMessageHandler.Area.ToString()} - Actor Ready.");
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
