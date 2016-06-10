using Akka.Actor;
using Akka.Event;
using EY.SSA.CommonBusinessLogic.BridgeInterfaces;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Actions;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.State;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using EY.SSA.CommonBusinessLogic.Messages.Response;

namespace EY.SSA.CommonBusinessLogic.Actors.Interfaces
{
    public class ClientAkkaMessageToExternalMessageTranslator : IAkkaMessagetoExternalActionMessageHandler
    {
        public ILoggingAdapter Logger { get; set; }

        // This is how we call methods on the http client
        private IHTTPExternalInterface HTTPExternalInterface { get; set; }

        public MicroServices.Area Area { get; } = MicroServices.Area.Client;

        public ClientAkkaMessageToExternalMessageTranslator(IHTTPExternalInterface httpExternalInterface, ILoggingAdapter logger)
        {
            Logger = logger;
            HTTPExternalInterface = httpExternalInterface;
        }



        public bool ProcessCommandEvent(CommandEventMessage eventCommand)
        {
            bool handled = false;

            // Sample code on how to handle custom commands for this area...

            //// Handle Insert
            //if (eventCommand.GetType() == typeof(ClientInsertedEvent))
            //    handled = TranslateAkkaInsertEventToExternalMessage(eventCommand as ClientInsertedEvent);
            //if(eventCommand.GetType() == typeof(ClientFailedInsertEvent))
            //    handled = TranslateAkkaFailedInsertEventToExternalMessage(eventCommand as ClientFailedInsertEvent);

            //// Handle Updates
            //if (eventCommand.GetType() == typeof(ClientUpdatedEvent))
            //    handled = TranslateAkkaUpdateEventToExternalMessage(eventCommand as ClientUpdatedEvent);
            //if (eventCommand.GetType() == typeof(ClientFailedUpdateEvent))
            //    handled = TranslateAkkaFailedUpdateEventToExternalMessage(eventCommand as ClientFailedUpdateEvent);

            //// Handle Deletes
            //if (eventCommand.GetType() == typeof(ClientDeletedEvent))
            //    handled = TranslateAkkaDeleteEventToExternalMessage(eventCommand as ClientDeletedEvent);
            //if (eventCommand.GetType() == typeof(ClientFailedDeleteEvent))
            //    handled = TranslateAkkaFailedDeleteEventToExternalMessage(eventCommand as ClientFailedDeleteEvent);

            if (!handled)
                Logger.Error($"For {Area.ToString()} area received an unknown command.");

            return handled;

        }

        public bool TranslateAkkaInsertEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            ClientInsertedEvent e = internalCommandEvent as ClientInsertedEvent;

            HTTPExternalInterface.HandleEDStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Processed, 
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.ResultClientState,
                        e.User,
                        e.ConnectionId,
                        e.Id
                    )
                ),
                false //User only?
            );
            return true;
        }

        public bool TranslateAkkaFailedInsertEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            ClientFailedInsertEvent e = internalCommandEvent as ClientFailedInsertEvent;

            HTTPExternalInterface.HandleFailedStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Failed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.OriginalClientState,
                        e.User,
                        e.ConnectionId,
                        e.Id
                    )
                ),
                true //User only?
            );
            return true;
        }

        public bool TranslateAkkaUpdateEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            ClientUpdatedEvent e = internalCommandEvent as ClientUpdatedEvent;

            HTTPExternalInterface.HandleEDStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Processed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.ResultClientState,
                        e.User,
                        e.ConnectionId,
                        e.Id
                    )
                ),
                false //User only?
            );
            return true;
        }

        public bool TranslateAkkaFailedUpdateEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            ClientFailedUpdateEvent e = internalCommandEvent as ClientFailedUpdateEvent;

            HTTPExternalInterface.HandleFailedStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Failed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.OriginalClientState,
                        e.User,
                        e.ConnectionId,
                        e.Id
                    )
                ),
                true //User only?
            );
            return true;
        }

        public bool TranslateAkkaUpsertEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            ClientUpdatedEvent e = internalCommandEvent as ClientUpdatedEvent;

            HTTPExternalInterface.HandleEDStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Processed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.ResultClientState,
                        e.User,
                        e.ConnectionId,
                        e.Id
                    )
                ),
                false //User only?
            );
            return true;
        }

        public bool TranslateAkkaFailedUpsertEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            ClientFailedUpdateEvent e = internalCommandEvent as ClientFailedUpdateEvent;

            HTTPExternalInterface.HandleFailedStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Failed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.OriginalClientState,
                        e.User,
                        e.ConnectionId,
                        e.Id
                    )
                ),
                true //User only?
            );
            return true;
        }

        public bool TranslateAkkaDeleteEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            ClientDeletedEvent e = internalCommandEvent as ClientDeletedEvent;

            HTTPExternalInterface.HandleEDStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Processed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.ResultClientState,
                        e.User,
                        e.ConnectionId,
                        e.Id
                    )
                ),
                false //User only?
            );
            return true;
        }

        public bool TranslateAkkaUnDeleteEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            ClientUnDeletedEvent e = internalCommandEvent as ClientUnDeletedEvent;

            HTTPExternalInterface.HandleEDStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Processed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.ResultClientState,
                        e.User,
                        e.ConnectionId,
                        e.Id
                    )
                ),
                false //User only?
            );
            return true;
        }

        public bool TranslateAkkaFailedDeleteEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            ClientFailedDeleteEvent e = internalCommandEvent as ClientFailedDeleteEvent;

            HTTPExternalInterface.HandleFailedStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Failed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        null,
                        e.User,
                        e.ConnectionId,
                        e.Id
                    )
                ),
                false //User only?
            );
            return true;
        }

        public bool TranslateAkkaFailedUnDeleteEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            ClientFailedUnDeleteEvent e = internalCommandEvent as ClientFailedUnDeleteEvent;

            HTTPExternalInterface.HandleFailedStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Failed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        null,
                        e.User,
                        e.ConnectionId,
                        e.Id
                    )
                ),
                false //User only?
            );
            return true;
        }

        public bool ProcessRequestResponse(Response akkaResponse)
        {
            bool handled = false;

            // Handle Insert
            if (akkaResponse.GetType() == typeof(ClientGetListResponse))
                handled = TranslateAkkaGetListResponseToExternalMessage(akkaResponse as ClientGetListResponse);
            if (akkaResponse.GetType() == typeof(ClientFailedInsertEvent))
                handled = TranslateAkkaFailedGetListResponseToExternalMessage(akkaResponse as ClientGetListResponse);

            return handled;
        }

        public bool TranslateAkkaGetListResponseToExternalMessage(Response akkaResponse)
        {
            ClientGetListResponse response = akkaResponse as ClientGetListResponse;
            ClientGetListRequest request = response.OriginalRequest as ClientGetListRequest;
            HTTPSourcedRequest httpRequest = request.OriginalHTTPRequest;


            HTTPExternalInterface.HandleRequestResponse(new HTTPDestinedRequestResponse(MicroServices.ProcessingStatus.Processed, response.ListOfClientStates, httpRequest), false);
            return true;
        }

        public bool TranslateAkkaFailedGetListResponseToExternalMessage(Response akkaResponse)
        {
            throw new NotImplementedException();
        }

    }
}
