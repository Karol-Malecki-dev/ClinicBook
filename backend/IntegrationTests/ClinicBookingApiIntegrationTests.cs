using Application.DTOs.Auth;
using Application.DTOs.Clinic;
using Application.DTOs.ClinicMembership;
using Application.DTOs.Doctor.DoctorProfile;
using Application.DTOs.Other;
using Domain.Entities;
using Domain.Entities.Clinic;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shared.Responses;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests;

public sealed class ClinicBookingApiIntegrationTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory = new();

    public ClinicBookingApiIntegrationTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Patient_can_discover_book_and_cancel_a_clinic_local_appointment()
    {
        const string ownerEmail = "booking.owner@example.com";
        const string doctorEmail = "booking.doctor@example.com";
        const string patientEmail = "booking.patient@example.com";
        const string otherPatientEmail = "booking.other-patient@example.com";

        await SeedUserAsync(ownerEmail, "Clinic Owner");
        var doctorId = await SeedUserAsync(doctorEmail, "Clinic Doctor");
        await SeedUserAsync(patientEmail, "Clinic Patient");
        await SeedUserAsync(otherPatientEmail, "Other Clinic Patient");

        var ownerTokens = await LoginAsync(ownerEmail);
        SetBearerToken(ownerTokens.AccessToken);

        var clinicResponse = await _client.PostAsJsonAsync("/api/clinics", new
        {
            Name = "Central Clinic",
            Address = new
            {
                Street = "Main Street",
                BuildingNumber = "1",
                City = "Warsaw",
                PostalCode = "00-001",
                Country = "Poland"
            },
            OpenTime = "08:00:00",
            CloseTime = "17:00:00",
            TimeZoneId = "Europe/Warsaw"
        });

        Assert.Equal(HttpStatusCode.Created, clinicResponse.StatusCode);
        var clinic = await clinicResponse.Content.ReadFromJsonAsync<ApiResponse<ClinicResponseDto>>();
        Assert.NotNull(clinic?.Data);
        var clinicId = clinic.Data.Id;
        Assert.Equal("Europe/Warsaw", clinic.Data.TimeZoneId);

        SetBearerToken(ownerTokens.AccessToken);
        var membershipResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{clinicId}/members",
            new AddClinicMembershipRequestDto
            {
                Email = doctorEmail,
                Role = ClinicRole.Doctor
            });
        Assert.Equal(HttpStatusCode.Created, membershipResponse.StatusCode);

        var doctorTokens = await LoginAsync(doctorEmail);
        SetBearerToken(doctorTokens.AccessToken);

        var profileResponse = await _client.PostAsJsonAsync(
            $"/api/doctors/{doctorId}/profile",
            new CreateDoctorProfileRequestDto
            {
                FirstName = "Jan",
                LastName = "Nowak"
            });
        Assert.Equal(HttpStatusCode.Created, profileResponse.StatusCode);

        SetBearerToken(doctorTokens.AccessToken);
        var scheduleResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{clinicId}/doctors/{doctorId}/availability",
            new
            {
                DayOfWeek = (int)DayOfWeek.Monday,
                StartTime = "09:00:00",
                EndTime = "10:00:00",
                SlotDurationMinutes = 30
            });
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);

        var date = NextMondayInClinicTime();
        var patientTokens = await LoginAsync(patientEmail);
        SetBearerToken(patientTokens.AccessToken);

        var discoverResponse = await _client.GetAsync("/api/clinics/discover");
        discoverResponse.EnsureSuccessStatusCode();
        var discoverResult = await discoverResponse.Content.ReadFromJsonAsync<ApiResponse<List<ClinicResponseDto>>>();
        Assert.Contains(discoverResult?.Data ?? [], item => item.Id == clinicId);

        var doctorsResponse = await _client.GetAsync($"/api/clinics/{clinicId}/doctors");
        doctorsResponse.EnsureSuccessStatusCode();
        var doctorsResult = await doctorsResponse.Content.ReadFromJsonAsync<ApiResponse<List<ClinicDoctorDto>>>();
        Assert.Contains(doctorsResult?.Data ?? [], item => item.UserId == doctorId);

        var slotsResponse = await _client.GetAsync(
            $"/api/clinics/{clinicId}/doctors/{doctorId}/availability/slots?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        slotsResponse.EnsureSuccessStatusCode();
        var slotsResult = await slotsResponse.Content.ReadFromJsonAsync<ApiResponse<List<AvailabilitySlotDto>>>();
        Assert.NotNull(slotsResult?.Data);
        Assert.Equal(2, slotsResult.Data.Count);

        var slot = slotsResult.Data[0];
        var bookingResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{clinicId}/doctors/{doctorId}/appointments",
            new CreateAppointmentRequestDto { StartsAtUtc = slot.StartsAtUtc });
        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);
        var bookingResult = await bookingResponse.Content.ReadFromJsonAsync<ApiResponse<AppointmentResponseDto>>();
        Assert.NotNull(bookingResult?.Data);
        Assert.Equal(patientEmail, bookingResult.Data.PatientEmail);
        Assert.Equal(slot.StartsAtUtc, bookingResult.Data.StartsAtUtc);

        var otherPatientTokens = await LoginAsync(otherPatientEmail);
        SetBearerToken(otherPatientTokens.AccessToken);
        var forbiddenCancelResponse = await _client.PostAsync(
            $"/api/appointments/{bookingResult.Data.Id}/cancel",
            content: null);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCancelResponse.StatusCode);

        var duplicateBookingResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{clinicId}/doctors/{doctorId}/appointments",
            new CreateAppointmentRequestDto { StartsAtUtc = slot.StartsAtUtc });
        Assert.Equal(HttpStatusCode.Conflict, duplicateBookingResponse.StatusCode);

        SetBearerToken(patientTokens.AccessToken);
        var myAppointmentsResponse = await _client.GetAsync("/api/appointments/mine");
        myAppointmentsResponse.EnsureSuccessStatusCode();
        var myAppointments = await myAppointmentsResponse.Content.ReadFromJsonAsync<ApiResponse<List<AppointmentResponseDto>>>();
        var appointment = Assert.Single(myAppointments?.Data ?? []);

        var cancelResponse = await _client.PostAsync(
            $"/api/appointments/{appointment.Id}/cancel",
            content: null);
        cancelResponse.EnsureSuccessStatusCode();

        var availableAgainResponse = await _client.GetAsync(
            $"/api/clinics/{clinicId}/doctors/{doctorId}/availability/slots?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        availableAgainResponse.EnsureSuccessStatusCode();
        var availableAgain = await availableAgainResponse.Content.ReadFromJsonAsync<ApiResponse<List<AvailabilitySlotDto>>>();
        Assert.Equal(2, availableAgain?.Data?.Count);

        SetBearerToken(ownerTokens.AccessToken);
        var clinicAppointmentsResponse = await _client.GetAsync(
            $"/api/clinics/{clinicId}/appointments?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        clinicAppointmentsResponse.EnsureSuccessStatusCode();
        var clinicAppointments = await clinicAppointmentsResponse.Content.ReadFromJsonAsync<ApiResponse<List<AppointmentResponseDto>>>();
        var cancelledAppointment = Assert.Single(clinicAppointments?.Data ?? []);
        Assert.Equal(AppointmentStatus.Cancelled, cancelledAppointment.Status);
    }

    [Fact]
    public async Task User_cannot_read_another_doctors_private_profile()
    {
        var scenario = await CreateClinicScenarioAsync("private-profile");
        const string otherEmail = "profile.other@example.com";
        await SeedUserAsync(otherEmail, "Other User");

        var otherTokens = await LoginAsync(otherEmail);
        SetBearerToken(otherTokens.AccessToken);
        var readResponse = await _client.GetAsync($"/api/doctors/{scenario.DoctorUserId}/profile");

        Assert.Equal(HttpStatusCode.Forbidden, readResponse.StatusCode);
    }

    [Fact]
    public async Task Clinic_booking_endpoints_require_authentication_and_staff_date_filter()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var clinicId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/appointments/mine")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.GetAsync($"/api/clinics/{clinicId}/appointments?date=2030-01-07")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.GetAsync($"/api/clinics/{clinicId}/doctors/{userId}/availability/slots?date=2030-01-07")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.PostAsync($"/api/appointments/{Guid.NewGuid()}/cancel", content: null)).StatusCode);

        var userEmail = "booking.missing-date@example.com";
        await SeedUserAsync(userEmail, "Missing Date User");
        var tokens = await LoginAsync(userEmail);
        SetBearerToken(tokens.AccessToken);

        var missingDateResponse = await _client.GetAsync($"/api/clinics/{clinicId}/appointments");
        Assert.Equal(HttpStatusCode.BadRequest, missingDateResponse.StatusCode);
    }

    [Fact]
    public async Task Clinic_timezone_schedule_boundaries_and_booking_payload_are_validated()
    {
        var scenario = await CreateClinicScenarioAsync("validation");
        SetBearerToken(scenario.OwnerTokens.AccessToken);

        var invalidTimeZoneResponse = await _client.PostAsJsonAsync(
            "/api/clinics",
            CreateClinicRequest("Unsupported zone", timeZoneId: "Not/A-Time-Zone"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidTimeZoneResponse.StatusCode);

        var invalidHoursResponse = await _client.PostAsJsonAsync(
            "/api/clinics",
            CreateClinicRequest("Invalid hours", openTime: "17:00:00", closeTime: "08:00:00"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidHoursResponse.StatusCode);

        SetBearerToken(scenario.DoctorTokens.AccessToken);
        var invalidDayResponse = await _client.PostAsJsonAsync(
            AvailabilityUrl(scenario),
            CreateAvailabilityRequest(dayOfWeek: 7));
        Assert.Equal(HttpStatusCode.BadRequest, invalidDayResponse.StatusCode);

        var invalidDurationResponse = await _client.PostAsJsonAsync(
            AvailabilityUrl(scenario),
            CreateAvailabilityRequest(slotDurationMinutes: 14));
        Assert.Equal(HttpStatusCode.BadRequest, invalidDurationResponse.StatusCode);

        var nonDivisibleWindowResponse = await _client.PostAsJsonAsync(
            AvailabilityUrl(scenario),
            CreateAvailabilityRequest(endTime: "10:15:00"));
        Assert.Equal(HttpStatusCode.BadRequest, nonDivisibleWindowResponse.StatusCode);

        var outsideClinicHoursResponse = await _client.PostAsJsonAsync(
            AvailabilityUrl(scenario),
            CreateAvailabilityRequest(startTime: "07:30:00", endTime: "08:30:00"));
        Assert.Equal(HttpStatusCode.BadRequest, outsideClinicHoursResponse.StatusCode);

        var invalidAppointmentResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/doctors/{scenario.DoctorUserId}/appointments",
            new CreateAppointmentRequestDto());
        Assert.Equal(HttpStatusCode.BadRequest, invalidAppointmentResponse.StatusCode);
    }

    [Fact]
    public async Task Schedule_windows_allow_adjacent_ranges_and_reject_overlaps()
    {
        var scenario = await CreateClinicScenarioAsync("schedule");
        SetBearerToken(scenario.DoctorTokens.AccessToken);
        var availabilityUrl = AvailabilityUrl(scenario);

        var firstResponse = await _client.PostAsJsonAsync(
            availabilityUrl,
            CreateAvailabilityRequest(startTime: "09:00:00", endTime: "10:00:00"));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<ApiResponse<DoctorWeeklyAvailabilityDto>>();
        Assert.NotNull(first?.Data);

        var adjacentResponse = await _client.PostAsJsonAsync(
            availabilityUrl,
            CreateAvailabilityRequest(startTime: "10:00:00", endTime: "11:00:00"));
        Assert.Equal(HttpStatusCode.Created, adjacentResponse.StatusCode);

        var overlappingResponse = await _client.PostAsJsonAsync(
            availabilityUrl,
            CreateAvailabilityRequest(startTime: "09:30:00", endTime: "10:30:00"));
        Assert.Equal(HttpStatusCode.Conflict, overlappingResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync(
            $"{availabilityUrl}/{first.Data.Id}",
            CreateAvailabilityRequest(startTime: "08:30:00", endTime: "09:30:00"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<DoctorWeeklyAvailabilityDto>>();
        Assert.Equal(new TimeOnly(8, 30), updated?.Data?.StartTime);

        var unknownUpdateResponse = await _client.PutAsJsonAsync(
            $"{availabilityUrl}/{Guid.NewGuid()}",
            CreateAvailabilityRequest());
        Assert.Equal(HttpStatusCode.NotFound, unknownUpdateResponse.StatusCode);

        var scheduleResponse = await _client.GetAsync(availabilityUrl);
        scheduleResponse.EnsureSuccessStatusCode();
        var schedule = await scheduleResponse.Content.ReadFromJsonAsync<ApiResponse<List<DoctorWeeklyAvailabilityDto>>>();
        Assert.Equal(2, schedule?.Data?.Count);

        var adjacent = Assert.Single(schedule!.Data!, item => item.StartTime == new TimeOnly(10, 0));
        var deleteResponse = await _client.DeleteAsync($"{availabilityUrl}/{adjacent.Id}");
        deleteResponse.EnsureSuccessStatusCode();
        var deleteResult = await deleteResponse.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        Assert.True(deleteResult?.Data);

        var unknownDeleteResponse = await _client.DeleteAsync($"{availabilityUrl}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, unknownDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Schedule_and_appointment_views_enforce_clinic_roles_and_doctor_scope()
    {
        var scenario = await CreateClinicScenarioAsync("role-scope");
        var secondDoctor = await AddDoctorToClinicAsync(scenario, "second-doctor");
        var localDate = new DateOnly(2030, 1, 7);
        var firstStart = LocalClinicTimeToUtc(localDate, new TimeOnly(9, 0));
        var secondStart = LocalClinicTimeToUtc(localDate, new TimeOnly(10, 0));
        var firstAppointmentId = await SeedAppointmentAsync(
            scenario.ClinicId,
            scenario.DoctorProfileId,
            scenario.PatientUserId,
            firstStart,
            firstStart.AddMinutes(30));
        var secondAppointmentId = await SeedAppointmentAsync(
            scenario.ClinicId,
            secondDoctor.ProfileId,
            scenario.OtherPatientUserId,
            secondStart,
            secondStart.AddMinutes(30));

        SetBearerToken(scenario.PatientTokens.AccessToken);
        var patientScheduleResponse = await _client.GetAsync(
            AvailabilityUrl(scenario));
        Assert.Equal(HttpStatusCode.Forbidden, patientScheduleResponse.StatusCode);

        var patientClinicAppointmentsResponse = await _client.GetAsync(
            ClinicAppointmentsUrl(scenario.ClinicId, localDate));
        Assert.Equal(HttpStatusCode.Forbidden, patientClinicAppointmentsResponse.StatusCode);

        SetBearerToken(scenario.DoctorTokens.AccessToken);
        var otherDoctorsScheduleResponse = await _client.GetAsync(
            AvailabilityUrl(scenario.ClinicId, secondDoctor.UserId));
        Assert.Equal(HttpStatusCode.Forbidden, otherDoctorsScheduleResponse.StatusCode);

        var doctorAppointmentsResponse = await _client.GetAsync(
            ClinicAppointmentsUrl(scenario.ClinicId, localDate));
        doctorAppointmentsResponse.EnsureSuccessStatusCode();
        var doctorAppointments = await doctorAppointmentsResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<AppointmentResponseDto>>>();
        var doctorAppointment = Assert.Single(doctorAppointments?.Data ?? []);
        Assert.Equal(firstAppointmentId, doctorAppointment.Id);

        SetBearerToken(scenario.OwnerTokens.AccessToken);
        var ownerAppointmentsResponse = await _client.GetAsync(
            ClinicAppointmentsUrl(scenario.ClinicId, localDate));
        ownerAppointmentsResponse.EnsureSuccessStatusCode();
        var ownerAppointments = await ownerAppointmentsResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<AppointmentResponseDto>>>();
        Assert.Equal(
            new[] { firstAppointmentId, secondAppointmentId }.OrderBy(id => id),
            ownerAppointments?.Data?.Select(item => item.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task Appointment_lifecycle_rejects_unauthorized_and_invalid_transitions()
    {
        var scenario = await CreateClinicScenarioAsync("lifecycle");
        var now = DateTimeOffset.UtcNow;
        var endedAppointmentId = await SeedAppointmentAsync(
            scenario.ClinicId,
            scenario.DoctorProfileId,
            scenario.PatientUserId,
            now.AddHours(-2),
            now.AddHours(-1));
        var startedAppointmentId = await SeedAppointmentAsync(
            scenario.ClinicId,
            scenario.DoctorProfileId,
            scenario.PatientUserId,
            now.AddMinutes(-5),
            now.AddMinutes(25));

        SetBearerToken(scenario.OtherPatientTokens.AccessToken);
        var unauthorizedCompletionResponse = await _client.PostAsync(
            $"/api/appointments/{endedAppointmentId}/complete",
            content: null);
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedCompletionResponse.StatusCode);

        SetBearerToken(scenario.PatientTokens.AccessToken);
        var patientCompletionResponse = await _client.PostAsync(
            $"/api/appointments/{endedAppointmentId}/complete",
            content: null);
        Assert.Equal(HttpStatusCode.Forbidden, patientCompletionResponse.StatusCode);

        var startedCancellationResponse = await _client.PostAsync(
            $"/api/appointments/{startedAppointmentId}/cancel",
            content: null);
        Assert.Equal(HttpStatusCode.Conflict, startedCancellationResponse.StatusCode);

        SetBearerToken(scenario.DoctorTokens.AccessToken);
        var completedResponse = await _client.PostAsync(
            $"/api/appointments/{endedAppointmentId}/complete",
            content: null);
        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);
        var completed = await completedResponse.Content
            .ReadFromJsonAsync<ApiResponse<AppointmentResponseDto>>();
        Assert.Equal(AppointmentStatus.Completed, completed?.Data?.Status);

        var repeatedCompletionResponse = await _client.PostAsync(
            $"/api/appointments/{endedAppointmentId}/complete",
            content: null);
        Assert.Equal(HttpStatusCode.Conflict, repeatedCompletionResponse.StatusCode);

        var unknownAppointmentResponse = await _client.PostAsync(
            $"/api/appointments/{Guid.NewGuid()}/cancel",
            content: null);
        Assert.Equal(HttpStatusCode.NotFound, unknownAppointmentResponse.StatusCode);
    }

    [Fact]
    public async Task Availability_slots_use_clinic_local_time_and_hide_past_or_booked_slots()
    {
        var scenario = await CreateClinicScenarioAsync("slot-time");
        SetBearerToken(scenario.DoctorTokens.AccessToken);
        var createScheduleResponse = await _client.PostAsJsonAsync(
            AvailabilityUrl(scenario),
            CreateAvailabilityRequest(startTime: "09:00:00", endTime: "10:00:00"));
        Assert.Equal(HttpStatusCode.Created, createScheduleResponse.StatusCode);

        var date = NextMondayInClinicTime();
        SetBearerToken(scenario.PatientTokens.AccessToken);
        var slotsResponse = await _client.GetAsync(AvailabilitySlotsUrl(scenario, date));
        slotsResponse.EnsureSuccessStatusCode();
        var slots = await slotsResponse.Content.ReadFromJsonAsync<ApiResponse<List<AvailabilitySlotDto>>>();
        Assert.NotNull(slots?.Data);
        Assert.Equal(2, slots.Data.Count);
        Assert.Equal(date.ToDateTime(new TimeOnly(9, 0)), slots.Data[0].LocalStart);

        var clinicTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var expectedUtcStart = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(
            date.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Unspecified),
            clinicTimeZone));
        Assert.Equal(expectedUtcStart, slots.Data[0].StartsAtUtc);

        var bookingResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/doctors/{scenario.DoctorUserId}/appointments",
            new CreateAppointmentRequestDto { StartsAtUtc = slots.Data[0].StartsAtUtc });
        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);

        var remainingSlotsResponse = await _client.GetAsync(AvailabilitySlotsUrl(scenario, date));
        remainingSlotsResponse.EnsureSuccessStatusCode();
        var remainingSlots = await remainingSlotsResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<AvailabilitySlotDto>>>();
        Assert.Single(remainingSlots?.Data ?? []);

        var pastDateResponse = await _client.GetAsync(
            AvailabilitySlotsUrl(scenario, new DateOnly(2000, 1, 1)));
        Assert.Equal(HttpStatusCode.BadRequest, pastDateResponse.StatusCode);

        var unscheduledDateResponse = await _client.GetAsync(
            AvailabilitySlotsUrl(scenario, date.AddDays(1)));
        unscheduledDateResponse.EnsureSuccessStatusCode();
        var unscheduledDate = await unscheduledDateResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<AvailabilitySlotDto>>>();
        Assert.Empty(unscheduledDate?.Data ?? []);
    }

    [Fact]
    public async Task Daylight_saving_gap_and_ambiguous_clinic_times_are_not_offered()
    {
        var scenario = await CreateClinicScenarioAsync(
            "dst",
            openTime: "00:00:00",
            closeTime: "23:59:00");
        SetBearerToken(scenario.DoctorTokens.AccessToken);
        var scheduleResponse = await _client.PostAsJsonAsync(
            AvailabilityUrl(scenario),
            CreateAvailabilityRequest(
                dayOfWeek: (int)DayOfWeek.Sunday,
                startTime: "02:00:00",
                endTime: "03:00:00",
                slotDurationMinutes: 15));
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);

        SetBearerToken(scenario.PatientTokens.AccessToken);
        foreach (var date in new[] { new DateOnly(2027, 3, 28), new DateOnly(2027, 10, 31) })
        {
            var response = await _client.GetAsync(AvailabilitySlotsUrl(scenario, date));
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AvailabilitySlotDto>>>();
            Assert.Empty(result?.Data ?? []);
        }
    }

    [Fact]
    public async Task Booking_rejects_unknown_resources_and_start_times_outside_generated_slots()
    {
        var scenario = await CreateClinicScenarioAsync("invalid-booking");
        SetBearerToken(scenario.DoctorTokens.AccessToken);
        var scheduleResponse = await _client.PostAsJsonAsync(
            AvailabilityUrl(scenario),
            CreateAvailabilityRequest());
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);

        SetBearerToken(scenario.PatientTokens.AccessToken);
        var unknownClinicResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{Guid.NewGuid()}/doctors/{scenario.DoctorUserId}/appointments",
            new CreateAppointmentRequestDto
            {
                StartsAtUtc = DateTimeOffset.UtcNow.AddDays(1)
            });
        Assert.Equal(HttpStatusCode.NotFound, unknownClinicResponse.StatusCode);

        var unknownDoctorResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/doctors/{Guid.NewGuid()}/appointments",
            new CreateAppointmentRequestDto
            {
                StartsAtUtc = DateTimeOffset.UtcNow.AddDays(1)
            });
        Assert.Equal(HttpStatusCode.NotFound, unknownDoctorResponse.StatusCode);

        var nextMonday = NextMondayInClinicTime();
        var invalidStart = LocalClinicTimeToUtc(nextMonday, new TimeOnly(9, 1));
        var outsideGeneratedSlotResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/doctors/{scenario.DoctorUserId}/appointments",
            new CreateAppointmentRequestDto { StartsAtUtc = invalidStart });
        Assert.Equal(HttpStatusCode.Conflict, outsideGeneratedSlotResponse.StatusCode);
    }

    [Fact]
    public async Task Clinic_hours_and_timezone_updates_respect_existing_schedule_and_appointments()
    {
        var scenario = await CreateClinicScenarioAsync("clinic-update");
        SetBearerToken(scenario.DoctorTokens.AccessToken);
        var scheduleResponse = await _client.PostAsJsonAsync(
            AvailabilityUrl(scenario),
            CreateAvailabilityRequest());
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);

        SetBearerToken(scenario.OwnerTokens.AccessToken);
        var narrowHoursResponse = await _client.PutAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}",
            CreateClinicUpdateRequest(
                "Updated clinic",
                openTime: "09:30:00",
                closeTime: "17:00:00"));
        Assert.Equal(HttpStatusCode.Conflict, narrowHoursResponse.StatusCode);

        var timeZoneUpdateResponse = await _client.PutAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}",
            CreateClinicUpdateRequest("Updated clinic", timeZoneId: "America/New_York"));
        Assert.Equal(HttpStatusCode.OK, timeZoneUpdateResponse.StatusCode);

        var nextMonday = NextMondayInTimeZone("America/New_York");
        SetBearerToken(scenario.PatientTokens.AccessToken);
        var slotsResponse = await _client.GetAsync(
            AvailabilitySlotsUrl(scenario.ClinicId, scenario.DoctorUserId, nextMonday));
        slotsResponse.EnsureSuccessStatusCode();
        var slots = await slotsResponse.Content.ReadFromJsonAsync<ApiResponse<List<AvailabilitySlotDto>>>();
        Assert.NotNull(slots?.Data);
        Assert.Equal(2, slots.Data.Count);

        var bookingResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/doctors/{scenario.DoctorUserId}/appointments",
            new CreateAppointmentRequestDto { StartsAtUtc = slots.Data[0].StartsAtUtc });
        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);

        SetBearerToken(scenario.OwnerTokens.AccessToken);
        var changedTimeZoneAfterBookingResponse = await _client.PutAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}",
            CreateClinicUpdateRequest("Updated clinic", timeZoneId: "Asia/Tokyo"));
        Assert.Equal(HttpStatusCode.Conflict, changedTimeZoneAfterBookingResponse.StatusCode);
    }

    [Fact]
    public async Task Clinic_appointment_date_filter_uses_local_day_boundaries()
    {
        var scenario = await CreateClinicScenarioAsync("local-date");
        var date = new DateOnly(2030, 1, 7);
        var localLateStart = LocalClinicTimeToUtc(date, new TimeOnly(23, 30));
        var nextDayMidnight = LocalClinicTimeToUtc(date.AddDays(1), TimeOnly.MinValue);
        var lateAppointmentId = await SeedAppointmentAsync(
            scenario.ClinicId,
            scenario.DoctorProfileId,
            scenario.PatientUserId,
            localLateStart,
            localLateStart.AddMinutes(30));
        await SeedAppointmentAsync(
            scenario.ClinicId,
            scenario.DoctorProfileId,
            scenario.PatientUserId,
            nextDayMidnight,
            nextDayMidnight.AddMinutes(30));

        SetBearerToken(scenario.OwnerTokens.AccessToken);
        var response = await _client.GetAsync(ClinicAppointmentsUrl(scenario.ClinicId, date));
        response.EnsureSuccessStatusCode();
        var appointments = await response.Content.ReadFromJsonAsync<ApiResponse<List<AppointmentResponseDto>>>();

        var appointment = Assert.Single(appointments?.Data ?? []);
        Assert.Equal(lateAppointmentId, appointment.Id);
        Assert.Equal(date.ToDateTime(new TimeOnly(23, 30)), appointment.LocalStart);
    }

    [PostgresFact]
    public async Task Concurrent_requests_for_the_same_slot_create_only_one_appointment()
    {
        var scenario = await CreateClinicScenarioAsync("concurrent-booking");
        SetBearerToken(scenario.DoctorTokens.AccessToken);
        var scheduleResponse = await _client.PostAsJsonAsync(
            AvailabilityUrl(scenario),
            CreateAvailabilityRequest(endTime: "09:30:00"));
        Assert.Equal(HttpStatusCode.Created, scheduleResponse.StatusCode);

        var date = NextMondayInClinicTime();
        SetBearerToken(scenario.PatientTokens.AccessToken);
        var slotsResponse = await _client.GetAsync(AvailabilitySlotsUrl(scenario, date));
        slotsResponse.EnsureSuccessStatusCode();
        var slots = await slotsResponse.Content.ReadFromJsonAsync<ApiResponse<List<AvailabilitySlotDto>>>();
        var slot = Assert.Single(slots?.Data ?? []);

        var bookingUrl = $"/api/clinics/{scenario.ClinicId}/doctors/{scenario.DoctorUserId}/appointments";
        var bookingRequest = new CreateAppointmentRequestDto { StartsAtUtc = slot.StartsAtUtc };
        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync(bookingUrl, bookingRequest),
            _client.PostAsJsonAsync(bookingUrl, bookingRequest));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        var appointmentsResponse = await _client.GetAsync("/api/appointments/mine");
        appointmentsResponse.EnsureSuccessStatusCode();
        var appointments = await appointmentsResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<AppointmentResponseDto>>>();
        Assert.Single(appointments?.Data ?? []);
    }

    [Fact]
    public async Task Clinic_member_roles_cannot_escalate_and_inactive_memberships_can_be_reactivated()
    {
        var scenario = await CreateClinicScenarioAsync("membership");
        const string administratorEmail = "clinic-membership.admin@example.com";
        const string receptionistEmail = "clinic-membership.reception@example.com";
        var administratorId = await SeedUserAsync(administratorEmail, "Clinic Administrator");
        await SeedUserAsync(receptionistEmail, "Clinic Receptionist");

        SetBearerToken(scenario.OwnerTokens.AccessToken);
        var administratorResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/members",
            new AddClinicMembershipRequestDto
            {
                Email = administratorEmail,
                Role = ClinicRole.ClinicAdministrator
            });
        Assert.Equal(HttpStatusCode.Created, administratorResponse.StatusCode);

        var removeAdministratorResponse = await _client.DeleteAsync(
            $"/api/clinics/{scenario.ClinicId}/members/{administratorId}");
        Assert.Equal(HttpStatusCode.BadRequest, removeAdministratorResponse.StatusCode);

        var removeOwnerResponse = await _client.DeleteAsync(
            $"/api/clinics/{scenario.ClinicId}/members/{scenario.OwnerId}");
        Assert.Equal(HttpStatusCode.BadRequest, removeOwnerResponse.StatusCode);

        var ownerEscalationResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/members",
            new AddClinicMembershipRequestDto
            {
                UserId = Guid.NewGuid(),
                Role = ClinicRole.Owner
            });
        Assert.Equal(HttpStatusCode.BadRequest, ownerEscalationResponse.StatusCode);

        var administratorTokens = await LoginAsync(administratorEmail);
        SetBearerToken(administratorTokens.AccessToken);
        var administratorEscalationResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/members",
            new AddClinicMembershipRequestDto
            {
                UserId = Guid.NewGuid(),
                Role = ClinicRole.ClinicAdministrator
            });
        Assert.Equal(HttpStatusCode.Forbidden, administratorEscalationResponse.StatusCode);

        var receptionistResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/members",
            new AddClinicMembershipRequestDto
            {
                Email = receptionistEmail,
                Role = ClinicRole.Receptionist
            });
        Assert.Equal(HttpStatusCode.Created, receptionistResponse.StatusCode);

        SetBearerToken(scenario.OwnerTokens.AccessToken);
        var patientMembershipResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/members",
            new AddClinicMembershipRequestDto
            {
                UserId = scenario.PatientUserId,
                Role = ClinicRole.Patient
            });
        Assert.Equal(HttpStatusCode.Created, patientMembershipResponse.StatusCode);
        var patientMembership = await patientMembershipResponse.Content
            .ReadFromJsonAsync<ApiResponse<ClinicMembershipResponseDto>>();
        Assert.NotNull(patientMembership?.Data);

        var deleteMembershipResponse = await _client.DeleteAsync(
            $"/api/clinics/{scenario.ClinicId}/members/{scenario.PatientUserId}");
        deleteMembershipResponse.EnsureSuccessStatusCode();

        var reactivatedMembershipResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/members",
            new AddClinicMembershipRequestDto
            {
                Email = "membership.patient@example.com",
                Role = ClinicRole.Patient
            });
        Assert.Equal(HttpStatusCode.OK, reactivatedMembershipResponse.StatusCode);
        var reactivatedMembership = await reactivatedMembershipResponse.Content
            .ReadFromJsonAsync<ApiResponse<ClinicMembershipResponseDto>>();
        Assert.Equal(patientMembership.Data.Id, reactivatedMembership?.Data?.Id);
        Assert.True(reactivatedMembership?.Data?.IsActive);
        Assert.NotEqual(Guid.Empty, administratorId);
    }

    [Fact]
    public async Task Receptionist_can_cancel_a_future_appointment_and_patient_lists_are_isolated()
    {
        var scenario = await CreateClinicScenarioAsync("staff-cancel");
        const string receptionistEmail = "staff-cancel.reception@example.com";
        await SeedUserAsync(receptionistEmail, "Front Desk");

        SetBearerToken(scenario.OwnerTokens.AccessToken);
        var membershipResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/members",
            new AddClinicMembershipRequestDto
            {
                Email = receptionistEmail,
                Role = ClinicRole.Receptionist
            });
        Assert.Equal(HttpStatusCode.Created, membershipResponse.StatusCode);

        var now = DateTimeOffset.UtcNow;
        var firstAppointmentId = await SeedAppointmentAsync(
            scenario.ClinicId,
            scenario.DoctorProfileId,
            scenario.PatientUserId,
            now.AddDays(1),
            now.AddDays(1).AddMinutes(30));
        await SeedAppointmentAsync(
            scenario.ClinicId,
            scenario.DoctorProfileId,
            scenario.OtherPatientUserId,
            now.AddDays(1).AddHours(1),
            now.AddDays(1).AddHours(1).AddMinutes(30));

        SetBearerToken(scenario.PatientTokens.AccessToken);
        var patientAppointmentsResponse = await _client.GetAsync("/api/appointments/mine");
        patientAppointmentsResponse.EnsureSuccessStatusCode();
        var patientAppointments = await patientAppointmentsResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<AppointmentResponseDto>>>();
        var patientAppointment = Assert.Single(patientAppointments?.Data ?? []);
        Assert.Equal(firstAppointmentId, patientAppointment.Id);

        var receptionistTokens = await LoginAsync(receptionistEmail);
        SetBearerToken(receptionistTokens.AccessToken);
        var cancelResponse = await _client.PostAsync(
            $"/api/appointments/{firstAppointmentId}/cancel",
            content: null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
    }

    private async Task<ClinicTestScenario> CreateClinicScenarioAsync(
        string prefix,
        string openTime = "08:00:00",
        string closeTime = "17:00:00",
        string timeZoneId = "Europe/Warsaw")
    {
        var normalizedPrefix = prefix.ToLowerInvariant().Replace(' ', '-');
        var ownerEmail = $"{normalizedPrefix}.owner@example.com";
        var doctorEmail = $"{normalizedPrefix}.doctor@example.com";
        var patientEmail = $"{normalizedPrefix}.patient@example.com";
        var otherPatientEmail = $"{normalizedPrefix}.other-patient@example.com";

        var ownerId = await SeedUserAsync(ownerEmail, $"{prefix} Owner");
        var doctorId = await SeedUserAsync(doctorEmail, $"{prefix} Doctor");
        var patientId = await SeedUserAsync(patientEmail, $"{prefix} Patient");
        var otherPatientId = await SeedUserAsync(otherPatientEmail, $"{prefix} Other Patient");

        var ownerTokens = await LoginAsync(ownerEmail);
        var doctorTokens = await LoginAsync(doctorEmail);
        var patientTokens = await LoginAsync(patientEmail);
        var otherPatientTokens = await LoginAsync(otherPatientEmail);

        SetBearerToken(ownerTokens.AccessToken);
        var clinicResponse = await _client.PostAsJsonAsync(
            "/api/clinics",
            CreateClinicRequest(
                $"{prefix} Clinic",
                openTime,
                closeTime,
                timeZoneId));
        Assert.Equal(HttpStatusCode.Created, clinicResponse.StatusCode);

        var createdClinic = await clinicResponse.Content
            .ReadFromJsonAsync<ApiResponse<ClinicResponseDto>>();
        Assert.NotNull(createdClinic?.Data);

        SetBearerToken(ownerTokens.AccessToken);
        var membershipResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{createdClinic.Data.Id}/members",
            new AddClinicMembershipRequestDto
            {
                Email = doctorEmail,
                Role = ClinicRole.Doctor
            });
        Assert.Equal(HttpStatusCode.Created, membershipResponse.StatusCode);

        SetBearerToken(doctorTokens.AccessToken);
        var profileResponse = await _client.PostAsJsonAsync(
            $"/api/doctors/{doctorId}/profile",
            new CreateDoctorProfileRequestDto
            {
                FirstName = $"{prefix} Doctor",
                LastName = "Profile"
            });
        Assert.Equal(HttpStatusCode.Created, profileResponse.StatusCode);

        var profile = await profileResponse.Content
            .ReadFromJsonAsync<ApiResponse<DoctorProfileResponseDto>>();
        Assert.NotNull(profile?.Data);

        return new ClinicTestScenario(
            createdClinic.Data.Id,
            ownerId,
            doctorId,
            profile.Data.Id,
            patientId,
            otherPatientId,
            ownerTokens,
            doctorTokens,
            patientTokens,
            otherPatientTokens);
    }

    private async Task<DoctorTestScenario> AddDoctorToClinicAsync(
        ClinicTestScenario scenario,
        string suffix)
    {
        var email = $"booking.{suffix}@example.com";
        var userId = await SeedUserAsync(email, $"Doctor {suffix}");
        var tokens = await LoginAsync(email);

        SetBearerToken(scenario.OwnerTokens.AccessToken);
        var membershipResponse = await _client.PostAsJsonAsync(
            $"/api/clinics/{scenario.ClinicId}/members",
            new AddClinicMembershipRequestDto
            {
                Email = email,
                Role = ClinicRole.Doctor
            });
        Assert.Equal(HttpStatusCode.Created, membershipResponse.StatusCode);

        SetBearerToken(tokens.AccessToken);
        var profileResponse = await _client.PostAsJsonAsync(
            $"/api/doctors/{userId}/profile",
            new CreateDoctorProfileRequestDto
            {
                FirstName = $"Doctor {suffix}",
                LastName = "Profile"
            });
        Assert.Equal(HttpStatusCode.Created, profileResponse.StatusCode);
        var profile = await profileResponse.Content
            .ReadFromJsonAsync<ApiResponse<DoctorProfileResponseDto>>();
        Assert.NotNull(profile?.Data);

        return new DoctorTestScenario(userId, profile.Data.Id, tokens);
    }

    private async Task<Guid> SeedAppointmentAsync(
        Guid clinicId,
        Guid doctorProfileId,
        Guid patientUserId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var appointment = new Appointment(
            clinicId,
            doctorProfileId,
            patientUserId,
            startsAt,
            endsAt);

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync();
        return appointment.Id;
    }

    private static CreateClinicRequestDto CreateClinicRequest(
        string name,
        string openTime = "08:00:00",
        string closeTime = "17:00:00",
        string timeZoneId = "Europe/Warsaw")
    {
        return new CreateClinicRequestDto
        {
            Name = name,
            Address = new AddressDto
            {
                Street = "Main Street",
                BuildingNumber = "1",
                City = "Warsaw",
                PostalCode = "00-001",
                Country = "Poland"
            },
            OpenTime = TimeOnly.Parse(openTime, CultureInfo.InvariantCulture),
            CloseTime = TimeOnly.Parse(closeTime, CultureInfo.InvariantCulture),
            TimeZoneId = timeZoneId
        };
    }

    private static UpdateClinicRequestDto CreateClinicUpdateRequest(
        string name,
        string openTime = "08:00:00",
        string closeTime = "17:00:00",
        string? timeZoneId = null)
    {
        return new UpdateClinicRequestDto
        {
            Name = name,
            Address = new AddressDto
            {
                Street = "Main Street",
                BuildingNumber = "1",
                City = "Warsaw",
                PostalCode = "00-001",
                Country = "Poland"
            },
            OpenTime = TimeOnly.Parse(openTime, CultureInfo.InvariantCulture),
            CloseTime = TimeOnly.Parse(closeTime, CultureInfo.InvariantCulture),
            TimeZoneId = timeZoneId
        };
    }

    private static SetDoctorWeeklyAvailabilityRequestDto CreateAvailabilityRequest(
        int dayOfWeek = (int)DayOfWeek.Monday,
        string startTime = "09:00:00",
        string endTime = "10:00:00",
        int slotDurationMinutes = 30)
    {
        return new SetDoctorWeeklyAvailabilityRequestDto
        {
            DayOfWeek = (DayOfWeek)dayOfWeek,
            StartTime = TimeOnly.Parse(startTime, CultureInfo.InvariantCulture),
            EndTime = TimeOnly.Parse(endTime, CultureInfo.InvariantCulture),
            SlotDurationMinutes = slotDurationMinutes
        };
    }

    private static string AvailabilityUrl(ClinicTestScenario scenario)
        => AvailabilityUrl(scenario.ClinicId, scenario.DoctorUserId);

    private static string AvailabilityUrl(Guid clinicId, Guid doctorUserId)
        => $"/api/clinics/{clinicId}/doctors/{doctorUserId}/availability";

    private static string AvailabilitySlotsUrl(ClinicTestScenario scenario, DateOnly date)
        => AvailabilitySlotsUrl(scenario.ClinicId, scenario.DoctorUserId, date);

    private static string AvailabilitySlotsUrl(Guid clinicId, Guid doctorUserId, DateOnly date)
        => $"/api/clinics/{clinicId}/doctors/{doctorUserId}/availability/slots?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    private static string ClinicAppointmentsUrl(Guid clinicId, DateOnly date)
        => $"/api/clinics/{clinicId}/appointments?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    private static DateTimeOffset LocalClinicTimeToUtc(DateOnly date, TimeOnly time)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var localTime = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone));
    }

    private static DateOnly NextMondayInTimeZone(string timeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)localToday.DayOfWeek + 7) % 7;
        return localToday.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
    }

    private sealed record ClinicTestScenario(
        Guid ClinicId,
        Guid OwnerId,
        Guid DoctorUserId,
        Guid DoctorProfileId,
        Guid PatientUserId,
        Guid OtherPatientUserId,
        AuthTokenResponse OwnerTokens,
        AuthTokenResponse DoctorTokens,
        AuthTokenResponse PatientTokens,
        AuthTokenResponse OtherPatientTokens);

    private sealed record DoctorTestScenario(
        Guid UserId,
        Guid ProfileId,
        AuthTokenResponse Tokens);

    private async Task<Guid> SeedUserAsync(string email, string displayName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Role = UserRole.User,
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "password123");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<AuthTokenResponse> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = "password123" });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>();
        Assert.NotNull(result?.Data);
        return result.Data;
    }

    private void SetBearerToken(string accessToken)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static DateOnly NextMondayInClinicTime()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)localToday.DayOfWeek + 7) % 7;
        return localToday.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
