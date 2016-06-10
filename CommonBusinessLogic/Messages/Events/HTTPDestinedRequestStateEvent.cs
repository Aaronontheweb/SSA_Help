using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Events
{
    public class HTTPDestinedRequestStateEvent:HTTPDestinedStateEvent
    {
        // - Actions are abstract
        // - Commands is a kind of Action
        // - Requests is a kind of Action


        public HTTPDestinedRequestStateEvent(MicroServices.ProcessingStatus status, string message, HTTPSourcedRequest originalRequest):base(status,message,originalRequest)
        {
            RequestType = originalRequest.RequestType;
        }

        public string RequestType { get; private set; }


    }
}
