using System;
using EnsureThat;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.Common.Configuration
{
    public static class EnvironmentConfigurationExtensions
    {
        public static void SetupConfiguration(this EnvironmentConfiguration environmentConfiguration)
        {
            EnsureArg.IsNotNull(environmentConfiguration, nameof(environmentConfiguration));

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("Environment.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = configurationBuilder.Build();

            configuration.GetSection("Environment").Bind(environmentConfiguration);

            if (environmentConfiguration.KeyVaultEndpoint != null)
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var keyVaultConfig = new AzureKeyVaultConfigurationOptions
                {
                    Vault = environmentConfiguration.KeyVaultEndpoint,
                    Client = keyVaultClient,
                    Manager = new DefaultKeyVaultSecretManager(),
                    ReloadInterval = TimeSpan.FromMinutes(5),
                };
                configurationBuilder.AddAzureKeyVault(keyVaultConfig);
                configuration = configurationBuilder.Build();

                configuration.GetSection("Environment").Bind(environmentConfiguration);
            }
        }
    }
}
