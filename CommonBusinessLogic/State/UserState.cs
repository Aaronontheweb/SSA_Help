using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using EY.SSA.CommonBusinessLogic.General;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.State
{
    public class UserState
    {
        [JsonProperty]
        public string DocumentType { get; protected set; }

        [JsonProperty]
        public string Id { get; private set; }

        //User Information
        [JsonProperty]
        public string FirstName { get; set; }

        [JsonProperty]
        public string UserName { get; set; }

        [JsonProperty]
        public string MiddleName { get; set; }

        [JsonProperty]
        public string LastName { get; set; }

        [JsonProperty]
        public string Title { get; set; }

        [JsonProperty]
        public string Email { get; set; }

        [JsonProperty]
        public string Phone { get; set; }

        [JsonProperty]
        public string AvatarURL { get; set; }

        [JsonProperty]
        public bool DefaultPermissions { get; set; }

        [JsonProperty]
        public string GlobalRole { get; set; }

        [JsonProperty]
        public string DefaultRole { get; set; }

        //StateInfo
        [JsonProperty]
        public bool isActive { get; set; }

        [JsonProperty]
        public string Industry { get; set; }

        [JsonProperty]
        public DateTime AddedOn { get; set; }

        [JsonProperty]
        public DateTime LastLoggedOn { get; set; }

        [JsonProperty]
        public string LastActiveEngagementId { get; set; }

        [JsonProperty]
        public string LastActiveProjectId { get; set; }

        [JsonProperty]
        public int LastKnownActiveProjectCount { get; set; }

        [JsonProperty]
        public Dictionary<string,UserPermission> Permissions;

        /// <summary>
        /// Default Constructor - Do not use.
        /// </summary>
        public UserState()
        {

        }

        /// <summary>
        /// Use this constructor when instantinating a new virgin user object.
        /// </summary>
        /// <param name="id"></param>
        public UserState(string id)
        {
            Id = id;
            DocumentType = DocumentTypes.UserState;
            Permissions = new Dictionary<string, UserPermission>();
            isActive = true;
        }

        public UserState(string id, UserState us)
        {
            Id = id;
            FirstName = us.FirstName;
            MiddleName = us.MiddleName;
            LastName = us.LastName;
            Title = us.Title;
            Email = us.Email;
            Phone = us.Phone;
            AvatarURL = us.AvatarURL;
            DefaultPermissions = us.DefaultPermissions;
            GlobalRole = us.GlobalRole;
            DefaultRole = us.DefaultRole;

            isActive = true;
            Industry = us.Industry;
            AddedOn = us.AddedOn;
            LastLoggedOn = us.LastLoggedOn;
            LastActiveEngagementId = us.LastActiveEngagementId;
            LastActiveProjectId = us.LastActiveProjectId;
            LastKnownActiveProjectCount = us.LastKnownActiveProjectCount;
            Permissions = new Dictionary<string, UserPermission>();
            foreach (KeyValuePair<string, UserPermission> kvp in us.Permissions)
            {
                Permissions.Add(kvp.Key, kvp.Value.Copy());
            }
        }
        public UserState Clone()
        {
            return new UserState(this.Id, this);
        }


        public static UserState InstantiateUserState(JObject jo)
        {
            UserState us = jo.ToObject<UserState>();
            return us;
        }

        internal void Update(UserState us)
        {
            FirstName = us.FirstName;
            MiddleName = us.MiddleName;
            LastName = us.LastName;
            Title = us.Title;
            Email = us.Email;
            Phone = us.Phone;
            AvatarURL = us.AvatarURL;
            DefaultPermissions = us.DefaultPermissions;
            GlobalRole = us.GlobalRole;
            DefaultRole = us.DefaultRole;

            isActive = us.isActive;
            Industry = us.Industry;
            AddedOn = us.AddedOn;
            LastLoggedOn = us.LastLoggedOn;
            LastActiveEngagementId = us.LastActiveEngagementId;
            LastActiveProjectId = us.LastActiveProjectId;
            LastKnownActiveProjectCount = us.LastKnownActiveProjectCount;
            Permissions = new Dictionary<string, UserPermission>();
            foreach (KeyValuePair<string, UserPermission> kvp in us.Permissions)
            {
                Permissions.Add(kvp.Key, kvp.Value.Copy());
            }
        }
    }
}
