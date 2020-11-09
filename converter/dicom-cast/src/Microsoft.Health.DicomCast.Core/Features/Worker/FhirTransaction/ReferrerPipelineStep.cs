// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ReferrerPipelineStep : IFhirTransactionPipelineStep
    {
        private readonly IFhirService _fhirService;

        public ReferrerPipelineStep(
            IFhirService fhirService)
        {
            EnsureArg.IsNotNull(fhirService, nameof(fhirService));

            _fhirService = fhirService;
        }

        public async Task PrepareRequestAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            // TODO: Figure out how to search uniquely
            Practitioner practitioner = await _fhirService.RetrievePractitionerAsync(new Identifier(), cancellationToken);

            FhirTransactionRequestMode requestMode = FhirTransactionRequestMode.None;

            IResourceId resourceId = null;

            // TODO: Since not creating new practitioner figure out what to do if fail to retrive practitioner
            if (practitioner != null)
            {
                resourceId = practitioner.ToServerResourceId();
            }

            context.Request.Referrer = new FhirTransactionRequestEntry(
                requestMode,
                null,
                resourceId,
                practitioner);
        }

        public void ProcessResponse(FhirTransactionContext context)
        {
            // No action needed.
        }
    }
}
