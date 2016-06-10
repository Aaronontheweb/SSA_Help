using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.General
{
    public static class DocumentTypes
    {
        public static string ClientState { get { return "ClientState"; } }
        public static string EngagementState { get { return "EngagementState"; } }
        public static string ClientListState { get { return "ClientListState"; } }
        public static string ProjectState { get { return "ProjectState"; } }
        public static string ProjectListState { get { return "ProjectListState"; } }
        public static string UserState { get { return "UserState"; } }
        public static string ApplicationConfig { get { return "ApplicationConfig"; } }
    }
    public static class UserRoles
    {
        public static string Administrator { get { return "Administrator"; } }
        public static string PowerUser { get { return "PowerUser"; } }
        public static string User { get { return "User"; } }
        public static string GlobalAdministrator { get { return "GlobalAdmistrator"; } }
        public static string GlobalPowerUser { get { return "GlobalPowerUser"; } }
    }

    public static class MicroServices
    {
        public enum Area
        {
            Invalid,
            Supervision,
            Client,
            Project,
            User,
            Config,
            Entity,
            CostPool,
            None
        }

        public enum CommandType
        {
            Invalid,
            Insert,
            Delete,
            Undelete,
            Update,
            Upsert
        }

        public enum ProcessingStatus
        {
            Invalid,
            Processing,
            Processed,
            Failed
        }

        public enum RequestType
        {
            Invalid,
            GetList,
            GetState,
            Subscribe,
            Register
        }

        /// <summary>
        /// Parse a command type string into a CommandType enum value.
        /// </summary>
        /// <param name="sCommandType">Command type string to parse</param>
        /// <param name="cType">CommandType enum value if successful otherwise the CommantType enum value of Invalid is returned.</param>
        /// <returns>True - if string matches an enum type name. False otherwise.</returns>
        public static bool ParseCommandType(string sCommandType, out CommandType cType)
        {

            // Handle Command
            if (Enum.TryParse<CommandType>(sCommandType, true, out cType))
            {
                if (Enum.IsDefined(typeof(MicroServices.CommandType), cType) | cType.ToString().Contains(","))
                {
                    return true;
                }
                else
                {
                    cType = CommandType.Invalid;
                    return false;
                }
            }
            else
            {
                cType = CommandType.Invalid;
                return false;
            }

        }

       
        /// <summary>
        /// Converts a command type string to a CommandType enum value
        /// </summary>
        /// <param name="sCommandType"></param>
        /// <returns>CommandType enum value if the string matches one of the valid value.  Otherwise 'Invalid' CommandType is returned.</returns>
        public static CommandType ParseCommandType(string sCommandType)
        {
            MicroServices.CommandType cType;
            // Handle Command
            ParseCommandType(sCommandType, out cType);
            return cType;

        }


        /// <summary>
        /// Parses the string sArea to an Area enum type.
        /// </summary>
        /// <param name="sArea">string containing an area</param>
        /// <param name="area">Valid Area enum if the string matches one of the standard enum value.  Invalid otherwise.</param>
        /// <returns>True if parse was successful, otherwise false.</returns>
        public static bool ParseArea(string sArea, out Area area)
        {

            // Handle Area
            if (Enum.TryParse<Area>(sArea, true, out area))
            {
                if (Enum.IsDefined(typeof(MicroServices.Area), area) | area.ToString().Contains(","))
                {
                    return true;
                }
                else
                {
                    area = Area.Invalid;
                    return false;
                }
            }
            else
            {
                area = Area.Invalid;
                return false;
            }

        }

        /// <summary>
        /// Parses the string sArea to an Area enum type.
        /// </summary>
        /// <param name="sArea">string containing an area</param>
        /// <returns>Valid Area enum if the string matches one of the standard enum value.  Invalid otherwise.</returns>
        public static Area ParseArea(string sArea)
        {
            MicroServices.Area area;
            ParseArea(sArea,out area);
            return area;

        }

        /// <summary>
        /// Parses the string sRequest to a Request enum type.
        /// </summary>
        /// <param name="sRequestType">string containing a request</param>
        /// <param name="requestType">Valid Request enum if the string matches one of the standard enum value.  Invalid otherwise.</param>
        /// <returns>True if parse was successful, otherwise false.</returns>
        public static bool ParseRequestType(string sRequestType, out RequestType requestType)
        {

            // Handle Command
            if (Enum.TryParse<RequestType>(sRequestType, true, out requestType))
            {
                if (Enum.IsDefined(typeof(MicroServices.RequestType), requestType) | requestType.ToString().Contains(","))
                {
                    return true;
                }
                else
                {
                    requestType = RequestType.Invalid;
                    return false;
                }
            }
            else
            {
                requestType = RequestType.Invalid;
                return false;
            }

        }

        /// <summary>
        /// Parses the string sRequestType to an RequestType enum type.
        /// </summary>
        /// <param name="sRequestType">string containing an request type</param>
        /// <returns>Valid RequestType enum if the string matches one of the standard enum value.  Invalid otherwise.</returns>
        public static RequestType ParseRequestType(string sRequestType)
        {
            MicroServices.RequestType requestType;
            ParseRequestType(sRequestType, out requestType);
            return requestType;

        }

    }
}
