using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.State
{
    /// <summary>
    /// This class serves as a template to hold an actor's simple state.  By simple state it means it holds a series of key/value
    /// pairs.  It is not designed to have nested values - although possible.
    /// </summary>
    public abstract class SimpleState
    {
        /// <summary>
        /// Initializes the a simple state object (key-value pairs).
        /// </summary>
        /// <param name="documentType">Specifies the type of document which uniquely represents this simple state.</param>
        /// <param name="InitializerDictionary">Dictionary of objects which specify the properties(key-value pairs) that will be tracked by this SimpleState</param>
        protected SimpleState(string id,string name, string documentType)
        {
            DocumentType = documentType;
            Items = new Dictionary<string,object>();
            Id = id;
            Name = name;

            
        }
        public string DocumentType { get; private set; }

        public string Id { get; private set; }

        //Client Information
        public string Name { get; set; }

        //StateInfo
        public bool isActive { get; set; }

        [JsonProperty]
        public IDictionary<string,object> Items { get; protected set; }

        // Provides an indexer capability to the class
        public object this[string key]
        {
            // returns value if exists
            get 
            { 
                return Items.ContainsKey(key)?Items[key]:null; 
            }

            // For now if a new "key" is received it will add it.
            // It will update existing "keys".
            // We could add code here to ignore new "keys".  If we do then we need to let the sender know that we are ignoring
            // the "key" because we don't know what it is.  In essence we make the state only work with the "keys" that were added
            // during the SimpleState construction.
            set { Items[key] = value; }
        }
    }

    public static class DictionaryExtenions
    {
        public static T Get<T>(this IDictionary<string, object> instance, string name)
        {
            if (instance.ContainsKey(name))
                return (T)instance[name];
            else
                return default(T);
        }

        public static T Get<T>(this SimpleState instance,string name)
        {
            return instance.Items.Get<T>(name);
        }

    }
}
