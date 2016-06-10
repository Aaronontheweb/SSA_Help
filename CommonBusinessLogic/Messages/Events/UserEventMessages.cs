using Akka.Actor;
using EY.SSA.CommonBusinessLogic.Actors;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.State;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.Messages.Events
{
    class UserDeletedEvent : DeleteCommandEventMessage
    {
        public UserDeletedEvent() { }

        public UserDeletedEvent(UserState us, string user, string connectionId)
            : base(us.Id, us.UserName, UserActor.ActorType, MicroServices.Area.User, "User Deleted", us, user, connectionId)
        {
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserState ResultUserState { get { return (UserState)Data; } }
    }

    class UserDeleteRecordedEvent : DeleteCommandEventMessage
    {
        public IActorRef Sender { get; private set; }
        public UserDeleteRecordedEvent() { }

        public UserDeleteRecordedEvent(IActorRef sender,UserDeleteCommand c, string user, string connectionId)
            : base(c.Id, null, UserActor.ActorType, MicroServices.Area.User, "User Delete Recorded", c, user, connectionId)
        {
            Sender = sender;
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserDeleteCommand OriginalCommand { get { return (UserDeleteCommand)Data; } }
    }
    class UserUnDeletedEvent : UnDeleteCommandEventMessage
    {
        public UserUnDeletedEvent() { }

        public UserUnDeletedEvent(UserState us, string user, string connectionId)
            : base(us.Id, us.UserName, UserActor.ActorType, MicroServices.Area.User, "User Activated", us, user, connectionId)
        {
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserState ResultUserState { get { return (UserState)Data; } }
    }
    class UserUnDeleteRecordedEvent : UnDeleteCommandEventMessage
    {
        public IActorRef Sender { get; private set; }

        public UserUnDeleteRecordedEvent() { }

        public UserUnDeleteRecordedEvent(IActorRef sender, UserUnDeleteCommand c, string user, string connectionId)
            : base(c.Id, null, UserActor.ActorType, MicroServices.Area.User, "User Undelete Recorded", c, user, connectionId)
        {
            Sender = sender;
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserUnDeleteCommand OriginalCommand { get { return (UserUnDeleteCommand)Data; } }
    }
    class UserUpdatedEvent : UpdateCommandEventMessage
    {
        public UserUpdatedEvent(){}

        public UserUpdatedEvent(UserState us, string user, string connectionId)
            : base(us.Id, us.UserName, UserActor.ActorType,MicroServices.Area.User, "User Updated", us, user, connectionId)
        {
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserState ResultUserState { get { return (UserState)Data; } }

    }

    class UserUpdateRecordedEvent : UpdateCommandEventMessage
    {
        public IActorRef Sender { get; private set; }

        public UserUpdateRecordedEvent() { }

        public UserUpdateRecordedEvent(IActorRef sender, UserUpdateCommand c, string user, string connectionId)
            : base(c.Id, null, UserActor.ActorType, MicroServices.Area.User, "User Update Recorded", c, user, connectionId)
        {
            Sender = sender;
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserUpdateCommand OriginalCommand { get { return (UserUpdateCommand)Data; } }
    }


    class UserInsertedEvent : InsertCommandEventMessage
    {
        public UserInsertedEvent() { }
        public UserInsertedEvent(UserState us, string user, string connectionId)
            : base(us.Id, us.UserName, UserActor.ActorType, MicroServices.Area.User, "User Inserted", us, user, connectionId)
        {
        }

        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserState ResultUserState { get { return (UserState)Data; } }

    }

    class UserInsertRecordedEvent : InsertCommandEventMessage
    {
        public IActorRef Sender { get; private set; }

        public UserInsertRecordedEvent() { }

        public UserInsertRecordedEvent(IActorRef sender, UserInsertCommand c, string user, string connectionId)
            : base(c.Id, null, UserActor.ActorType, MicroServices.Area.User, "User Insert Recorded", c, user, connectionId)
        {
            Sender = sender;
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserInsertCommand OriginalCommand { get { return (UserInsertCommand)Data; } }
    }


    class UserFailedInsertEvent : FailedInsertCommandEventMessage
    {
        public UserFailedInsertEvent() { }

        public UserFailedInsertEvent(string reason, UserState originalData, string user, string connectionId)
            : base(originalData.Id, originalData.UserName, UserActor.ActorType, MicroServices.Area.User, "User Insert Failed", originalData, user, connectionId)
        {

        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserState OriginalUserState { get { return (UserState)Data; } }

    }

    class UserUpsertedEvent : UpsertCommandEventMessage
    {
        public UserUpsertedEvent() { }
        public UserUpsertedEvent(UserState us, string user, string connectionId)
            : base(us.Id, us.UserName, UserActor.ActorType, MicroServices.Area.User, "User Upserted", us, user, connectionId)
        {
        }

        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserState ResultUserState { get { return (UserState)Data; } }

    }

    class UserUpsertRecordedEvent : UpsertCommandEventMessage
    {
        public IActorRef Sender { get; private set; }

        public UserUpsertRecordedEvent() { }

        public UserUpsertRecordedEvent(IActorRef sender, UserUpsertCommand c, string user, string connectionId)
            : base(c.Id, null, UserActor.ActorType, MicroServices.Area.User, "User Upsert Recorded", c, user, connectionId)
        {
            Sender = sender;
        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserUpsertCommand OriginalCommand { get { return (UserUpsertCommand)Data; } }
    }



    class UserFailedUpsertEvent : FailedUpsertCommandEventMessage
    {
        public UserFailedUpsertEvent() { }

        public UserFailedUpsertEvent(string reason, UserState originalData, string user, string connectionId)
            : base(originalData.Id, originalData.UserName, UserActor.ActorType, MicroServices.Area.User, "User Insert Failed", originalData, user, connectionId)
        {

        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserState OriginalUserState { get { return (UserState)Data; } }

    }
    class UserFailedUpdateEvent : FailedUpdateCommandEventMessage
    {
        public UserFailedUpdateEvent(){}

        public UserFailedUpdateEvent(string reason, UserState originalData, string user, string connectionId)
            : base(originalData.Id, originalData.UserName, UserActor.ActorType,MicroServices.Area.User, "User Update Failed", originalData, user, connectionId)
        {

        }
        [JsonIgnore] // Extremely important to ignore this otherwise JSON.NET will not serialize it properly
        public UserState OriginalUserState { get { return (UserState)Data; }  }
    }
    class UserFailedDeleteEvent : FailedDeleteCommandEventMessage
    {
        public UserFailedDeleteEvent() { }

        public UserFailedDeleteEvent(string reason, UserState originalData, string user, string connectionId)
            : base(originalData.Id, originalData.UserName, UserActor.ActorType, MicroServices.Area.User, reason, originalData, user, connectionId)
        {

        }
        public UserFailedDeleteEvent(string reason, string id, string user, string connectionId)
            : base(id, "", UserActor.ActorType, MicroServices.Area.User, reason, null, user, connectionId)
        {

        }
    }
    class UserFailedUnDeleteEvent : FailedUnDeleteCommandEventMessage
    {
        public UserFailedUnDeleteEvent() { }

        public UserFailedUnDeleteEvent(string reason, string id, string user, string connectionId)
            : base(id, null, UserActor.ActorType, MicroServices.Area.User, reason, null, user, connectionId)
        {

        }
    }
}
