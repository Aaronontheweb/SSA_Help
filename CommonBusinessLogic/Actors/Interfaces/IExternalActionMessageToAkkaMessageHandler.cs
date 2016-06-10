using Akka.Actor;
using Akka.Event;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Requests;

namespace EY.SSA.CommonBusinessLogic.Actors.Interfaces
{
    public interface IExternalActionMessageToAkkaMessageHandler
    {

        IActorRef SendTo { get; set; }

        IActorRef ReplyTo { get; set; }

        ILoggingAdapter Logger { get; set; }

        MicroServices.Area Area { get; }


        /// <summary>
        /// Processes the the HTTP Sourced command has a valid command
        /// </summary>
        /// <param name="c"></param>
        /// <param name="cmdType"></param>
        /// <returns></returns>
        bool ProcessCommand(HTTPSourcedCommand externalCommand);

        void TranslateExternalInsertCommandToAkkaMessage(HTTPSourcedCommand externalCommand);
        void TranslateExternalUpsertCommandToAkkaMessage(HTTPSourcedCommand externalCommand);
        void TranslateExternalUpdateCommandToAkkaMessage(HTTPSourcedCommand externalCommand);
        void TranslateExternalDeleteCommandToAkkaMessage(HTTPSourcedCommand externalCommand);
        void TranslateExternalUnDeleteCommandToAkkaMessage(HTTPSourcedCommand externalCommand);

        bool ProcessRequest(HTTPSourcedRequest externalRequest);
        void TranslateExternalGetListRequestToAkkaMessage(HTTPSourcedRequest externalRequest);

        bool ProcessHTTPClientRemoval(RemoveHTTPClient e);


    }
}
