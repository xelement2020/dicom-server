// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagService : ICustomTagService
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly IIndexDataStore _indexDataStore;
        private readonly IReindexJob _reindexJob;

        public CustomTagService(ICustomTagStore customTagStore, IIndexDataStore indexDataStore, IReindexJob reindexJob)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
            EnsureArg.IsNotNull(reindexJob, nameof(reindexJob));

            _customTagStore = customTagStore;
            _indexDataStore = indexDataStore;
            _reindexJob = reindexJob;
        }

        public async Task<AddCustomTagResponse> AddCustomTagAsync(IEnumerable<CustomTagEntry> customTags, CancellationToken cancellationToken = default)
        {
            foreach (var tag in customTags)
            {
                long key = await _customTagStore.AddCustomTagAsync(tag.Path, tag.VR, tag.Level, CustomTagStatus.Reindexing);
                tag.Key = key;
                tag.Status = CustomTagStatus.Reindexing;
            }

            long? lastWatermark = await _indexDataStore.GetLatestInstanceAsync(cancellationToken);
            if (lastWatermark.HasValue)
            {
                // if lastWatermark doesn't exist, means no instance in database
                await _reindexJob.ReindexAsync(customTags, lastWatermark.Value);
            }

            // Update tab status
            foreach (var tag in customTags)
            {
                await _customTagStore.UpdateCustomTagStatusAsync(tag.Key, CustomTagStatus.Added);
                tag.Status = CustomTagStatus.Added;
            }

            return new AddCustomTagResponse(customTags, string.Empty);
        }
    }
}
