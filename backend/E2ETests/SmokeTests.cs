using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace E2ETests;

public class SmokeTests
{
    private static readonly Uri BackendBaseUri = CreateUri("SMOKE_API_URL", "http://localhost:5000");
    private static readonly Uri FrontendBaseUri = CreateUri("SMOKE_FRONTEND_URL", "http://localhost:3000");
    private static readonly string DemoOwnerEmail = GetEnvironmentValue(
        "SMOKE_DEMO_OWNER_EMAIL",
        "owner@clinicbook.local");
    private static readonly string DemoPatientEmail = GetEnvironmentValue(
        "SMOKE_DEMO_PATIENT_EMAIL",
        "patient@clinicbook.local");
    private static readonly string DemoPassword = GetEnvironmentValue(
        "SMOKE_DEMO_PASSWORD",
        "ClinicBook123!");

    [Fact]
    public async Task Backend_health_endpoint_returns_success()
    {
        using var client = CreateClient(BackendBaseUri);

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Backend_protected_auth_endpoint_rejects_anonymous_request()
    {
        using var client = CreateClient(BackendBaseUri);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Frontend_root_returns_html_shell()
    {
        using var client = CreateClient(FrontendBaseUri);

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<div id=\"root\"></div>", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Demo_patient_can_book_view_and_cancel_an_appointment()
    {
        using var client = CreateClient(BackendBaseUri);
        var patientToken = await LoginAsync(client, DemoPatientEmail);
        var ownerToken = await LoginAsync(client, DemoOwnerEmail);
        var date = NextFutureMondayInClinicTime();

        var clinics = await GetDataAsync(client, "/api/clinics/discover", patientToken);
        var clinic = Assert.Single(
            clinics.EnumerateArray(),
            item => item.GetProperty("name").GetString() == "ClinicBook Demo Clinic");
        var clinicId = clinic.GetProperty("id").GetGuid();

        var doctors = await GetDataAsync(
            client,
            $"/api/clinics/{clinicId}/doctors",
            patientToken);
        var doctor = Assert.Single(doctors.EnumerateArray());
        var doctorUserId = doctor.GetProperty("userId").GetGuid();

        var slots = await GetDataAsync(
            client,
            $"/api/clinics/{clinicId}/doctors/{doctorUserId}/availability/slots?date={date:yyyy-MM-dd}",
            patientToken);
        var slot = slots.EnumerateArray().First();
        var startsAtUtc = slot.GetProperty("startsAtUtc").GetDateTimeOffset();

        var booking = await SendDataAsync(
            client,
            HttpMethod.Post,
            $"/api/clinics/{clinicId}/doctors/{doctorUserId}/appointments",
            patientToken,
            new { StartsAtUtc = startsAtUtc });
        var appointmentId = booking.GetProperty("id").GetGuid();
        Assert.Equal(startsAtUtc, booking.GetProperty("startsAtUtc").GetDateTimeOffset());

        var patientAppointments = await GetDataAsync(client, "/api/appointments/mine", patientToken);
        Assert.Contains(
            patientAppointments.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == appointmentId);

        var clinicAppointments = await GetDataAsync(
            client,
            $"/api/clinics/{clinicId}/appointments?date={date:yyyy-MM-dd}",
            ownerToken);
        Assert.Contains(
            clinicAppointments.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == appointmentId
                && item.GetProperty("status").GetInt32() == 1);

        await SendDataAsync(
            client,
            HttpMethod.Post,
            $"/api/appointments/{appointmentId}/cancel",
            patientToken);

        var cancelledAppointments = await GetDataAsync(
            client,
            $"/api/clinics/{clinicId}/appointments?date={date:yyyy-MM-dd}",
            ownerToken);
        var cancelled = Assert.Single(
            cancelledAppointments.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == appointmentId);
        Assert.Equal(2, cancelled.GetProperty("status").GetInt32());
    }

    private static HttpClient CreateClient(Uri baseUri)
    {
        return new HttpClient
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    private static Uri CreateUri(string variableName, string fallback)
    {
        return new Uri(GetEnvironmentValue(variableName, fallback), UriKind.Absolute);
    }

    private static string GetEnvironmentValue(string variableName, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = DemoPassword });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login response did not contain an access token.");
    }

    private static async Task<JsonElement> GetDataAsync(
        HttpClient client,
        string requestUri,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await ReadDataAsync(response);
    }

    private static async Task<JsonElement> SendDataAsync(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        string accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, requestUri)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await ReadDataAsync(response);
    }

    private static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.TryGetProperty("data", out var data)
            ? data.Clone()
            : body.Clone();
    }

    private static DateOnly NextFutureMondayInClinicTime()
    {
        var clinicTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, clinicTimeZone).DateTime);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0)
        {
            daysUntilMonday = 7;
        }

        return today.AddDays(daysUntilMonday);
    }
}
