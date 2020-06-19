using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Web.Http;
using HCA.Configuration;
using HCA.Logger;
using HCA.Queue.MQ;

using HieSb.Core.Data.Message;

namespace HieSb.Service.Inbound.MeditechHttp.Tests
{
    public static class Constants
    {
        #region Public Static Properties
        /// <summary>
        /// Name of the app
        /// </summary>
        public static readonly string AppName = "Inbound.MeditechHttp";

        /// <summary>
        /// FQDN of the machine running IBM MQ
       
        #region Public Static Methods
        /// <summary>
        /// Creates a new QueueConnection using the static properties
        /// </summary>
        /// <returns></returns>
        public static QueueConnection CreateConnection()
        {
            return new QueueConnection(AppName, HostName, PortNumber, ChannelName, QueueManagerName);
        }

        /// <summary>
        /// Creates a new QueueConnection using the static properties and a queue name
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns></returns>
        public static QueueConnection CreateConnection(string queueName)
        {
            return new QueueConnection(AppName, HostName, PortNumber, ChannelName, QueueManagerName, queueName);
        }

        /// <summary>
        /// Creates a testing queue and returns the name
        /// </summary>
        /// <returns></returns>
        public static string CreateTestQueue([CallerMemberName] string callerMemberName = "")
        {
            // Generate the name of the testing queue based off the test method name
            var queueName = $"InboundMeditechHttpTests_{callerMemberName}";

            // Ensure length
            queueName = queueName.Truncate(40, "", false);

            // Add a random number for uniqueness
            var random = new Random();
            queueName += $"_{random.Next(0, 10000)}";

            // Create an admin client
            using (var adminClient = new QueueAdmin(CreateConnection()))
            {
                // Create the queue
                adminClient.CreateQueue(queueName);

                // Try to clear it just in case the queue already existed
                adminClient.ClearQueue(queueName);

                // Return the name of the queue created for testing
                return queueName;
            }
        }

        /// <summary>
        /// Checks the queue depth of a test queue
        /// </summary>
        /// <param name="testQueueName">Name of the queue</param>
        /// <returns></returns>
        public static int CheckQueueDepth(string testQueueName)
        {
            // Create an admin client
            using (var adminClient = new QueueAdmin(CreateConnection()))
            {
                // Return the depth of the queue
                return adminClient.GetRealQueueDepth(testQueueName);
            }
        }

        /// <summary>
        /// Deletes the test queue
        /// </summary>
        /// <param name="testQueueName"></param>
        public static void DeleteQueue(string testQueueName)
        {
            // Create an admin client
            using (var adminClient = new QueueAdmin(CreateConnection()))
            {
                // Clear the queue
                adminClient.ClearQueue(testQueueName);

                // Delete the queue
                adminClient.DeleteQueue(testQueueName);
            }
        }

        /// <summary>
        /// Gets the next CanonicalMessage off the queue if one is available
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public static CanonicalMessage GetMessage(string queueName)
        {
            // Create a normal client
            using (var queueClient = new QueueClient(CreateConnection(queueName)))
            {
                // Read the next message
                var message = queueClient.Receive<CanonicalMessage>();

                // Check that a message was returned
                if (message != null)
                {
                    // Mark as read
                    message.Complete();

                    // Return the canonical message
                    return message.Data;
                }
                else
                {
                    // Else return null
                    return null;
                }
            }
        }

        /// <summary>
        /// Creates an HIE SB ConfigurationManager with the PrimaryQueue and ErrorQueue settings mocked up
        /// </summary>
        /// <param name="primaryQueueName"></param>
        /// <param name="errorQueueName"></param>
        /// <returns></returns>
        public static IConfigurationManager CreateConfigurationManager(string primaryQueueName, string errorQueueName)
        {
            // Create the HIE configuration
            var configurationManager = Core.ServiceHost.HieConfigurationUtility.GetHieConfigurationManager();

            // Add the mock configuration
            var mockSource = new Mocks.MockConfigurationSource();
            configurationManager.AddSource(mockSource);

            // Add the temporary queues
            mockSource.AddQueueSettings(AppName, Constants.HostName, Constants.PortNumber, Constants.ChannelName, Constants.QueueManagerName, primaryQueueName);
            mockSource.AddQueueSettings("CommonQueues.ErrorMessage", Constants.HostName, Constants.PortNumber, Constants.ChannelName, Constants.QueueManagerName, errorQueueName);

            // Return the new configuration manager
            return configurationManager;
        }

        /// <summary>
        /// Initializes a Controller to use during testing
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configurationManager"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static T CreateController<T>(IConfigurationManager configurationManager, HttpRequestMessage request, [CallerMemberName] string callerMemberName = "") where T : ApiController
        {
            // Create a logger
            var logger = new HCA.Logger.StandardLogger.Logger();

            // Create a log file name based off the calling method
            var logFileName = callerMemberName + ".log";

            // Check if the normal file already exists
            if (System.IO.File.Exists(logFileName))
            {
                // Delete the existing file
                System.IO.File.Delete(logFileName);
            }

            // Add the normal log file
            logger.AddFileDestination(logFileName);

            // Create a log file for performance
            var performanceLogFileName = callerMemberName + ".Performance.log";

            // Check if the performance file already exists
            if (System.IO.File.Exists(performanceLogFileName))
            {
                // Delete the existing performance file
                System.IO.File.Delete(performanceLogFileName);
            }

            // Add the performance log file
            logger.AddPerformanceFileDestination(performanceLogFileName);


            // Create a LogicControllerActivator instance
            var logicControllerActivator = new HCA.Logic.ServiceHost.LogicControllerActivator(configurationManager, logger);


            // Call the method on the logic activator to create the controller
            var controller = (T)logicControllerActivator.Create(request, null, typeof(T));

            // Set the request property on the controller
            controller.Request = request;

            // Set the fake identity on the controller
            controller.User = new Mocks.MockPrincipal("HCA\\IXI7171");

            // Return the controller
            return controller;
        }

        /// <summary>
        /// Creates a ByteArrayContent from a sample Progress Note PDF
        /// </summary>
        /// <returns></returns>
        public static ByteArrayContent CreateSampleProgressNoteContent()
        {
            // Construct the file name
            var fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"");

            // Read all the bytes
            var fileBytes = System.IO.File.ReadAllBytes(fileName);

            // Return as a ByteArrayContent
            return new ByteArrayContent(fileBytes);
        }

        /// <summary>
        /// Creates a ByteArrayContent from a sample Progress Note PDF
        /// </summary>
        /// <returns></returns>
        public static byte[] CreateSampleProgressNoteBytes()
        {
            // Construct the file name
            var fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"");

            // Read all the bytes
            var fileBytes = System.IO.File.ReadAllBytes(fileName);

            // Return all bytes
            return fileBytes;
        }

        /// <summary>
        /// Creates a ByteArrayContent from a sample Progress Note PDF
        /// </summary>
        /// <returns></returns>
        public static ByteArrayContent CreateSampleObstetricianNoteContent()
        {
            // Construct the file name
            var fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"");

            // Read all the bytes
            var fileBytes = System.IO.File.ReadAllBytes(fileName);

            // Return as a ByteArrayContent
            return new ByteArrayContent(fileBytes);
        }
        #endregion


    }
}
