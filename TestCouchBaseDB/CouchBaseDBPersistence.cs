using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;

namespace TestCouchBaseDB
{
    class CouchBaseDBPersistence
    {
        public static readonly CouchBaseDBPersistence Instance = new CouchBaseDBPersistence();

        public CouchBaseDBPersistence() { }

        public static Config DefaultConfiguration()
        {
            return ConfigurationFactory.FromResource<CouchBaseDBPersistence>("TestCouchBaseDB.reference.conf");
        }

    }
}
