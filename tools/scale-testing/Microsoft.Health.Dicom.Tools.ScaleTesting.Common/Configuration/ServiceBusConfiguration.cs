namespace Microsoft.Health.Dicom.Tools.ScaleTesting.Common.Configuration
{
    public class ServiceBusConfiguration
    {
        public string ConnectionString { get; set; }

        public string Subscription { get; set; }

        public string Topic { get; set; }
    }
}
