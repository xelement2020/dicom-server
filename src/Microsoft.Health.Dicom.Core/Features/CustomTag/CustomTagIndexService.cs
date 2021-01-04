// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagIndexService : ICustomTagIndexService
    {
        private readonly ICustomTagIndexStore _customTagIndexStore;
        private readonly IDicomItemValueRetriever _dicomItemValueRetriever;

        public CustomTagIndexService(ICustomTagIndexStore customTagIndexStore, IDicomItemValueRetriever dicomItemValueRetriever)
        {
            EnsureArg.IsNotNull(customTagIndexStore, nameof(customTagIndexStore));
            EnsureArg.IsNotNull(dicomItemValueRetriever, nameof(dicomItemValueRetriever));
            _customTagIndexStore = customTagIndexStore;
            _dicomItemValueRetriever = dicomItemValueRetriever;
        }

        public async Task AddCustomTagIndexes(Dictionary<long, DicomItem> customTagIndexes, VersionedInstanceIdentifier instanceIdentifier, CancellationToken cancellationToken = default)
        {
            foreach (var item in customTagIndexes.Keys)
            {
                object value = _dicomItemValueRetriever.Retrieve(customTagIndexes[item]);
                if (value is string)
                {
                    await _customTagIndexStore.AddCustomTagStringIndexes(item, instanceIdentifier, (string)value, cancellationToken);
                }
                else if (value is long)
                {
                    await _customTagIndexStore.AddCustomTagLongIndexes(item, instanceIdentifier, (long)value, cancellationToken);
                }
                else if (value is DateTime)
                {
                    await _customTagIndexStore.AddCustomTagDateTimeIndexes(item, instanceIdentifier, (DateTime)value, cancellationToken);
                }
                else if (value is double)
                {
                    await _customTagIndexStore.AddCustomTagDoubleIndexes(item, instanceIdentifier, (double)value, cancellationToken);
                }
                else
                {
                    // TODO: process the error
                    throw new Exception("dsdfsf");
                }
            }
        }
    }
}
