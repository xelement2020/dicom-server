// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace DicomImporter
{
    public static class DicomFileBlobTrigger
    {
        [FunctionName("DicomFileBlobTrigger")]
        public static async Task Run([BlobTrigger("dicomimport/{name}")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            string studyInstanceUID = string.Empty;
            string seriesUID = string.Empty;
            string sopInstanceUID = string.Empty;

            // Check the DICOM file is valid

            DicomFile dicomFile = null;
            try
            {
                dicomFile = await DicomFile.OpenAsync(myBlob);
                DicomDataset dicomDataset = dicomFile.Dataset;

                studyInstanceUID = dicomDataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                seriesUID = dicomDataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                sopInstanceUID = dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            }
            catch
            {
                log.LogError("Input file is not a valid DICOM file.");
                await MoveBlobToRejected(name, log);
                return;
            }

            // Post the Dicom file to the server

            var dicomWebClient = new DicomWebClient(new HttpClient());
            HttpStatusCode response = HttpStatusCode.Unused;

            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            using (var stream = new MemoryStream())
            {
                await dicomFile.SaveAsync(stream);

                var validByteContent = new ByteArrayContent(stream.ToArray());
                validByteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
                multiContent.Add(validByteContent);
            }

            request.Content = multiContent;
            try
            {
                response = await dicomWebClient.PostMultipartContentAsync(multiContent, "http://localhost:63837/studies");
            }
            catch
            {
                log.LogError("Exception while trying to send DICOM file to server.");
                return;
            }

            if (response != HttpStatusCode.Accepted)
            {
                log.LogError($"Server returned {response} and did not accept the file.");
                await MoveBlobToRejected(name, log);
                return;
            }

            await RemoveBlob(name, log);
        }

        private static CloudBlockBlob GetBlobReference(string containerName, string blobName, ILogger log)
        {
            var connectionString = System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
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
            CloudBlockBlob srcBlob = GetBlobReference("dicomimport", name, log);
            CloudBlockBlob destBlob = GetBlobReference("dicomrejectedimports", name, log);

            await destBlob.StartCopyAsync(srcBlob);
            await srcBlob.DeleteAsync();
        }

        private static async Task RemoveBlob(string name, ILogger log)
        {
            CloudBlockBlob srcBlob = GetBlobReference("dicomimport", name, log);
            await srcBlob.DeleteAsync();
        }
    }
}
