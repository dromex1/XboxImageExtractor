using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace XboxImageExtractor.Core
{
    public static class Analytics
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ProjectId = "xbox-c7b8d";
        private const string ApiKey = "AIzaSyDA8-9JrRqYAdScMVQv-MXldI6nWC0_XLA";
        
        public static async Task LogEventAsync(string eventName, string details)
        {
            try
            {
                // Organize cleanly into a subcollection per MachineName
                string machine = Environment.MachineName.Replace(" ", "_");
                string url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/users/{machine}/events?key={ApiKey}";
                
                string jsonBody = $@"{{
                    ""fields"": {{
                        ""eventName"": {{ ""stringValue"": ""{eventName}"" }},
                        ""details"": {{ ""stringValue"": ""{details}"" }},
                        ""user"": {{ ""stringValue"": ""{machine}"" }},
                        ""timestamp"": {{ ""timestampValue"": ""{DateTime.UtcNow:O}"" }}
                    }}
                }}";

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                // Fire and forget HttpRequest
                await _httpClient.PostAsync(url, content);
            }
            catch 
            { 
               // Silent fail: użytkownik nie powinien wiedzieć ani odczuć problemów z internetem
            }
        }
        
        public static void Initialize()
        {
            _ = LogEventAsync("AppLaunched", "Xbox Image Extractor successfully launched.");
        }
    }
}
