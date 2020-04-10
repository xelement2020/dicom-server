// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    public class DicomInstanceEntryProcessorTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly DicomDataset _dicomDataset1 = Samples.CreateRandomInstanceDataset(
            studyInstanceUid: "1",
            seriesInstanceUid: "2",
            sopInstanceUid: "3",
            sopClassUid: "4");

        private readonly DicomDataset _dicomDataset2 = Samples.CreateRandomInstanceDataset(
            studyInstanceUid: "10",
            seriesInstanceUid: "11",
            sopInstanceUid: "12",
            sopClassUid: "13");

        private readonly Func<DicomStoreResponseBuilder> _dicomStoreResponseBuilderFactory;
        private readonly IDicomStoreService _dicomStoreService = Substitute.For<IDicomStoreService>();
        private readonly DicomInstanceEntryProcessor _dicomInstanceEntryProcessor;

        public DicomInstanceEntryProcessorTests()
        {
            _dicomStoreResponseBuilderFactory = () => new DicomStoreResponseBuilder(new MockUrlResolver());

            _dicomInstanceEntryProcessor = new DicomInstanceEntryProcessor(
                _dicomStoreResponseBuilderFactory,
                _dicomStoreService,
                NullLogger<DicomInstanceEntryProcessor>.Instance);
        }

        [Fact]
        public async Task GivenNullDicomInstanceEntries_WhenProcessed_ThenNoContentShouldBeReturned()
        {
            DicomStoreResponse response = await _dicomInstanceEntryProcessor.ProcessAsync(
                dicomInstanceEntries: null,
                requiredStudyInstanceUid: null,
                cancellationToken: DefaultCancellationToken);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(response.Dataset);
        }

        [Fact]
        public async Task GivenEmptyDicomInstanceEntries_WhenProcessed_ThenNoContentShouldBeReturned()
        {
            DicomStoreResponse response = await _dicomInstanceEntryProcessor.ProcessAsync(
                dicomInstanceEntries: new IDicomInstanceEntry[0],
                requiredStudyInstanceUid: null,
                cancellationToken: DefaultCancellationToken);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(response.Dataset);
        }

        [Fact]
        public async Task GivenAValidDicomInstanceEntry_WhenProcessed_ThenSuccessfulEntryShouldBeAdded()
        {
            IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

            dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset1);

            DicomStoreResponse response = await _dicomInstanceEntryProcessor.ProcessAsync(
                new[] { dicomInstanceEntry },
                requiredStudyInstanceUid: null,
                cancellationToken: DefaultCancellationToken);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Dataset);

            ValidationHelpers.ValidateReferencedSopSequence(
                response.Dataset,
                ("3", "/1/2/3", "4"));
        }

        [Fact]
        public async Task GiveAnInvalidDicomDataset_WhenProcessed_ThenFailedEntryShouldBeAdded()
        {
            IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

            dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns<DicomDataset>(_ => throw new Exception());

            DicomStoreResponse response = await _dicomInstanceEntryProcessor.ProcessAsync(
                new[] { dicomInstanceEntry },
                requiredStudyInstanceUid: null,
                cancellationToken: DefaultCancellationToken);

            Assert.NotNull(response);
            Assert.Equal((int)HttpStatusCode.Conflict, response.StatusCode);
            Assert.NotNull(response.Dataset);

            ValidationHelpers.ValidateFailedSopSequence(
                response.Dataset,
                (null, null, TestConstants.ProcessingFailureReasonCode));
        }
    }
}
