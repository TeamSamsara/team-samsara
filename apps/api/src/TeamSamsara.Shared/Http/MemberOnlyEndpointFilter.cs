// File: /team-samsara/apps/api/src/TeamSamsara.Shared/Http/MemberOnlyEndpointFilter.cs
// Version: 1.0.0
// Latest commit: feature/identity-profile
// Author: Gerrah
//
// Purpose: Rejects requests to a member-only endpoint unless the caller is a verified member.

using Microsoft.AspNetCore.Http;
using TeamSamsara.Shared.Context;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace TeamSamsara.Shared.Http;

public class MemberOnlyEndpointFilter : IEndpointFilter
{
    #region Fields

    private readonly ICurrentUserContext _currentUser;

    #endregion

    #region Constructors

    public MemberOnlyEndpointFilter(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    #endregion

    #region Public Methods

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_currentUser.AuthState == AuthState.Anonymous)
        {
            return HttpResults.Unauthorized();
        }

        if (_currentUser.AuthState != AuthState.Member)
        {
            return HttpResults.Json(
                new AuthStateResponse(_currentUser.AuthState),
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }

    #endregion
}
