using System;

namespace HieSb.Service.Inbound.MeditechHttp.Logic.Data
{
    /// <summary>
    /// Response object for a PDF upload
    /// </summary>
    public class UploadResponse
    {
        #region Public Properties
        /// <summary>
        /// Flag indicating if the process was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Contains any thrown exception
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// The message ID of the CanonicalMessage created
        /// </summary>
        public string ServiceBusMessageID { get; set; }
        #endregion
    }
}
