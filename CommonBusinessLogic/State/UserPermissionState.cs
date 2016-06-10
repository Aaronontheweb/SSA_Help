using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using EY.SSA.CommonBusinessLogic.General;
using Newtonsoft.Json;

namespace EY.SSA.CommonBusinessLogic.State
{
    public class UserPermission
    {

        [JsonProperty]
        MicroServices.Area Area = MicroServices.Area.Client;

        [JsonProperty]
        private List<string> Roles;

        [JsonProperty]
        MicroServices.Area SubArea = MicroServices.Area.Client;

        [JsonProperty]
        public Dictionary<string,UserPermission> SubAreaPermissions;

        public UserPermission()
        {

        }

        public UserPermission(MicroServices.Area area, MicroServices.Area subArea, string[] roles)
        {
            Area = area;
            SubArea = subArea;
            Roles = new List<string>(roles);
            SubAreaPermissions = new Dictionary<string, UserPermission>();
        }
        public UserPermission(MicroServices.Area area, string[] roles)
        {
            Area = area;
            SubArea = MicroServices.Area.None;
            Roles = new List<string>(roles);
            SubAreaPermissions = new Dictionary<string, UserPermission>();
        }

        /// <summary>
        /// Performs a deep copy of the user's permissions
        /// </summary>
        /// <returns></returns>
        public UserPermission Copy()
        {
            UserPermission newUserPermission = new UserPermission(this.Area, this.SubArea, this.Roles.ToArray());
            SubAreaPermissions = new Dictionary<string, UserPermission>();
            if (SubAreaPermissions.Count == 0)
                return newUserPermission;
            else
            {
                foreach(KeyValuePair<string,UserPermission> kvp in this.SubAreaPermissions)
                {
                    newUserPermission.SubAreaPermissions.Add(kvp.Key, kvp.Value.Copy());
                }
            }
            return newUserPermission;
        }
    }
}
