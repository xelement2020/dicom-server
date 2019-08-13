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

namespace DicomImporter.Clients
{
    public static class DicomWebClient
    {
        private static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicom = new MediaTypeWithQualityHeaderValue("application/dicom");
        private static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicomJson = new MediaTypeWithQualityHeaderValue("application/dicom+json");

        private static string StudiesPostUrl
        {
            get
            {
                if (Environment.GetEnvironmentVariable("DicomServerUrl") == null)
                {
                    return "http://localhost:63837";
                }

                return $"{Environment.GetEnvironmentVariable("DicomServerUrl")}/studies";
            }
        }

        public static async Task<HttpStatusCode> PostDicomFile(DicomFile dicomFile)
        {
            using (var stream = new MemoryStream())
            {
                await dicomFile.SaveAsync(stream);

                var validByteContent = new ByteArrayContent(stream.ToArray());
                return await PostDicomFileStream(validByteContent);
            }
        }

        public static async Task<HttpStatusCode> PostDicomFileStream(Stream stream)
        {
            var validByteContent = new ByteArrayContent(new StreamContent(stream).ReadAsByteArrayAsync().Result);
            return await PostDicomFileStream(validByteContent);
        }

        private static async Task<HttpStatusCode> PostDicomFileStream(ByteArrayContent fileContent)
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"{MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            fileContent.Headers.ContentType = MediaTypeApplicationDicom;
            multiContent.Add(fileContent);

            var httpClient = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, StudiesPostUrl);
            request.Headers.Accept.Add(MediaTypeApplicationDicomJson);
            request.Content = multiContent;

            using (HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                return response.StatusCode;
            }
        }
    }
}
