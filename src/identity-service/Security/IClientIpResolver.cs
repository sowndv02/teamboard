namespace IdentityService.Security;

public interface IClientIpResolver
{
    string Resolve(HttpContext context);
}