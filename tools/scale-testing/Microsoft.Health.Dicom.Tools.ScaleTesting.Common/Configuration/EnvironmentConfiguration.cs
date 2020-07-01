using System;

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.Common.Configuration
{
    public class EnvironmentConfiguration
    {
        public string DicomEndpoint { get; set; }

        public string KeyVaultEndpoint { get; set; }

        public ServiceBusConfiguration ServerBus => new ServiceBusConfiguration();
    }
}
