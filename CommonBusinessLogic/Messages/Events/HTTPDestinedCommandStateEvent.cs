using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Events
{
    public class HTTPDestinedCommandStateEvent:HTTPDestinedStateEvent
    {
        // - Actions are abstract
        // - Commands is a kind of Action
        // - Requests is a kind of Action


        public HTTPDestinedCommandStateEvent(MicroServices.ProcessingStatus status, string message, HTTPSourcedCommand originalCommand):base(status,message,originalCommand)
        {
            Action = originalCommand.CommandType;
            Data = originalCommand.Data;

        }

        public string Action { get; private set; }

        public object Data { get; private set; }
    }
}
