using System.Security.Cryptography;

namespace MiniCourseCatalog.Mvc.Middleware;

/// <summary>
/// Sinh CSP nonce riêng cho mỗi request (dùng cho các khối &lt;script&gt; inline hợp lệ,
/// xem HttpContext.Items["csp-nonce"]) và gắn các security header chuẩn vào mọi response.
/// script-src chỉ cho phép 'self' + nonce + cdn.jsdelivr.net (bootstrap-icons, Chart.js) —
/// không dùng 'unsafe-inline' cho script. style-src vẫn cần 'unsafe-inline' vì Bootstrap
/// và các view dùng nhiều thuộc tính style="..." nội tuyến, không khả thi để nonce hết trong
/// phạm vi bản vá này (đây là đánh đổi có chủ đích, ít rủi ro hơn nhiều so với unsafe-inline
/// cho script-src).
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items["csp-nonce"] = nonce;

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                $"script-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "img-src 'self' data:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
