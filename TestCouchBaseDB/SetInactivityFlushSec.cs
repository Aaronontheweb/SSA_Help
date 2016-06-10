using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Configuration
{
    class SetInactivityFlushSec : ConfigurationMessage<int>
    {
        public SetInactivityFlushSec(int maxEvents)
            : base("InactivityFlushSec", maxEvents)
        {

        }

    }
}
