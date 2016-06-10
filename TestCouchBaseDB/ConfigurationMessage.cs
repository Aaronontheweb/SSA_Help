using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.Messages.Configuration
{
    abstract class ConfigurationMessage<T>
    {
        public ConfigurationMessage(string parameter, T value)
        {
            Parameter = parameter;
            _Value = value;
        }

        public string Parameter { get; private set; }

        private T _Value;

        public T Value
        {
            get { return _Value; }
        }
    }
}
