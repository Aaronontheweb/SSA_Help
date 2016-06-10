using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Configuration
{
    class SetSnapshotTriggerCount:ConfigurationMessage<int>
    {
        public SetSnapshotTriggerCount(int maxEvents):base("SnapshotTriggerCount",maxEvents)
        {

        }

    }
}
