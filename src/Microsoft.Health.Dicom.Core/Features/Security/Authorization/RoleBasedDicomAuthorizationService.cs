﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Core.Features.Security.Authorization
{
    public class RoleBasedDicomAuthorizationService : IDicomAuthorizationService
    {
        private IDicomRequestContextAccessor _dicomRequestContextAccessor;
        private string _rolesClaimName;
        private Dictionary<string, Role> _roles;

        public RoleBasedDicomAuthorizationService(AuthorizationConfiguration authorizationConfiguration, IDicomRequestContextAccessor dicomRequestContextAccessor)
        {
            EnsureArg.IsNotNull(authorizationConfiguration, nameof(authorizationConfiguration));
            _dicomRequestContextAccessor = EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));

            _rolesClaimName = authorizationConfiguration.RolesClaim;
            _roles = authorizationConfiguration.Roles.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        }

        public ValueTask<DataActions> CheckAccess(DataActions dataActions)
        {
            ClaimsPrincipal principal = _dicomRequestContextAccessor.DicomRequestContext.Principal;

            DataActions permittedDataActions = 0;
            foreach (Claim claim in principal.FindAll(_rolesClaimName))
            {
                if (_roles.TryGetValue(claim.Value, out Role role))
                {
                    permittedDataActions |= role.AllowedDataActions;
                    if (permittedDataActions == dataActions)
                    {
                        break;
                    }
                }
            }

            return new ValueTask<DataActions>(dataActions & permittedDataActions);
        }
    }
}