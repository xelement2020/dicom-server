// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagIndexService : ICustomTagIndexService
    {
        private readonly ICustomTagIndexStore _customTagIndexStore;

        public CustomTagIndexService(ICustomTagIndexStore customTagIndexStore)
        {
            _customTagIndexStore = customTagIndexStore;
        }

        public async Task AddCustomTagIndexes(Dictionary<long, DicomItem> customTagIndexes, VersionedInstanceIdentifier instanceIdentifier, CancellationToken cancellationToken = default)
        {
            foreach (var item in customTagIndexes.Keys)
            {
                await _customTagIndexStore.AddCustomTagStringIndexes(item, instanceIdentifier, customTagIndexes[item].ToString(), cancellationToken);
            }
        }
    }
}
