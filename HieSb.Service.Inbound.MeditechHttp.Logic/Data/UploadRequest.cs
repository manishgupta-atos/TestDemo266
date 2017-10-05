using System.Collections.Generic;

namespace HieSb.Service.Inbound.MeditechHttp.Logic.Data
{
    /// <summary>
    /// Request object for a PDF upload
    /// </summary>
    public class UploadRequest
    {
        #region Public Properties
        /// <summary>
        /// Username of the identity that uploaded the file
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// IP Address of the sending system
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// URL of the request
        /// </summary>
        public string RequestUri { get; set; }

        /// <summary>
        /// Payload of the PDF file that was uploaded to the service
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// List of key-value pairs of custom data
        /// </summary>
        public List<KeyValuePair<string, string>> Properties { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new UploadRequest
        /// </summary>
        public UploadRequest()
        {
            // Initialize the array of custom properties
            Properties = new List<KeyValuePair<string, string>>();
        }
        #endregion
    }
}
