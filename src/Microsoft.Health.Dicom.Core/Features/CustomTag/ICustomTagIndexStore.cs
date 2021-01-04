// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public interface ICustomTagIndexStore
    {
        // TODO: make it allow add bunch indexes
        Task AddCustomTagStringIndexes(long customTagKey, VersionedInstanceIdentifier instanceIdentifier, string indexValue, CancellationToken cancellationToken = default);

        Task AddCustomTagLongIndexes(long customTagKey, VersionedInstanceIdentifier instanceIdentifier, long indexValue, CancellationToken cancellationToken = default);

        Task AddCustomTagDoubleIndexes(long customTagKey, VersionedInstanceIdentifier instanceIdentifier, double indexValue, CancellationToken cancellationToken = default);

        Task AddCustomTagDateTimeIndexes(long customTagKey, VersionedInstanceIdentifier instanceIdentifier, DateTime indexValue, CancellationToken cancellationToken = default);
    }
}
