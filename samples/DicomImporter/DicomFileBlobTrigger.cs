// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using DicomImporter.Clients;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace DicomImporter
{
    public static class DicomFileBlobTrigger
    {
        private static readonly bool CheckFilesValid = Environment.GetEnvironmentVariable("VerifyFilesBeforeUpload") == "true";

        [FunctionName("DicomFileBlobTrigger")]
        public static async Task Run([BlobTrigger("dicomimport/{name}")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            // Check the DICOM file can be opened
            DicomFile dicomFile = null;
            if (CheckFilesValid)
            {
                try
                {
                    dicomFile = await DicomFile.OpenAsync(myBlob);
                }
                catch
                {
                    log.LogError("Input file is not a valid DICOM file.");
                    await BlobClient.MoveBlobToRejected(name, log);
                    return;
                }
            }

            // Post the Dicom file to the server
            HttpStatusCode response = HttpStatusCode.Unused;
            try
            {
                if (CheckFilesValid)
                {
                    response = await DicomWebClient.PostDicomFile(dicomFile);
                }
                else
                {
                    response = await DicomWebClient.PostDicomFileStream(myBlob);
                }
            }
            catch
            {
                log.LogError("Exception while trying to send DICOM file to server. Check the server is running.");
                await BlobClient.MoveBlobToRejected(name, log);
                return;
            }

            switch (response)
            {
                case HttpStatusCode.Accepted:
                    log.LogInformation("Server accepted the file.");
                    break;
                case HttpStatusCode.BadRequest:
                    log.LogError($"Server returned {response} and did not accept the file. Most likely caused by bad syntax.");
                    await BlobClient.MoveBlobToRejected(name, log);
                    return;
                case HttpStatusCode.Conflict:
                    log.LogError($"Server returned {response} and did not accept the file. Most likely causesd by a file with the same name already stored on the server, or a UID attribute might be invalid.");
                    await BlobClient.MoveBlobToRejected(name, log);
                    return;
                default:
                    log.LogError($"Server returned {response} and did not accept the file.");
                    await BlobClient.MoveBlobToRejected(name, log);
                    return;
            }

            await BlobClient.RemoveBlob(name, log);
        }
    }
}
