using Akka.Event;
using EY.SSA.CommonBusinessLogic.BridgeInterfaces;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.Messages.Response;
using System;

namespace EY.SSA.CommonBusinessLogic.Actors.Interfaces
{
    public class UserAkkaMessageToExternalMessageTranslator : IAkkaMessagetoExternalActionMessageHandler
    {
        public ILoggingAdapter Logger { get; set; }

        // This is how we call methods on the http client
        private IHTTPExternalInterface HTTPExternalInterface { get; set; }

        public MicroServices.Area Area { get; } = MicroServices.Area.User;

        public UserAkkaMessageToExternalMessageTranslator(IHTTPExternalInterface httpExternalInterface, ILoggingAdapter logger)
        {
            Logger = logger;
            HTTPExternalInterface = httpExternalInterface;
        }



        public bool ProcessCommandEvent(CommandEventMessage eventCommand)
        {
            bool handled = false;

            // Sample code on how to handle custom commands for this area...

            //// Handle Insert
            //if (eventCommand.GetType() == typeof(UserInsertedEvent))
            //    handled = TranslateAkkaInsertEventToExternalMessage(eventCommand as UserInsertedEvent);
            //if(eventCommand.GetType() == typeof(UserFailedInsertEvent))
            //    handled = TranslateAkkaFailedInsertEventToExternalMessage(eventCommand as UserFailedInsertEvent);

            //// Handle Updates
            //if (eventCommand.GetType() == typeof(UserUpdatedEvent))
            //    handled = TranslateAkkaUpdateEventToExternalMessage(eventCommand as UserUpdatedEvent);
            //if (eventCommand.GetType() == typeof(UserFailedUpdateEvent))
            //    handled = TranslateAkkaFailedUpdateEventToExternalMessage(eventCommand as UserFailedUpdateEvent);

            //// Handle Deletes
            //if (eventCommand.GetType() == typeof(UserDeletedEvent))
            //    handled = TranslateAkkaDeleteEventToExternalMessage(eventCommand as UserDeletedEvent);
            //if (eventCommand.GetType() == typeof(UserFailedDeleteEvent))
            //    handled = TranslateAkkaFailedDeleteEventToExternalMessage(eventCommand as UserFailedDeleteEvent);

            if (!handled)
                Logger.Error($"For {Area.ToString()} area received an unknown command.");

            return handled;

        }

        public bool TranslateAkkaInsertEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            UserInsertedEvent e = internalCommandEvent as UserInsertedEvent;

            HTTPExternalInterface.HandleEDStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Processed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.ResultUserState,
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
            UserFailedInsertEvent e = internalCommandEvent as UserFailedInsertEvent;

            HTTPExternalInterface.HandleFailedStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Failed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.OriginalUserState,
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
            UserUpdatedEvent e = internalCommandEvent as UserUpdatedEvent;

            HTTPExternalInterface.HandleEDStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Processed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.ResultUserState,
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
            UserFailedUpdateEvent e = internalCommandEvent as UserFailedUpdateEvent;

            HTTPExternalInterface.HandleFailedStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Failed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.OriginalUserState,
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
            UserUpsertedEvent e = internalCommandEvent as UserUpsertedEvent;

            HTTPExternalInterface.HandleEDStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Processed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.ResultUserState,
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
            UserFailedUpsertEvent e = internalCommandEvent as UserFailedUpsertEvent;

            HTTPExternalInterface.HandleFailedStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Failed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.OriginalUserState,
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
            UserDeletedEvent e = internalCommandEvent as UserDeletedEvent;

            HTTPExternalInterface.HandleEDStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Processed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.ResultUserState,
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
            UserFailedDeleteEvent e = internalCommandEvent as UserFailedDeleteEvent;

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

        public bool TranslateAkkaUnDeleteEventToExternalMessage(CommandEventMessage internalCommandEvent)
        {
            UserUnDeletedEvent e = internalCommandEvent as UserUnDeletedEvent;

            HTTPExternalInterface.HandleEDStateMessage(
                new HTTPDestinedCommandStateEvent(
                    MicroServices.ProcessingStatus.Processed,
                    e.Message,
                    new HTTPSourcedCommand(
                        e.CommandType.ToString(),
                        e.Area.ToString(),
                        null,
                        e.ResultUserState,
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
            UserFailedUnDeleteEvent e = internalCommandEvent as UserFailedUnDeleteEvent;

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
            if (akkaResponse.GetType() == typeof(UserGetListResponse))
                handled = TranslateAkkaGetListResponseToExternalMessage(akkaResponse as UserGetListResponse);
            if (akkaResponse.GetType() == typeof(UserFailedInsertEvent))
                handled = TranslateAkkaFailedGetListResponseToExternalMessage(akkaResponse as UserGetListResponse);

            return handled;
        }

        public bool TranslateAkkaGetListResponseToExternalMessage(Response akkaResponse)
        {
            UserGetListResponse response = akkaResponse as UserGetListResponse;
            UserGetListRequest request = response.OriginalRequest as UserGetListRequest;
            HTTPSourcedRequest httpRequest = request.OriginalHTTPRequest;


            HTTPExternalInterface.HandleRequestResponse(new HTTPDestinedRequestResponse(MicroServices.ProcessingStatus.Processed, response.ListOfUserStates, httpRequest), false);
            return true;
        }

        public bool TranslateAkkaFailedGetListResponseToExternalMessage(Response akkaResponse)
        {
            throw new NotImplementedException();
        }
    }
}
