using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace Boxfusion.VehicleBooking.Tests.Integration
{
    /// <summary>
    /// Integration tests call the RUNNING backend over HTTP, exactly like the app does.
    /// Base URL: environment variable VB_API_BASE (default http://localhost:21021).
    /// If the backend is not reachable, these tests are SKIPPED (not failed).
    /// </summary>
    public static class TestEnvironment
    {
        public static readonly string BaseUrl = (Environment.GetEnvironmentVariable("VB_API_BASE") ?? "http://localhost:21021").TrimEnd('/');

        private static readonly Lazy<bool> _backendUp = new Lazy<bool>(() =>
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                return http.GetAsync(BaseUrl + "/swagger/index.html").GetAwaiter().GetResult().IsSuccessStatusCode;
            }
            catch { return false; }
        });

        public static bool BackendIsUp => _backendUp.Value;
    }

    /// <summary>[IntegrationFact] = a normal [Fact] that is skipped when the backend isn't running.</summary>
    public sealed class IntegrationFactAttribute : FactAttribute
    {
        public IntegrationFactAttribute()
        {
            if (!TestEnvironment.BackendIsUp)
                Skip = $"Backend not running at {TestEnvironment.BaseUrl} - start it to run integration tests.";
        }
    }

    /// <summary>Result of one API call: HTTP status + the ABP envelope { success, result, error }.</summary>
    public sealed class ApiResult
    {
        public HttpStatusCode Status { get; init; }
        public JsonNode Body { get; init; }
        public bool Success => Body?["success"]?.GetValue<bool>() ?? false;
        public JsonNode Result => Body?["result"];
        public string ErrorMessage => Body?["error"]?["message"]?.GetValue<string>() ?? "";
        public override string ToString() => $"{(int)Status} {Body?.ToJsonString()}";
    }

    /// <summary>A tiny HTTP client that optionally carries a login token.</summary>
    public sealed class ApiClient
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        public string Token { get; set; }

        public Task<ApiResult> GetAsync(string path) => SendAsync(HttpMethod.Get, path, null);
        public Task<ApiResult> PostAsync(string path, object body = null) => SendAsync(HttpMethod.Post, path, body ?? new { });
        public Task<ApiResult> PutAsync(string path, object body) => SendAsync(HttpMethod.Put, path, body);

        private async Task<ApiResult> SendAsync(HttpMethod method, string path, object body)
        {
            using var request = new HttpRequestMessage(method, TestEnvironment.BaseUrl + path);
            if (!string.IsNullOrEmpty(Token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            if (body != null)
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await Http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            JsonNode json = null;
            try { json = string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text); } catch (JsonException) { }
            return new ApiResult { Status = response.StatusCode, Body = json };
        }
    }
}
