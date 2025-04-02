using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class BearerPrefixMiddleware
{
    private readonly RequestDelegate _next;

    public BearerPrefixMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && !authHeader.StartsWith("Bearer "))
        {
            // Préfixe le token avec "Bearer " si ce n'est pas déjà le cas.
            context.Request.Headers["Authorization"] = "Bearer " + authHeader;
        }

        await _next(context);
    }
}

