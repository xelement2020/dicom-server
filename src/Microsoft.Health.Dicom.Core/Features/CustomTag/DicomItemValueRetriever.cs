// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class DicomItemValueRetriever : IDicomItemValueRetriever
    {
        private static readonly IReadOnlyDictionary<string, Func<DicomItem, object>> Retrievers = new Dictionary<string, Func<DicomItem, object>>()
        {
            { DicomVRCode.AE,  RetrieveAs<string> },
            { DicomVRCode.AS,   RetrieveAs<string> },
            { DicomVRCode.AT,  RetrieveAttributeTag },
            { DicomVRCode.CS,   RetrieveAs<string> },
            { DicomVRCode.DA,   RetrieveAs<DateTime> },
            { DicomVRCode.DS,   RetrieveAs<string> },
            { DicomVRCode.DT,  RetrieveAs<DateTime> },
            { DicomVRCode.FL,  RetrieveAs<double> },
            { DicomVRCode.FD,  RetrieveAs<double> },
            { DicomVRCode.IS,   RetrieveAs<string> },
            { DicomVRCode.LO,   RetrieveAs<string> },
            { DicomVRCode.PN,   RetrieveAs<string> },
            { DicomVRCode.SH,   RetrieveAs<string> },
            { DicomVRCode.SL,   RetrieveAs<long> },
            { DicomVRCode.SS,   RetrieveAs<long> },
            { DicomVRCode.TM,   RetrieveAs<DateTime> },
            { DicomVRCode.UI,   RetrieveAs<string> },
            { DicomVRCode.UL,   RetrieveAs<long> },
            { DicomVRCode.US,   RetrieveAs<long> },
        };

        public object Retrieve(DicomItem dicomItem)
        {
            EnsureArg.IsNotNull(dicomItem, nameof(dicomItem));
            string vrCode = dicomItem.ValueRepresentation.Code;
            if (!Retrievers.ContainsKey(vrCode))
            {
                // TODO: information: the vrcode is not supported
                throw new Exception("dsdf");
            }

            return Retrievers[vrCode].Invoke(dicomItem);
        }

        private static object RetrieveAs<T>(DicomItem dicomItem)
        {
            return (dicomItem as DicomDateElement).Get<T>();
        }

        private static object RetrieveAttributeTag(DicomItem dicomItem)
        {
            EnsureArg.IsOfType(dicomItem, typeof(DicomAttributeTag), nameof(dicomItem));
            DicomAttributeTag attributeTag = dicomItem as DicomAttributeTag;
            DicomTag dicomTag = attributeTag.Get<DicomTag>();

            // convert to long
            return (((long)dicomTag.Group) << 16) + dicomTag.Element;
        }
    }
}
