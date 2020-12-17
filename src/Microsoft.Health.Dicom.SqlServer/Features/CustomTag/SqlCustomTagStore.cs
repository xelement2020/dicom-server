// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.CustomTag
{
    internal class SqlCustomTagStore : ICustomTagStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public SqlCustomTagStore(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }

        public async Task<long> AddCustomTagAsync(string path, string vr, CustomTagLevel level, CustomTagStatus status, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.AddCustomTag.PopulateCommand(sqlCommandWrapper, path, vr, (byte)level, (byte)status);

                return (long)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
        }

        public async Task DeleteCustomTagAsync(long key, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.DeleteCustomTag.PopulateCommand(sqlCommandWrapper, key);

                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<VersionedInstanceIdentifier>> GetVersionedInstancesAsync(long endWatermark, int top = 10, IndexStatus indexStatus = IndexStatus.Created, CancellationToken cancellationToken = default)
        {
            List<VersionedInstanceIdentifier> results = new List<VersionedInstanceIdentifier>();
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.GetVersionedInstances.PopulateCommand(sqlCommandWrapper, endWatermark, top, (byte)indexStatus);

                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string rStudyInstanceUid, string rSeriesInstanceUid, string rSopInstanceUid, long rWatermark) = reader.ReadRow(
                               VLatest.Instance.StudyInstanceUid,
                               VLatest.Instance.SeriesInstanceUid,
                               VLatest.Instance.SopInstanceUid,
                               VLatest.Instance.Watermark);

                        results.Add(new VersionedInstanceIdentifier(
                               rStudyInstanceUid,
                               rSeriesInstanceUid,
                               rSopInstanceUid,
                               rWatermark));
                    }
                }
            }

            return results;
        }

        public async Task UpdateCustomTagStatusAsync(long key, CustomTagStatus status, CancellationToken cancellationToken = default)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.UpdateCustomTagStatus.PopulateCommand(sqlCommandWrapper, key, (byte)status);
                await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            }
        }
    }
}
