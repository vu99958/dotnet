using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

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
        
        // [ONBOARDING COMMENT]: Cấu hình Polly Policy để tăng tính bền bỉ (Resilience)
        private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;

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
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
            
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BASE_URL),
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            // [ONBOARDING COMMENT]: Chính sách WaitAndRetry - Thử lại 3 lần với Exponential Backoff (2s, 4s, 8s) khi gặp lỗi rớt mạng hoặc 5xx.
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            // [ONBOARDING COMMENT]: Chính sách Circuit Breaker - Nếu API lỗi liên tục 5 lần, tự động ngắt kết nối (Mở mạch) trong 30 giây để tránh làm nghẽn Server.
            _circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? token, HttpContent? content = null)
        {
            if (url.StartsWith("/"))
                url = url.Substring(1);

            var request = new HttpRequestMessage(method, url);
            
            if (!string.IsNullOrEmpty(token))
            {
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

        // [ONBOARDING COMMENT]: Wrap lời gọi API qua PolicyWrap (Retry -> Circuit Breaker)
        public static async Task<HttpResponseMessage> GetAsync(string url, string? token = null)
        {
            return await _retryPolicy.WrapAsync(_circuitBreakerPolicy).ExecuteAsync(() => 
                _httpClient.SendAsync(CreateRequest(HttpMethod.Get, url, token)));
        }

        public static async Task<HttpResponseMessage> PostAsync(string url, HttpContent? content = null, string? token = null)
        {
            return await _retryPolicy.WrapAsync(_circuitBreakerPolicy).ExecuteAsync(() => 
                _httpClient.SendAsync(CreateRequest(HttpMethod.Post, url, token, content)));
        }

        public static async Task<HttpResponseMessage> PutAsync(string url, HttpContent content, string? token = null)
        {
            return await _retryPolicy.WrapAsync(_circuitBreakerPolicy).ExecuteAsync(() => 
                _httpClient.SendAsync(CreateRequest(HttpMethod.Put, url, token, content)));
        }

        public static async Task<HttpResponseMessage> DeleteAsync(string url, string? token = null)
        {
            return await _retryPolicy.WrapAsync(_circuitBreakerPolicy).ExecuteAsync(() => 
                _httpClient.SendAsync(CreateRequest(HttpMethod.Delete, url, token)));
        }
    }
}
