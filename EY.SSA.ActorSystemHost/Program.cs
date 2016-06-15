using Akka.Actor;
using NLog;
using System;
//using Couchbase;
using EY.SSA.CommonBusinessLogic;

namespace EY.SSA.ActorSystemHost
{
    class Program
    {
        //NLog
        public static Logger ApplicationLogger;


        static void Main(string[] args)
        {
            try
            {
                ApplicationLogger = NLog.LogManager.GetCurrentClassLogger();

            }
            catch (Exception ex)
            {
                ColorConsole.WriteLineRed("There seems to be a problem initializing the logger. Error message:{0} Inner Exception:{1}", ex.Message, ex.InnerException.Message ?? "");
                return;
            }
            string assembly = "C# Build: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            ApplicationLogger.Info("Version:" + assembly);

            //initialize the ClusterHelper
            //ClusterHelper.Initialize("couchbaseClients/couchbase");
            
            
            // Instantiate the actor system
            SSAActorSystem.Create();

            ApplicationLogger.Info("Host Actor System running on {0}", System.AppDomain.CurrentDomain.FriendlyName);
            string command = "";


            while (command != "exit")
            {
                Console.WriteLine("Enter a command:");
                command = Console.ReadLine(); // holds the app from closing

                if(command == "PrintSupervisorList")
                {
                    SSAActorSystem.ActorReferences.SupervisorRegistry.Tell("PrintSupervisorList",ActorRefs.Nobody);
                }

                ApplicationLogger.Info("User entered command line command:{0}", command);
            }


            ApplicationLogger.Info("Host Actor System on {0} is shutting down.", System.AppDomain.CurrentDomain.FriendlyName);
            // Shutdown the actor system
            SSAActorSystem.Shutdown();
            ApplicationLogger.Info("Host Actor System on {0} is shut down.", System.AppDomain.CurrentDomain.FriendlyName);

            // Close the Cluster Helper
            //ClusterHelper.Close();


        }

 
    }
}
