// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace DicomImporter
{
    public static class DicomFileBlobTrigger
    {
        private static readonly string StudiesPostUrl = $"{Environment.GetEnvironmentVariable("DicomServerUrl")}/studies";
        private static readonly string ImportContainerName = "dicomimport";
        private static readonly string RejectContainerName = "dicomrejectedimports";

        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicom = new MediaTypeWithQualityHeaderValue("application/dicom");
        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicomJson = new MediaTypeWithQualityHeaderValue("application/dicom+json");

        [FunctionName("DicomFileBlobTrigger")]
        public static async Task Run([BlobTrigger("dicomimport/{name}")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            // Check the DICOM file can be opened
            DicomFile dicomFile = null;
            try
            {
                dicomFile = await DicomFile.OpenAsync(myBlob);
            }
            catch
            {
                log.LogError("Input file is not a valid DICOM file.");
                await MoveBlobToRejected(name, log);
                return;
            }

            // Post the Dicom file to the server
            HttpStatusCode response = HttpStatusCode.Unused;
            try
            {
                response = await PostDicomFile(dicomFile);
            }
            catch
            {
                log.LogError("Exception while trying to send DICOM file to server.");
                return;
            }

            switch (response)
            {
                case HttpStatusCode.Accepted:
                    log.LogInformation("Server accepted the file.");
                    break;
                case HttpStatusCode.BadRequest:
                    log.LogError("Server did not accept the file as it was not a valid DICOM file.");
                    await MoveBlobToRejected(name, log);
                    return;
                case HttpStatusCode.Conflict:
                    log.LogError("Server did not accept the file as a file with the UIDs already exists");
                    await MoveBlobToRejected(name, log);
                    return;
                default:
                    log.LogError($"Server returned {response} and did not accept the file.");
                    await MoveBlobToRejected(name, log);
                    return;
            }

            await RemoveBlob(name, log);
        }

        private static async Task<HttpStatusCode> PostDicomFile(DicomFile dicomFile)
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            using (var stream = new MemoryStream())
            {
                await dicomFile.SaveAsync(stream);

                var validByteContent = new ByteArrayContent(stream.ToArray());
                validByteContent.Headers.ContentType = MediaTypeApplicationDicom;
                multiContent.Add(validByteContent);
            }

            var httpClient = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, StudiesPostUrl);
            request.Headers.Accept.Add(MediaTypeApplicationDicomJson);
            request.Content = multiContent;

            using (HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                return response.StatusCode;
            }
        }

        private static CloudBlockBlob GetBlobReference(string containerName, string blobName, ILogger log)
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

        private static async Task MoveBlobToRejected(string name, ILogger log)
        {
            CloudBlockBlob srcBlob = GetBlobReference(ImportContainerName, name, log);
            CloudBlockBlob destBlob = GetBlobReference(RejectContainerName, name, log);

            await destBlob.StartCopyAsync(srcBlob);
            await srcBlob.DeleteAsync();
        }

        private static async Task RemoveBlob(string name, ILogger log)
        {
            CloudBlockBlob srcBlob = GetBlobReference(ImportContainerName, name, log);
            await srcBlob.DeleteAsync();
        }
    }
}
