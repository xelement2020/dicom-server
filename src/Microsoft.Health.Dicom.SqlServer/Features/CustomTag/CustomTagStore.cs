// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.CustomTag
{
    internal class CustomTagStore : ICustomTagStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public CustomTagStore(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }

        public async Task<IEnumerable<CustomTagEntry>> AddCustomTagsAsync(IEnumerable<CustomTagEntry> customTags, CancellationToken cancellationToken = default)
        {
            List<CustomTagEntry> results = new List<CustomTagEntry>();
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.AddCustomTags.PopulateCommand(sqlCommandWrapper, customTags.Select(item => ConvertToCustomTagRow(item)));

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (long rKey, string rPath, string rVR, byte rLevel, byte rStatus) = reader.ReadRow(
                               VLatest.CustomTag.Key,
                               VLatest.CustomTag.Path,
                               VLatest.CustomTag.VR,
                               VLatest.CustomTag.Level,
                               VLatest.CustomTag.Status);

                        results.Add(new CustomTagEntry(
                               rKey,
                               rPath,
                               rVR,
                               (CustomTagLevel)rLevel,
                               (CustomTagStatus)rStatus));
                    }
                }
            }

            return results;
        }

        private VLatest.UDTCustomTagRow ConvertToCustomTagRow(CustomTagEntry customTag)
        {
            return new VLatest.UDTCustomTagRow(
                customTag.Key,
                customTag.Path,
                customTag.VR,
                (byte)customTag.Level,
                (byte)customTag.Status);
        }
    }
}
