﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomSeries : DicomStudy
    {
        [JsonConstructor]
        public DicomSeries(string studyInstanceUID, string seriesInstanceUID)
            : base(studyInstanceUID)
        {
            // Run the instance identifiers through the regular expression check.
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID, nameof(seriesInstanceUID));
            EnsureArg.IsTrue(Regex.IsMatch(seriesInstanceUID, DicomIdentifierValidator.IdentifierRegex));
            EnsureArg.IsNotEqualTo(studyInstanceUID, seriesInstanceUID);

            SeriesInstanceUID = seriesInstanceUID;
        }

        public string SeriesInstanceUID { get; }

        public override bool Equals(object obj)
        {
            if (obj is DicomSeries identity)
            {
                return StudyInstanceUID.Equals(identity.StudyInstanceUID, EqualsStringComparison) &&
                        SeriesInstanceUID.Equals(identity.SeriesInstanceUID, EqualsStringComparison);
            }

            return false;
        }

        public override int GetHashCode()
            => (StudyInstanceUID + SeriesInstanceUID).GetHashCode(EqualsStringComparison);
    }
}