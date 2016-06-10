using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Actions;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Response
{
    public class HTTPDestinedRequestResponse
    {

        // - Actions are abstract
        // - Commands is a kind of Action
        // - Requests is a kind of Action

        public HTTPDestinedRequestResponse(MicroServices.ProcessingStatus status, object responseData, HTTPSourcedRequest originalAction)
        {
            ConnectionId = originalAction.ConnectionId;
            Action = originalAction.RequestType;
            User = originalAction.User;
            Area = originalAction.Area;
            Status = status.ToString();
            Data = responseData;
        }


        public string Action { get; private set; }

        public string Status { get; private set; }

        public string Area { get; private set; }

        public string ConnectionId { get; private set; }

        public string User { get; private set; }
        public object Data { get; private set; }
    }
}
