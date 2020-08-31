﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Context
{
    public interface IDicomRequestContextAccessor
    {
        IDicomRequestContext DicomRequestContext { get; set; }
    }
}