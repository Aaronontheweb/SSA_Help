using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using Akka.Actor;
using Akka.Configuration;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core.Serialization;

using NLog;
using System.Diagnostics;
using Newtonsoft.Json;
using Couchbase.Management;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Actors;

namespace TestCouchBaseDB
{
    class Program
    {

        //NLog
        private static Logger ApplicationLogger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {

            var _ActorSystem = ActorSystem.Create("SSAActorSystem");

            Program.ApplicationLogger.Info("Instantiating SupervisorRegistryActor");
            ActorReferences.SupervisorRegistry = _ActorSystem.ActorOf(Props.Create<SupervisorRegistryActor>(), "SupervisorRegistry");
            Program.ApplicationLogger.Info("Instantiated SupervisorRegistryActor");



            Program.ApplicationLogger.Info("Instantiating Client Supervisor.");
            ActorReferences.SupervisorActors.Add(_ActorSystem.ActorOf(Props.Create<ClientSupervisor>(), "ClientSupervisor"));
            Program.ApplicationLogger.Info("Instantiated Client Supervisor.");

            foreach (IActorRef supervisorActor in ActorReferences.SupervisorActors)
                supervisorActor.Tell(new SupervisorRegistryReady(ActorReferences.SupervisorRegistry));

            string command = "";

            while (command != "exit")
            {
                Console.WriteLine("Enter a command:");
                command = Console.ReadLine(); // holds the app from closing

                if (command == "PrintSupervisorList")
                {
                    ActorReferences.SupervisorRegistry.Tell("PrintSupervisorList", ActorRefs.Nobody);
                }

                ApplicationLogger.Info("User entered command line command:{0}", command);
            }

        }



    }

    public static class ActorReferences
    {
        public static HashSet<IActorRef> SupervisorActors = new HashSet<IActorRef>();
        public static IActorRef SupervisorRegistry { get; set; }
    }

}
