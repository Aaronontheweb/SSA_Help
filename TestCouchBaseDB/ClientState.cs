using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using EY.SSA.CommonBusinessLogic.General;
using Newtonsoft.Json;
namespace EY.SSA.CommonBusinessLogic.State
{
    public class ClientState
    {
        [JsonProperty]
        public string DocumentType { get; protected set; }

        [JsonProperty]
        public string Id { get; private set; }

        //Client Information
        [JsonProperty]
        public string Name { get; set; }

        //StateInfo
        [JsonProperty]
        public bool isActive { get; set; }

        [JsonProperty]
        public string Industry { get; set; }
        [JsonProperty]
        public DateTime AddedOn { get; set; }
        [JsonProperty]
        public string ClientLeadUserId { get; set; }
        [JsonProperty]
        public string ClientContactName { get; set; }
        [JsonProperty]
        public string ClientContactTitle { get; set; }
        [JsonProperty]
        public string ClientContactEmail { get; set; }
        [JsonProperty]
        public string ClientContactPhone { get; set; }
        [JsonProperty]
        public string LastActiveLeadUserId { get; set; }
        [JsonProperty]
        public string LastActiveEngagementId { get; set; }
        [JsonProperty]
        public string LastActiveProjectId { get; set; }
        [JsonProperty]
        public int LastActiveFiscalYear { get; set; }
        [JsonProperty]
        public int LastKnownActiveProjectCount { get; set; }

        [JsonProperty]
        public List<EngagementState> Engagements;

        public ClientState()
        {

        } 

        public ClientState(string id)
        {
            Id = id;
            DocumentType = DocumentTypes.ClientState;
            Industry = "";
            AddedOn = DateTime.Now;
            ClientLeadUserId = "";
            ClientContactName = "";
            ClientContactTitle = "";
            ClientContactEmail = "";
            ClientContactPhone = "";
            LastActiveLeadUserId = "";
            LastActiveEngagementId = "";
            LastActiveProjectId = "";
            LastActiveFiscalYear = DateTime.Now.Year;
            LastKnownActiveProjectCount = 0;
            Engagements = new List<EngagementState>();
            isActive = true;
        }

        public ClientState(string id, ClientState cs)
        {
            Id = id;
            Name = cs.Name;
            DocumentType = DocumentTypes.ClientState;
            Industry = cs.Industry;
            AddedOn = cs.AddedOn;
            ClientLeadUserId = cs.ClientLeadUserId;
            ClientContactName = cs.ClientContactName;
            ClientContactTitle = cs.ClientContactTitle;
            ClientContactEmail = cs.ClientContactEmail;
            ClientContactPhone = cs.ClientContactPhone;
            LastActiveLeadUserId = cs.LastActiveLeadUserId;
            LastActiveEngagementId = cs.LastActiveEngagementId;
            LastActiveProjectId = cs.LastActiveProjectId;
            LastActiveFiscalYear = cs.LastActiveFiscalYear;
            LastKnownActiveProjectCount = cs.LastKnownActiveProjectCount;
            Engagements = cs.Engagements.Clone<List<EngagementState>>();
            isActive = true;
        }
        public ClientState Clone()
        {
            return new ClientState(this.Id, this);
        }


        public static ClientState InstantiateClientState(JObject jo)
        {
            ClientState cs = jo.ToObject<ClientState>();
            return cs;
        }

        internal void Update(ClientState cs)
        {
            Name = cs.Name;
            Industry = cs.Industry;
            ClientLeadUserId = cs.ClientLeadUserId;
            ClientContactName = cs.ClientContactName;
            ClientContactTitle = cs.ClientContactTitle;
            ClientContactEmail = cs.ClientContactEmail;
            ClientContactPhone = cs.ClientContactPhone;
            Engagements = cs.Engagements.Clone<List<EngagementState>>();
        }


    }



}
