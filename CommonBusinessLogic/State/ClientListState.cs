﻿using System.Collections.Generic;
using System.Linq;
using EY.SSA.CommonBusinessLogic.Actors;
using Newtonsoft.Json;
using Akka.Actor;

namespace EY.SSA.CommonBusinessLogic.State

{
    public class ClientListState:SimpleState
    {
        public ClientListState(string id, string listName):base(id,listName,ClientListActor.ActorType)
        {
            Items = new Dictionary<string, object>();
        }

        public ClientListItem this[string key]
        {
            // returns value if exists
            get
            {
                return Items.ContainsKey(key) ? (ClientListItem)Items[key] : null;
            }

            // For now if a new "key" is received it will add it.
            // It will update existing "keys".
            // We could add code here to ignore new "keys".  If we do then we need to let the sender know that we are ignoring
            // the "key" because we don't know what it is.  In essence we make the state only work with the "keys" that were added
            // during the SimpleState construction.
            set { Items[key] = (ClientListItem)value; }

        }

        public Dictionary<string, ClientListItem> ClientList {
            get 
            {
                return Items.ToDictionary(k => k.Key, k =>(ClientListItem)k.Value);
            }
        }

        
    }

    public class ClientListItem
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public bool IsActive { get; set; }




        public ClientListItem(string clientPersistenceId,string clientName, bool isActive)
        {
            Id = clientPersistenceId;
            Name = clientName;
            IsActive = isActive;
        }


        public static ClientListItem GenerateClientListItemFromClientState(ClientState cs)
        {
            ClientListItem cli = new ClientListItem(
                cs.Id,
                cs.Name,
                cs.isActive
            );
            return cli;
        }

        public ClientListItem Copy()
        {
            ClientListItem cli = new ClientListItem(
                this.Id,
                this.Name,
                this.IsActive
            );
            return cli;
        }





    }
}
