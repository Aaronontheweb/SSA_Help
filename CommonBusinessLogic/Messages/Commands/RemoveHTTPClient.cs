using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EY.SSA.CommonBusinessLogic.General;

namespace EY.SSA.CommonBusinessLogic.Messages.Commands
{
    public class RemoveHTTPClient
    {
        public RemoveHTTPClient(string connectionId, string user)
        {
            ConnectionId = connectionId;
            User = user;
        }

        public string ConnectionId { get; private set; }


        public string User { get; private set; }
    }
}
