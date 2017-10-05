using System;
using System.Collections.Generic;
using System.Linq;
using HCA.Configuration;
using HCA.Logic;

namespace HieSb.Service.Inbound.MeditechHttp.Logic
{
    [ServiceName("Inbound.MeditechHttp")]
    [ServiceCategory("Inbound")]
    public class MeditechHttpConfigurationOptions: InboundConfigurationOptions
    {
        #region App Settings
        /// <summary>
        /// The port number that should host the API
        /// </summary>
        [AppSetting]
        public int ListeningPort { get; set; }

        /// <summary>
        /// Windows authentication for service
        /// </summary>
        [AppSetting]
        public bool UseWindowsAuth { get; set; }

        /// <summary>
        /// AD groups for users allowed to access the service
        /// </summary>
        public List<string> AllowedUserGroups { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new MeditechHttpConfigurationOptions
        /// </summary>
        /// <param name="configurationManager"></param>
        public MeditechHttpConfigurationOptions(IConfigurationManager configurationManager)
            : base(configurationManager)
        {
            // Manually load AD groups since it needs to be split on the comma
            AllowedUserGroups = configurationManager.GetValue<string>(ServiceName + ".AllowedUserGroups").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        #endregion
    }
}
