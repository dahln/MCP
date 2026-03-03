using Blazored.Toast.Services;
using BlazorSpinner;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Portal.LUNA.App.Services;

public class API
{
    private readonly HttpClient _httpClient;
    private readonly SpinnerService _spinnerService;
    private readonly IToastService _toastService;
    private readonly NavigationManager _navigationManager;

    public API(HttpClient httpClient, SpinnerService spinnerService, IToastService toastService, NavigationManager navigationManager)
    {
        _httpClient = httpClient;
        _spinnerService = spinnerService;
        _toastService = toastService;
        _navigationManager = navigationManager;
    }

    public async Task<T?> GetAsync<T>(string path, bool showSpinner = true, bool isIdentityRequest = false, bool redirectOn404 = true)
    {
        var response = await SendAsync(HttpMethod.Get, path, showSpinner);
        var noErrors = await ParseErrorsDisplayAsToast(response, isIdentityRequest, redirectOn404);
        return noErrors ? await ParseResponse<T>(response) : default;
    }

    public async Task<bool> GetAsync(string path, bool showSpinner = true, bool isIdentityRequest = false, bool redirectOn404 = true)
    {
        var response = await SendAsync(HttpMethod.Get, path, showSpinner);
        return await ParseErrorsDisplayAsToast(response, isIdentityRequest, redirectOn404);
    }

    public async Task<T?> PostAsync<T>(string path, object content, bool showSpinner = true, bool isIdentityRequest = false, bool redirectOn404 = true)
    {
        var response = await SendAsync(HttpMethod.Post, path, showSpinner, content);
        var noErrors = await ParseErrorsDisplayAsToast(response, isIdentityRequest, redirectOn404);
        return noErrors ? await ParseResponse<T>(response) : default;
    }

    public async Task<bool> PostAsync(string path, object content, bool showSpinner = true, bool isIdentityRequest = false, bool redirectOn404 = true)
    {
        var response = await SendAsync(HttpMethod.Post, path, showSpinner, content);
        return await ParseErrorsDisplayAsToast(response, isIdentityRequest, redirectOn404);
    }

    public async Task<T?> PutAsync<T>(string path, object content, bool showSpinner = true, bool isIdentityRequest = false, bool redirectOn404 = true)
    {
        var response = await SendAsync(HttpMethod.Put, path, showSpinner, content);
        var noErrors = await ParseErrorsDisplayAsToast(response, isIdentityRequest, redirectOn404);
        return noErrors ? await ParseResponse<T>(response) : default;
    }

    public async Task<bool> PutAsync(string path, object content, bool showSpinner = true, bool isIdentityRequest = false, bool redirectOn404 = true)
    {
        var response = await SendAsync(HttpMethod.Put, path, showSpinner, content);
        return await ParseErrorsDisplayAsToast(response, isIdentityRequest, redirectOn404);
    }

    public async Task<bool> DeleteAsync(string path, bool showSpinner = true, bool isIdentityRequest = false, bool redirectOn404 = true)
    {
        var response = await SendAsync(HttpMethod.Delete, path, showSpinner);
        return await ParseErrorsDisplayAsToast(response, isIdentityRequest, redirectOn404);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, bool showSpinner, object? content = null)
    {
        if (showSpinner) _spinnerService.Show();

        var request = new HttpRequestMessage(method, path);
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };

        if (content != null)
        {
            string json = JsonConvert.SerializeObject(content);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);

        if (showSpinner) _spinnerService.Hide();

        return response;
    }

    public async Task<T?> ParseResponse<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode && response.Content != null)
        {
            try
            {
                string content = await response.Content.ReadAsStringAsync();
                if (typeof(T) == typeof(string))
                    content = $"\"{content}\"";
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch { }
        }
        return default;
    }

    public async Task<bool> ParseErrorsDisplayAsToast(HttpResponseMessage response, bool isIdentityRequest = false, bool redirectOn404 = true)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized && redirectOn404)
        {
            _navigationManager.NavigateTo("/", true);
        }

        if (!response.IsSuccessStatusCode && !isIdentityRequest)
        {
            string error = await ParseResponseError(response);
            if (!string.IsNullOrEmpty(error)) _toastService.ShowError(error);
            return false;
        }
        else if (response.StatusCode == HttpStatusCode.Unauthorized && isIdentityRequest && !redirectOn404)
        {
            var errors = await ParseIdentityLoginResponseError(response);
            foreach (var e in errors) _toastService.ShowError(e);
            return false;
        }
        else if (!response.IsSuccessStatusCode && isIdentityRequest)
        {
            var errors = await ParseIdentityResponseError(response);
            foreach (var e in errors) _toastService.ShowError(e);
            return false;
        }

        return true;
    }

    private async Task<string> ParseResponseError(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<List<string>> ParseIdentityResponseError(HttpResponseMessage response)
    {
        var errors = new List<string>();
        try
        {
            var details = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(details);
            var errorList = doc.RootElement.GetProperty("errors");
            foreach (var entry in errorList.EnumerateObject())
            {
                if (entry.Value.ValueKind == JsonValueKind.String)
                    errors.Add(entry.Value.GetString()!);
                else if (entry.Value.ValueKind == JsonValueKind.Array)
                    errors.AddRange(entry.Value.EnumerateArray().Select(e => e.GetString() ?? "").Where(e => !string.IsNullOrEmpty(e)));
            }
        }
        catch { }
        return errors;
    }

    private async Task<List<string>> ParseIdentityLoginResponseError(HttpResponseMessage response)
    {
        var errors = new List<string>();
        try
        {
            var details = await response.Content.ReadAsStringAsync();
            var obj = Newtonsoft.Json.Linq.JObject.Parse(details);
            string? detail = (string?)obj["detail"];
            if (!string.IsNullOrEmpty(detail) && detail != "Failed")
                errors.Add(detail);
        }
        catch { }
        return errors;
    }
}
