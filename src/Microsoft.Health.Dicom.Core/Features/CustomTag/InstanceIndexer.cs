// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class InstanceIndexer : IInstanceIndexer
    {
        private readonly IMetadataStore _metadataStore;
        private readonly ICustomTagIndexService _customTagIndexService;

        public InstanceIndexer(IMetadataStore metadataStore, ICustomTagIndexService customTagIndexService)
        {
            _metadataStore = metadataStore;
            _customTagIndexService = customTagIndexService;
        }

        public async Task IndexInstanceAsync(IEnumerable<CustomTagEntry> customTags, VersionedInstanceIdentifier instance, CancellationToken cancellationToken = default)
        {
            DicomDataset dataset = await _metadataStore.GetInstanceMetadataAsync(instance);
            Dictionary<long, DicomItem> indexes = new Dictionary<long, DicomItem>();
            foreach (var tag in customTags)
            {
                // TODO: this query performance can be optimized
                DicomItem dicomItem = FindDicomItem(dataset, tag);
                if (dicomItem != null)
                {
                    indexes.Add(tag.Key, dicomItem);
                }
            }

            await _customTagIndexService.AddCustomTagIndexes(indexes, instance, cancellationToken);
        }

        private DicomItem FindDicomItem(DicomDataset dataset, CustomTagEntry tag)
        {
            foreach (var item in dataset)
            {
                string path = GetDicomTagPath(item.Tag);
                if (string.Equals(path, tag.Path, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        // TODO: make this as extension method
        private static string GetDicomTagPath(DicomTag tag)
        {
            return tag.Group.ToString("X4") + tag.Element.ToString("X4");
        }
    }
}
