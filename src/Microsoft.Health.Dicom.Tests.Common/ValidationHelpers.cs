// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class ValidationHelpers
    {
        public static void ValidateFailureSequence(DicomSequence dicomSequence, ushort expectedFailureCode, params DicomDataset[] expectedFailedDatasets)
        {
            Assert.Equal(DicomTag.FailedSOPSequence, dicomSequence.Tag);
            Assert.True(dicomSequence.Count() == expectedFailedDatasets.Length);
            var expectedSopClassUids = new HashSet<string>(expectedFailedDatasets.Select(x => x.GetSingleValueOrDefault(DicomTag.SOPClassUID, string.Empty)));
            var expectedSopInstanceUids = new HashSet<string>(expectedFailedDatasets.Select(x => x.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty)));

            foreach (DicomDataset dataset in dicomSequence)
            {
                Assert.True(dataset.Count() == 3);
                Assert.Equal(expectedFailureCode, dataset.GetSingleValue<ushort>(DicomTag.FailureReason));
                Assert.Contains(dataset.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID), expectedSopClassUids);
                Assert.Contains(dataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID), expectedSopInstanceUids);
            }
        }

        public static void ValidateSuccessSequence(DicomSequence dicomSequence, params DicomDataset[] expectedDatasets)
        {
            Assert.Equal(DicomTag.ReferencedSOPSequence, dicomSequence.Tag);
            Assert.True(dicomSequence.Count() == expectedDatasets.Length);
            var datasetsBySopInstanceUid = expectedDatasets.ToDictionary(x => x.GetSingleValue<string>(DicomTag.SOPInstanceUID), x => x);

            foreach (DicomDataset dataset in dicomSequence)
            {
                Assert.True(dataset.Count() == 3);
                var referencedSopInstanceUid = dataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                Assert.True(datasetsBySopInstanceUid.ContainsKey(referencedSopInstanceUid));
                DicomDataset referenceDataset = datasetsBySopInstanceUid[referencedSopInstanceUid];
                var dicomInstance = referenceDataset.ToDicomInstanceIdentifier();
                Assert.Equal(referenceDataset.GetSingleValue<string>(DicomTag.SOPClassUID), dataset.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID));
                Assert.EndsWith(
                    $"studies/{dicomInstance.StudyInstanceUid}/series/{dicomInstance.SeriesInstanceUid}/instances/{dicomInstance.SopInstanceUid}",
                    dataset.GetSingleValue<string>(DicomTag.RetrieveURL));
            }
        }

        public static void ValidateReferencedSopSequence(DicomDataset actualDicomDataset, params (string SopInstanceUid, string RetrieveUri, string SopClassUid)[] expectedValues)
        {
            Assert.True(actualDicomDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence sequence));
            Assert.Equal(expectedValues.Length, sequence.Count());

            for (int i = 0; i < expectedValues.Length; i++)
            {
                DicomDataset actual = sequence.ElementAt(i);

                Assert.Equal(expectedValues[i].SopInstanceUid, actual.GetSingleValueOrDefault<string>(DicomTag.ReferencedSOPInstanceUID));
                Assert.Equal(expectedValues[i].RetrieveUri, actual.GetSingleValueOrDefault<string>(DicomTag.RetrieveURL));
                Assert.Equal(expectedValues[i].SopClassUid, actual.GetSingleValueOrDefault<string>(DicomTag.ReferencedSOPClassUID));
            }
        }

        public static void ValidateFailedSopSequence(DicomDataset actualDicomDataset, params (string SopInstanceUid, string SopClassUid, ushort FailureReason)[] expectedValues)
        {
            Assert.True(actualDicomDataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence sequence));
            Assert.Equal(expectedValues.Length, sequence.Count());

            for (int i = 0; i < expectedValues.Length; i++)
            {
                DicomDataset actual = sequence.ElementAt(i);

                ValidateNullOrCorrectValue(expectedValues[i].SopInstanceUid, actual, DicomTag.ReferencedSOPInstanceUID);
                ValidateNullOrCorrectValue(expectedValues[i].SopClassUid, actual, DicomTag.ReferencedSOPClassUID);

                Assert.Equal(expectedValues[i].FailureReason, actual.GetSingleValueOrDefault<ushort>(DicomTag.FailureReason));
            }

            void ValidateNullOrCorrectValue(string expectedValue, DicomDataset actual, DicomTag dicomTag)
            {
                if (expectedValue == null)
                {
                    Assert.False(actual.TryGetSingleValue(dicomTag, out string _));
                }
                else
                {
                    Assert.Equal(expectedValue, actual.GetSingleValueOrDefault<string>(dicomTag));
                }
            }
        }
    }
}
