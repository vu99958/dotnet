using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace QuanLyNhanSu.DesktopClient.Services
{
    /// <summary>
    /// Lớp trung gian quản lý HttpClient duy nhất cho toàn bộ Desktop Client.
    /// Giải quyết vấn đề Socket Exhaustion và DRY (Don't Repeat Yourself).
    /// </summary>
    public static class ApiClient
    {
        private static readonly HttpClient _httpClient;
        public static readonly string BASE_URL;

        static ApiClient()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            BASE_URL = configuration.GetSection("ApiSettings:BaseUrl").Value ?? "https://localhost:44387/";
            bool isDevelopment = configuration.GetValue<bool>("ApiSettings:IsDevelopment");

            var handler = new HttpClientHandler();
            
            if (isDevelopment)
            {
                // Bỏ qua lỗi chứng chỉ SSL giả (Dành cho môi trường dev)
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
            
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BASE_URL),
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            // Thường dùng cho các ABP endpoint để xác định request từ client script/app
            _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        /// <summary>
        /// Tạo HttpRequestMessage với Token nếu có
        /// </summary>
        private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? token, HttpContent? content = null)
        {
            // Loại bỏ dấu / ở đầu nếu có để ghép đúng với BaseAddress
            if (url.StartsWith("/"))
                url = url.Substring(1);

            var request = new HttpRequestMessage(method, url);
            
            if (!string.IsNullOrEmpty(token))
            {
                // Phân biệt Token JWT và User Key
                // JWT Token luôn có dấu '.' (chia làm 3 phần). User Key thì không có.
                if (!token.Contains("."))
                {
                    request.Headers.Add("X-User-Key", token.Trim());
                }
                else
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            
            if (content != null)
            {
                request.Content = content;
            }
            
            return request;
        }

        public static async Task<HttpResponseMessage> GetAsync(string url, string? token = null)
        {
            return await _httpClient.SendAsync(CreateRequest(HttpMethod.Get, url, token));
        }

        public static async Task<HttpResponseMessage> PostAsync(string url, HttpContent? content = null, string? token = null)
        {
            return await _httpClient.SendAsync(CreateRequest(HttpMethod.Post, url, token, content));
        }

        public static async Task<HttpResponseMessage> PutAsync(string url, HttpContent content, string? token = null)
        {
            return await _httpClient.SendAsync(CreateRequest(HttpMethod.Put, url, token, content));
        }

        public static async Task<HttpResponseMessage> DeleteAsync(string url, string? token = null)
        {
            return await _httpClient.SendAsync(CreateRequest(HttpMethod.Delete, url, token));
        }
    }
}
