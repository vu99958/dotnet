using IdentityModel.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuanLyNhanSu.DesktopClient.Services
{
    public class TokenService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _accessToken = string.Empty;
        private static DateTime _tokenExpiration = DateTime.MinValue;

        public static async Task<string> GetValidTokenAsync(string identityServerUrl, string clientId, string clientSecret)
        {
            // [ONBOARDING COMMENT]: Kiểm tra Token còn hạn không. Cắt hao hụt 1 phút (60s) để tránh lỗi network delay.
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiration > DateTime.UtcNow.AddMinutes(1))
            {
                return _accessToken;
            }

            var discovery = await _httpClient.GetDiscoveryDocumentAsync(identityServerUrl);
            if (discovery.IsError) throw new Exception("Không thể kết nối Identity Server");

            // [ONBOARDING COMMENT]: Xin cấp token theo luồng M2M (Machine-to-Machine) bằng Client Credentials.
            var response = await _httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = discovery.TokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = "QuanLyNhanSu" // Tên scope của hệ thống API
            });

            if (response.IsError) throw new Exception($"Lỗi xin cấp Token: {response.Error}");

            _accessToken = response.AccessToken ?? "";
            _tokenExpiration = DateTime.UtcNow.AddSeconds(response.ExpiresIn);

            return _accessToken;
        }
    }
}
