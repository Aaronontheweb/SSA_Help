using System.Net;
using System.Collections.Generic;
using Akka.Actor;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.Messages.Events;

namespace EY.SSA.ActorSystemHost
{
    public static class SSAActorSystem
    {

        private static ActorSystem _ActorSystem;
        private static string ActorSystemName = "SSAActorSystem";
        private static string HostName = Dns.GetHostName();


        public static void Create()
        {

            // Instantiate the actor system
            Program.ApplicationLogger.Info("Starting SSA Actor System at ActorSystemHost at:{0}",HostName);
            _ActorSystem = Akka.Actor.ActorSystem.Create(SSAActorSystem.ActorSystemName);
            Program.ApplicationLogger.Info("Started SSA Actor System at ActorSystemHost at:{0}", HostName);


            Program.ApplicationLogger.Info("Instantiating SupervisorRegistryActor");
            ActorReferences.SupervisorRegistry = _ActorSystem.ActorOf(Props.Create<SupervisorRegistryActor>(), "SupervisorRegistry");
            Program.ApplicationLogger.Info("Instantiated SupervisorRegistryActor");


            Program.ApplicationLogger.Info("Instantiating Client Supervisor.");
            ActorReferences.SupervisorActors.Add(_ActorSystem.ActorOf(Props.Create<ClientSupervisor>(), "ClientSupervisor"));
            Program.ApplicationLogger.Info("Instantiated Client Supervisor.");

            //Program.ApplicationLogger.Info("Instantiating User Supervisor.");
            //ActorReferences.SupervisorActors.Add(_ActorSystem.ActorOf(Props.Create<UserSupervisor>(), "UserSupervisor"));
            //Program.ApplicationLogger.Info("Instantiated User Supervisor.");

            foreach (IActorRef supervisorActor in ActorReferences.SupervisorActors)
                supervisorActor.Tell(new SupervisorRegistryReady(ActorReferences.SupervisorRegistry));

        }

        public static void Shutdown()
        {
            // Gracefully shutdown the actor system
            _ActorSystem.Terminate();

        }

        public static class ActorReferences
        {
            public static HashSet<IActorRef> SupervisorActors = new HashSet<IActorRef>();
            public static IActorRef SupervisorRegistry { get; set; }
        }

    }
}
