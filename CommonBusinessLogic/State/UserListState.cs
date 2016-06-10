using System.Collections.Generic;
using System.Linq;
using EY.SSA.CommonBusinessLogic.Actors;
using Newtonsoft.Json;
using Akka.Actor;

namespace EY.SSA.CommonBusinessLogic.State

{
    public class UserListState:SimpleState
    {
        public UserListState(string id, string listName):base(id,listName,UserListActor.ActorType)
        {
            Items = new Dictionary<string, object>();
        }

        public UserListItem this[string key]
        {
            // returns value if exists
            get
            {
                return Items.ContainsKey(key) ? (UserListItem)Items[key] : null;
            }

            // For now if a new "key" is received it will add it.
            // It will update existing "keys".
            // We could add code here to ignore new "keys".  If we do then we need to let the sender know that we are ignoring
            // the "key" because we don't know what it is.  In essence we make the state only work with the "keys" that were added
            // during the SimpleState construction.
            set { Items[key] = (UserListItem)value; }

        }

        public Dictionary<string, UserListItem> UserList {
            get 
            {
                return Items.ToDictionary(k => k.Key, k =>(UserListItem)k.Value);
            }
        }

        
    }

    public class UserListItem
    {
        public string Id { get; set; }

        public string UserName { get; set; }
        public bool IsActive { get; set; }




        public UserListItem(string UserPersistenceId,string userName, bool isActive)
        {
            Id = UserPersistenceId;
            this.UserName = userName;
            IsActive = isActive;
        }


        public static UserListItem GenerateUserListItemFromUserState(UserState user)
        {
            UserListItem cli = new UserListItem(
                user.Id,
                user.UserName,
                user.isActive
            );
            return cli;
        }

        public UserListItem Copy()
        {
            UserListItem cli = new UserListItem(
                this.Id,
                this.UserName,
                this.IsActive
            );
            return cli;
        }





    }
}
