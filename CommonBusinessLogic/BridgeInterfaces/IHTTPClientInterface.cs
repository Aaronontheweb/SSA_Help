using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Actions;
using EY.SSA.CommonBusinessLogic.Messages.Response;

namespace EY.SSA.CommonBusinessLogic.BridgeInterfaces
{
    public interface IHTTPExternalInterface
    {
        void HandleINGStateMessage(HTTPDestinedStateEvent stateMessage, bool userOnly);
        void HandleEDStateMessage(HTTPDestinedStateEvent stateMessage, bool userOnly);
        void HandleFailedStateMessage(HTTPDestinedStateEvent stateMessage, bool userOnly);


        // TODO create the object to pass back to the browser
        // HTTPDestinedRequestResponse
        void HandleRequestResponse(HTTPDestinedRequestResponse requestResponse, bool userOnly);


        void JoinGroup(string Group, string ConnectionId);
    }
}
