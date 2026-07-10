using System;
using System.IO;
using System.Text.RegularExpressions;

namespace RefactorTool
{
    class Program
    {
        static void Main(string[] args)
        {
            string desktopClientPath = @"D:\C#\Dotnet\QuanLyNhanSu.DesktopClient";
            var csFiles = Directory.GetFiles(desktopClientPath, "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in csFiles)
            {
                // Skip auto-generated or external files
                if (file.Contains(@"\obj\") || file.Contains(@"\bin\") || file.Contains("Designer.cs") || file.Contains("ApiClient.cs"))
                    continue;

                string content = File.ReadAllText(file);
                
                // If it doesn't have HttpClientHandler or HttpClient, skip
                if (!content.Contains("HttpClientHandler") && !content.Contains("HttpClient client"))
                    continue;
                    
                Console.WriteLine($"Refactoring {Path.GetFileName(file)}...");

                // Add using statement if not present
                if (!content.Contains("using QuanLyNhanSu.DesktopClient.Services;"))
                {
                    content = content.Replace("using System.Windows.Forms;", "using System.Windows.Forms;\r\nusing QuanLyNhanSu.DesktopClient.Services;");
                }

                // Replacing:
                // HttpClientHandler handler = new HttpClientHandler();
                // handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                // using (HttpClient client = new HttpClient(handler))
                // {
                //      ...
                // }
                // OR similar variants
                
                // We'll just replace specific strings that create HttpClient
                // For instance, FormLeaveRequest.cs has:
                // HttpClientHandler handler = new HttpClientHandler();
                // handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                // using (HttpClient client = new HttpClient(handler))
                
                content = Regex.Replace(content, @"HttpClientHandler\s+handler\s*=\s*new\s+HttpClientHandler\(\);", "");
                content = Regex.Replace(content, @"using\s*var\s*handler\s*=\s*new\s+HttpClientHandler\(\);", "");
                content = Regex.Replace(content, @"handler\.ServerCertificateCustomValidationCallback\s*=\s*\(sender,\s*cert,\s*chain,\s*sslPolicyErrors\)\s*=>\s*true;", "");
                
                // Replace: using (HttpClient client = new HttpClient(handler))
                // Since ApiClient is static, we don't need 'using (var client = ...)'
                // But there's a block { ... }. We can just remove the 'using' line and the braces, OR just keep it as a no-op block.
                // Keeping it as a no-op block:
                // {
                //     var client = ...
                // }
                
                content = Regex.Replace(content, @"using\s*\(\s*(?:var|HttpClient)\s+client\s*=\s*new\s+HttpClient\(handler\)[^)]*\)", "");
                
                // Also: using var client = new HttpClient(handler) { BaseAddress = new Uri(API_BASE_URL) };
                content = Regex.Replace(content, @"using\s+var\s+client\s*=\s*new\s+HttpClient\(handler\)[^;]*;", "");
                
                // Now replace client.PostAsync(...) with ApiClient.PostAsync(...)
                // Actually, wait, some requests add token manually inside the block!
                // client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
                content = Regex.Replace(content, @"client\.DefaultRequestHeaders\.Authorization\s*=\s*new\s*(?:System\.)?Net\.Http\.Headers\.AuthenticationHeaderValue\([^)]*\);", "");
                
                // client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                content = Regex.Replace(content, @"client\.DefaultRequestHeaders\.Add\(""X-Requested-With"",\s*""XMLHttpRequest""\);", "");
                
                // Now change calls:
                // await client.GetAsync("https://localhost:44387/api/...") 
                // to: await ApiClient.GetAsync("api/...", userToken)
                content = Regex.Replace(content, @"client\.GetAsync\(""?https://localhost:44387/([^""]+)""?\)", "ApiClient.GetAsync(\"$1\", userToken)");
                content = Regex.Replace(content, @"client\.PostAsync\(""?https://localhost:44387/([^""]+)""?,\s*([^\)]+)\)", "ApiClient.PostAsync(\"$1\", $2, userToken)");
                content = Regex.Replace(content, @"client\.PutAsync\(""?https://localhost:44387/([^""]+)""?,\s*([^\)]+)\)", "ApiClient.PutAsync(\"$1\", $2, userToken)");
                content = Regex.Replace(content, @"client\.DeleteAsync\(""?https://localhost:44387/([^""]+)""?\)", "ApiClient.DeleteAsync(\"$1\", userToken)");

                // Wait, if url is a variable, e.g., client.GetAsync(API_BASE_URL + "/api/...")
                content = Regex.Replace(content, @"client\.GetAsync\(API_BASE_URL\s*\+\s*""([^""]+)""\)", "ApiClient.GetAsync(\"$1\", _userToken)");
                content = Regex.Replace(content, @"client\.PostAsync\(API_BASE_URL\s*\+\s*""([^""]+)""?,\s*([^\)]+)\)", "ApiClient.PostAsync(\"$1\", $2, _userToken)");
                
                File.WriteAllText(file, content);
            }
        }
    }
}
