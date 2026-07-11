using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using QuanLyNhanSu.Domain;
using Volo.Abp.Domain.Repositories;

namespace QuanLyNhanSu
{
    public class UserKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public UserKeyAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userKeyHeader = context.Request.Headers["X-User-Key"].FirstOrDefault();

            if (!string.IsNullOrEmpty(userKeyHeader))
            {
                Console.WriteLine($"[UserKeyMiddleware] Nhận được X-User-Key: {userKeyHeader}");
                var userKeyRepository = context.RequestServices.GetRequiredService<IRepository<UserKey, Guid>>();
                var userKey = await userKeyRepository.FirstOrDefaultAsync(x => x.Key == userKeyHeader && x.Status == "active");

                if (userKey != null && (!userKey.ExpirationDate.HasValue || userKey.ExpirationDate.Value >= DateTime.UtcNow))
                {
                    Console.WriteLine($"[UserKeyMiddleware] Tìm thấy Key hợp lệ cho UserId: {userKey.UserId}");
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userKey.UserId.ToString()),
                        new Claim(ClaimTypes.Role, userKey.Role),
                        new Claim("sub", userKey.UserId.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, "OpenIddict.Validation.AspNetCore");
                    context.User = new ClaimsPrincipal(identity);
                }
                else
                {
                    Console.WriteLine($"[UserKeyMiddleware] Key không hợp lệ, hết hạn, hoặc bị khóa.");
                }
            }

            await _next(context);
        }
    }
}
