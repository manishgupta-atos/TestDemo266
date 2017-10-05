using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;

using HCA.Logger;

using HieSb.Core.Data.Message;
using HieSb.Core.Data.Patient;
using HieSb.Service.Workflow.RoutingResults;

using HieSb.Service.Inbound.MeditechHttp.Logic.Data;
using HieSb.Service.Inbound.MeditechHttp.Logic.Exceptions;

namespace HieSb.Service.Inbound.MeditechHttp.Logic
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class MeditechHttpLogic : InboundLogic<MeditechHttpConfigurationOptions>
    {
        #region Constructor
        public MeditechHttpLogic(MeditechHttpConfigurationOptions configurationOptions, ILogger logger, Action<Exception> shutdownAction)
            : base(configurationOptions, logger, shutdownAction)
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Processes a PDF file upload 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<UploadResponse> ProcessUploadAsync(UploadRequest request)
        {
            // Create a CanonicalMessage to use with auditing
            var canonicalMessage = new CanonicalMessage(ServiceName, HieWorkflow.Outbound);

            // Performance log the action
            using (CreatePerformanceMarker("ProcessMessage", canonicalMessage))
            {
                try
                {
                    // Archive the raw message
                    await ArchiveStepAsync(canonicalMessage, "HTTP Post", "Received the HTTP Post", true);

                    // Construct a header string for logging
                    var headerString = "";

                    if (request != null && request.Properties != null)
                    {
                        headerString = string.Join(", ", request?.Properties.Select(x => x.Key + " = " + x.Value));
                    }

                    // Log the IP and the Headers
                    LogDebug($"The HTTP Post was received from IP Address: {request?.IpAddress} with the following HTTP Headers: {headerString}", canonicalMessage);

                    // Validate basic parts of the upload request
                    ValidateBasicRequest(request);

                    // Authorize the user
                    if (ConfigurationOptions.UseWindowsAuth)
                    {
                        AuthorizeUser(request.Username);
                    }

                    // Check for the HTTP Header MeditechNoteType set to OB
                    if (request.Properties.Contains(new KeyValuePair<string, string>("MeditechNoteType", "OB")))
                    {
                        // This is an Obstetrician Note
                        return await ProcessObstetricianNoteAsync(request, canonicalMessage);
                    }
                    else
                    {
                        // Otherwise this is a Progress Note
                        return await ProcessProgressNoteAsync(request, canonicalMessage);
                    }
                }
                catch (Exception ex)
                {
                    // Archive the error
                    await ArchiveErrorAsync(canonicalMessage, ex, "");

                    // Log Exception
                    LogError($"An error occurred while trying to process the upload. {HCA.Exceptions.ExceptionUtilities.GetString(ex)}", canonicalMessage);

                    // Create a new response
                    return new UploadResponse()
                    {
                        Success = false,
                        Exception = ex
                    };
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Authorize the user
        /// </summary>
        /// <param name="username"></param>
        private void AuthorizeUser(string username)
        {
            try
            {
                // Create a variable to hold the results
                var wasUserFound = false;

                // Open a connection to AD
                using (var context = new PrincipalContext(ContextType.Domain))
                {
                    // Search for the identity
                    var principal = UserPrincipal.FindByIdentity(context, username);

                    // Loop through each group
                    foreach (var groupName in ConfigurationOptions.AllowedUserGroups)
                    {
                        // Get the group's AD object
                        var groupObject = GroupPrincipal.FindByIdentity(context, groupName);

                        // List all members (since principal.IsMemberOf() doesn't do recursion)
                        var allGroupMembers = groupObject.GetMembers(true);

                        // Check if the current user exists in the list
                        if (allGroupMembers.Contains(principal))
                        {
                            // Set the result variable
                            wasUserFound = true;

                            // Break out of the loop
                            break;
                        }
                    }
                }

                // Check if the user was found in any group
                if (!wasUserFound)
                {
                    // Since the user was not found, throw an exception
                    throw new UserNotAuthorizedException(username, ConfigurationOptions.AllowedUserGroups, null);
                }
            }
            catch (Exception ex)
            {
                // Rethrow new exception
                throw new UserNotAuthorizedException(username, ConfigurationOptions.AllowedUserGroups, ex);
            }
        }

        /// <summary>
        /// Validates basic information about the upload request
        /// </summary>
        /// <param name="request"></param>
        private void ValidateBasicRequest(UploadRequest request)
        {
            // Check that a request was sent
            if (request == null)
            {
                throw new InvalidRequestException("The upload request cannot be null.");
            }

            // Check the payload
            if (request.Payload == null || request.Payload.Length == 0)
            {
                throw new InvalidRequestException("The upload request's payload must contain binary data.");
            }

            // Check the properties
            if (request.Properties == null || !request.Properties.Any())
            {
                throw new InvalidRequestException("The upload request's properties must contain values.");
            }

            // Check the request URI
            if (string.IsNullOrWhiteSpace(request.RequestUri))
            {
                throw new InvalidRequestException("The upload request's URI cannot be null.");
            }

            // Check the username
            if (ConfigurationOptions.UseWindowsAuth && string.IsNullOrWhiteSpace(request.Username))
            {
                throw new InvalidRequestException("The upload request's Username cannot be null.");
            }
        }

        /// <summary>
        /// Processes a Progress Note type PDF file
        /// </summary>
        /// <param name="request"></param>
        /// <param name="canonicalMessage"></param>
        /// <returns></returns>
        private async Task<UploadResponse> ProcessProgressNoteAsync(UploadRequest request, CanonicalMessage canonicalMessage)
        {
            // Log
            LogInfo("Beginning processing of the Progress Note", canonicalMessage);

            // Extract the header values
            var headerRegion = request.Properties.FirstOrDefault(x => x.Key.ToLowerInvariant() == "region").Value;
            var headerId = request.Properties.FirstOrDefault(x => x.Key.ToLowerInvariant() == "id").Value;
            var headerDateTime = request.Properties.FirstOrDefault(x => x.Key.ToLowerInvariant() == "datetime").Value;
            var headerAuthor = request.Properties.FirstOrDefault(x => x.Key.ToLowerInvariant() == "author").Value;
            var headerDocumentType = request.Properties.FirstOrDefault(x => x.Key.ToLowerInvariant() == "documenttype").Value;

            // Validate the header values
            if (string.IsNullOrWhiteSpace(headerRegion))
            {
                throw new InvalidRequestException("The header 'Region' is required.");
            }

            if (string.IsNullOrWhiteSpace(headerId))
            {
                throw new InvalidRequestException("The header 'Id' is required.");
            }

            if (string.IsNullOrWhiteSpace(headerDateTime))
            {
                throw new InvalidRequestException("The header 'DateTime' is required.");
            }
            else
            {
                // Create variable to hold parsed value
                DateTime parsedDateTime;

                // Attempt to parse the DateTime in an exact format
                if (!DateTime.TryParseExact(headerDateTime, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out parsedDateTime))
                {
                    throw new InvalidRequestException($"The header 'DateTime' had value '{headerDateTime} which could not be parsed as 'yyyyMMddHHmmss'.");
                }
            }

            if (string.IsNullOrWhiteSpace(headerAuthor))
            {
                throw new InvalidRequestException("The header 'Author' is required.");
            }

            if (string.IsNullOrWhiteSpace(headerDocumentType))
            {
                throw new InvalidRequestException("The header 'DocumentType' is required.");
            }
            else if (headerDocumentType.ToLowerInvariant() != "pdf")
            {
                throw new InvalidRequestException($"The header 'DocumentType' had unknown value of '{headerDocumentType}'.");
            }

            // Construct the CanonicalMessage

            // Add the properties
            canonicalMessage.Properties.Add("Author", headerAuthor);
            canonicalMessage.Properties.Add("ConfidentialityFlag", "N");
            canonicalMessage.Properties.Add("DocumentType", "PDF");
            canonicalMessage.Properties.Add("MeditechNoteType", "PN");
            canonicalMessage.Properties.Add("EncounterDate", headerDateTime);
            canonicalMessage.Properties.Add("Region", headerRegion);
            canonicalMessage.Properties.Add("EncounterID", headerId + headerDateTime);

            // Add the payload
            canonicalMessage.Payload = new Payload(request.Payload)
            {
                PayloadType = PayloadTypes.PDF
            };

            // Add the payload properties
            canonicalMessage.Payload.Properties.Add("OriginalFilePath", request.RequestUri);

            // Add a master identifier to the patient
            canonicalMessage.Payload.Patient = new CanonicalPatient()
            {
                MasterIdentifier = new PatientIdentifier()
                {
                    ID = headerId,
                    Domain = headerRegion
                }
            };

            // Archive the message
            await ArchiveStepAsync(canonicalMessage, "Parsed Progress Note", "The Progress Note upload request was parsed.", true);

            // Send the message to the queue
            PutQueueMessage(canonicalMessage);

            // Log the message success
            LogMessageSuccess(canonicalMessage);

            // Log
            LogInfo("Successfully completed the Progress Note processing.", canonicalMessage);

            // Return a success
            return new UploadResponse()
            {
                Success = true,
                ServiceBusMessageID = canonicalMessage.ServiceBusMessageID.ToString()
            };
        }

        /// <summary>
        /// Processes an Obstetrician Note type PDF file
        /// </summary>
        /// <param name="request"></param>
        /// <param name="canonicalMessage"></param>
        /// <returns></returns>
        private async Task<UploadResponse> ProcessObstetricianNoteAsync(UploadRequest request, CanonicalMessage canonicalMessage)
        {
            // Log
            LogInfo("Beginning processing of the Obstetrician Note", canonicalMessage);

            // Extract the header values
            var headerRegion = request.Properties.FirstOrDefault(x => x.Key.ToLowerInvariant() == "region").Value;
            var headerId = request.Properties.FirstOrDefault(x => x.Key.ToLowerInvariant() == "id").Value;
            var headerPregnancyId = request.Properties.FirstOrDefault(x => x.Key.ToLowerInvariant() == "pregnancyid").Value;
            var headerMeditechNoteType = request.Properties.FirstOrDefault(x => x.Key.ToLowerInvariant() == "meditechnotetype").Value;
            var headerDocumentType = request.Properties.FirstOrDefault(x => x.Key.ToLowerInvariant() == "documenttype").Value;

            // Validate the header values
            if (string.IsNullOrWhiteSpace(headerRegion))
            {
                throw new InvalidRequestException("The header 'Region' is required.");
            }

            if (string.IsNullOrWhiteSpace(headerId))
            {
                throw new InvalidRequestException("The header 'Id' is required.");
            }

            if (string.IsNullOrWhiteSpace(headerPregnancyId))
            {
                throw new InvalidRequestException("The header 'PregnancyID' is required.");
            }

            if (string.IsNullOrWhiteSpace(headerMeditechNoteType))
            {
                throw new InvalidRequestException("The header 'MeditechNoteType' is required.");
            }

            if (string.IsNullOrWhiteSpace(headerDocumentType))
            {
                throw new InvalidRequestException("The header 'DocumentType' is required.");
            }
            else if (headerDocumentType.ToLowerInvariant() != "pdf")
            {
                throw new InvalidRequestException($"The header 'DocumentType' had unknown value of '{headerDocumentType}'.");
            }

            // Construct the CanonicalMessage

            // Add the properties
            canonicalMessage.Properties.Add("ConfidentialityFlag", "N");
            canonicalMessage.Properties.Add("DocumentType", "PDF");
            canonicalMessage.Properties.Add("MeditechNoteType", "OB");
            canonicalMessage.Properties.Add("Region", headerRegion);
            canonicalMessage.Properties.Add("PregnancyID", headerPregnancyId);

            // Add the payload
            canonicalMessage.Payload = new Payload(request.Payload)
            {
                PayloadType = PayloadTypes.PDF
            };

            // Add the payload properties
            canonicalMessage.Payload.Properties.Add("OriginalFilePath", request.RequestUri);

            // Add a master identifier to the patient
            canonicalMessage.Payload.Patient = new CanonicalPatient()
            {
                MasterIdentifier = new PatientIdentifier()
                {
                    ID = headerId,
                    Domain = headerRegion
                }
            };

            // Archive the message
            await ArchiveStepAsync(canonicalMessage, "Parsed Obstetrician Note", "The Obstetrician Note upload request was parsed.", true);

            // Send the message to the queue
            PutQueueMessage(canonicalMessage);

            // Log the message success
            LogMessageSuccess(canonicalMessage);

            // Log
            LogInfo("Successfully completed the Obstetrician Note processing.", canonicalMessage);

            // Return a success
            return new UploadResponse()
            {
                Success = true,
                ServiceBusMessageID = canonicalMessage.ServiceBusMessageID.ToString()
            };
        }
        #endregion
    }
}
