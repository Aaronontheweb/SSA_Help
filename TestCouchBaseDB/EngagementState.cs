using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EY.SSA.CommonBusinessLogic.State
{
    public class EngagementState
    {
        /// <summary>
        /// Initializes the a engagement state object (key-value pairs).
        /// </summary>
        /// <param name="documentType">Specifies the type of document which uniquely represents this simple state.</param>
        /// <param name="InitializerDictionary">Dictionary of objects which specify the properties(key-value pairs) that will be tracked by this SimpleState</param>
        public EngagementState(string id, string name)
        {
            EngagementId = id;
            EngagementName = name;
        }

        /// <summary>
        /// Default constructor - needed by JSON.Net
        /// </summary>
        public EngagementState(){
        }



        public string DocumentType { get; private set; } = General.DocumentTypes.EngagementState;

        public string EngagementId { get;  set; }

        //Client Information
        public string EngagementName { get; set; }

        //StateInfo
        public bool isActive { get; set; }

        public bool RequiredFieldsComplete { get; set; }

        public string EngagmentCode { get; set; }
        public int EngagmentProjectsCount { get; set; }
        public string EngagementDescription { get; set; }
        public string ContactFirstName { get; set; }
        public string ContactLastName { get; set; }
        public string Title { get; set; }
        public string EMail { get; set; }
        public string Phone { get; set; }

    }

}