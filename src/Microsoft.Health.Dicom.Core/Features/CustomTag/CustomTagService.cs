// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagService : ICustomTagService
    {
        private readonly ICustomTagStore _customTagStore;

        public CustomTagService(ICustomTagStore customTagStore)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));

            _customTagStore = customTagStore;
        }

        public async Task<AddCustomTagResponse> AddCustomTagAsync(IEnumerable<CustomTagEntry> customTags, CancellationToken cancellationToken = default)
        {
            IEnumerable<CustomTagEntry> entries = await _customTagStore.AddCustomTagsAsync(customTags, cancellationToken);

            // TODO: start creating jobs
            return new AddCustomTagResponse(entries, string.Empty);
        }
    }
}
