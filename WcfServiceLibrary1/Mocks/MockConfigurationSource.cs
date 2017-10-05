using System.Collections.Generic;

using HCA.Configuration;

namespace HieSb.Service.Inbound.MeditechHttp.Tests.Mocks
{
    public class MockConfigurationSource : IConfigurationSource
    {
        public Dictionary<string, string> SourceValues { get; set; }

        public MockConfigurationSource()
        {
            SourceValues = new Dictionary<string, string>();
        }

        public void AddQueueSettings(string queueSettingName, string hostName, int port, string channelName, string queueManagerName, string queueName)
        {
            SourceValues.Add(queueSettingName + ".Queue.HostName", hostName);
            SourceValues.Add(queueSettingName + ".Queue.Port", port.ToString());
            SourceValues.Add(queueSettingName + ".Queue.ChannelName", channelName);
            SourceValues.Add(queueSettingName + ".Queue.QueueManagerName", queueManagerName);
            SourceValues.Add(queueSettingName + ".Queue.QueueName", queueName);
        }

        public string GetValue(string keyName)
        {
            if (SourceValues.ContainsKey(keyName))
            {
                return SourceValues[keyName];
            }
            else
            {
                return null;
            }
        }

        public void Dispose()
        {
            SourceValues.Clear();
        }
    }
}
