// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Features.CustomTag
{
    internal class SqlCustomTagIndexStore : ICustomTagIndexStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public SqlCustomTagIndexStore(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }

        public async Task AddCustomTagStringIndexes(long customTagKey, VersionedInstanceIdentifier instanceIdentifier, string indexValue, CancellationToken cancellationToken = default)
        {
            List<CustomTagEntry> results = new List<CustomTagEntry>();
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                VLatest.AddCustomTagIndexString.PopulateCommand(
                    sqlCommandWrapper,
                    customTagKey,
                    instanceIdentifier.StudyInstanceUid,
                    instanceIdentifier.SeriesInstanceUid,
                    instanceIdentifier.SopInstanceUid,
                    indexValue,
                    instanceIdentifier.Version);

                try
                {
                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    // TODO: process as the number
                    switch (ex.Number)
                    {
                        default:
                            throw new DataStoreException(ex);
                    }
                }
            }
        }
    }
}
