using Akka.Actor;
using Akka.Event;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.Messages.Response;

namespace EY.SSA.CommonBusinessLogic.Actors.Interfaces
{
    public interface IAkkaMessagetoExternalActionMessageHandler
    {


        ILoggingAdapter Logger { get; set; }

        MicroServices.Area Area { get; }



        //Events from commands
        bool ProcessCommandEvent(CommandEventMessage externalCommand);

        bool TranslateAkkaInsertEventToExternalMessage(CommandEventMessage internalCommandEvent);
        bool TranslateAkkaFailedInsertEventToExternalMessage(CommandEventMessage internalCommandEventEvent);
        bool TranslateAkkaUpdateEventToExternalMessage(CommandEventMessage internalCommandEvent);
        bool TranslateAkkaFailedUpdateEventToExternalMessage(CommandEventMessage internalCommandEvent);
        bool TranslateAkkaUpsertEventToExternalMessage(CommandEventMessage internalCommandEvent);
        bool TranslateAkkaFailedUpsertEventToExternalMessage(CommandEventMessage internalCommandEvent);
        bool TranslateAkkaDeleteEventToExternalMessage(CommandEventMessage internalCommandEvent);
        bool TranslateAkkaUnDeleteEventToExternalMessage(CommandEventMessage internalCommandEvent);
        bool TranslateAkkaFailedDeleteEventToExternalMessage(CommandEventMessage internalCommandEvent);
        bool TranslateAkkaFailedUnDeleteEventToExternalMessage(CommandEventMessage internalCommandEvent);


        // Responses to requests
        bool ProcessRequestResponse(Response akkaResponse);

        bool TranslateAkkaGetListResponseToExternalMessage(Response akkaResponse);
        bool TranslateAkkaFailedGetListResponseToExternalMessage(Response akkaResponse);



    }
}
