using EY.SSA.CommonBusinessLogic.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Commands
{
    interface ICommandType
    {
        MicroServices.CommandType CommandType { get; set; }
    }
}
