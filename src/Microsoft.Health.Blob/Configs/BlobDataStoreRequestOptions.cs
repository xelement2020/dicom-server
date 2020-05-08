// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Blob.Configs
{
    public class BlobDataStoreRequestOptions
    {
        public TimeSpan ExponentialRetryBackoffDelta { get; set; } = TimeSpan.FromSeconds(4);

        public int ExponentialRetryMaxAttempts { get; set; } = 3;

        public TimeSpan ServerTimeout { get; set; } = TimeSpan.FromMinutes(2);

        public int ParallelOperationThreadCount { get; set; } = 2;
    }
}
