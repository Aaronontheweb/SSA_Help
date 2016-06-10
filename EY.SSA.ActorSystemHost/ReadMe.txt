About this project:
The main goal of this project is to host the common business logic of the actor system.  This portion of the system is what will maintain state for the system.   
The decision behind placing the bridge actors here was two fold:
1) Isolate the remainder of the actor system from any external communication systems.
2) ability to scale this portion of the system independently of any other parts.
 

When recreating this project you will need certain NUGET packages.  These are:
* Akka.Actor - Actor system
* Akka.Actor.Remote - allows for remote actor systems
* Akka.Logger.NLog - installs logging components

