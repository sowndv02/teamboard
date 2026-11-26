using Microsoft.IdentityModel.Tokens;

namespace Teamboard.Shared.Auth;

internal interface IJwksKeySetSource
{
    JsonWebKeySet Load();
}