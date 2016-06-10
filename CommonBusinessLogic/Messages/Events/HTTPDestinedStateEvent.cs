using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Actions;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Events
{
    public abstract class HTTPDestinedStateEvent
    {

        // - Actions are abstract
        // - Commands is a kind of Action
        // - Requests is a kind of Action

        public HTTPDestinedStateEvent(MicroServices.ProcessingStatus status, string message, HTTPSourcedAction originalAction)
        {
            ConnectionId = originalAction.ConnectionId;
            Action = originalAction.Action;
            User = originalAction.User;
            Area = originalAction.Area;
            Message = message;
            Status = status;
        }

        public string Message { get; private set; }

        public string Action { get; private set; }

        public MicroServices.ProcessingStatus Status { get; private set; }

        public string Area { get; private set; }

        public string ConnectionId { get; private set; }

        public string User { get; private set; }


    }
}
