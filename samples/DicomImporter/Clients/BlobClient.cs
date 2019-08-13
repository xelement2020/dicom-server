// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace DicomImporter.Clients
{
    public static class BlobClient
    {
        private static string ImportContainerName
        {
            get
            {
                if (Environment.GetEnvironmentVariable("ImportContainerName") == null)
                {
                    return "dicomimport";
                }

                return Environment.GetEnvironmentVariable("ImportContainerName");
            }
        }

        private static string RejectContainerName
        {
            get
            {
                if (Environment.GetEnvironmentVariable("RejectContainerName") == null)
                {
                    return "dicomrejectedimports";
                }

                return Environment.GetEnvironmentVariable("RejectContainerName");
            }
        }

        public static CloudBlockBlob GetBlobReference(string containerName, string blobName, ILogger log)
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            if (CloudStorageAccount.TryParse(connectionString, out CloudStorageAccount storageAccount))
            {
                try
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = cloudBlobClient.GetContainerReference(containerName);
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                    return blockBlob;
                }
                catch
                {
                    log.LogCritical("Unable to get blob reference");
                    return null;
                }
            }
            else
            {
                log.LogCritical("Unable to parse connection string and create storage account reference");
                return null;
            }
        }

        public static async Task MoveBlobToRejected(string name, ILogger log)
        {
            CloudBlockBlob srcBlob = GetBlobReference(ImportContainerName, name, log);
            CloudBlockBlob destBlob = GetBlobReference(RejectContainerName, name, log);

            await destBlob.StartCopyAsync(srcBlob);
            await srcBlob.DeleteAsync();
        }

        public static async Task RemoveBlob(string name, ILogger log)
        {
            CloudBlockBlob srcBlob = GetBlobReference(ImportContainerName, name, log);
            await srcBlob.DeleteAsync();
        }
    }
}
