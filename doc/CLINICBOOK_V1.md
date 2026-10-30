# ClinicBook V1

V1 delivers the basic clinic appointment lifecycle on top of the existing account and clinic-membership model.

## V1 workflow

1. A clinic owner creates a clinic and confirms its opening hours and IANA time-zone ID.
2. A doctor creates their own doctor profile. A clinic owner or administrator can add an already registered account to the clinic by email.
3. A doctor manages their own schedule, while a clinic owner or administrator can manage any active doctor in that clinic.
4. An authenticated patient discovers a clinic, selects a doctor and date, and books an available slot.
5. Patients can view their appointments and cancel future bookings. Clinic doctors see only their own clinic appointments; receptionists and clinic administrators can view clinic appointments.
6. Authorized clinic staff can mark an appointment completed after it ends.

## Time-zone and schedule rules

- The clinic chooses its time zone. The browser's current time zone is only a suggested value in the clinic form; the clinic can edit it before saving.
- Weekly schedule times are local to the clinic. Appointment start and end instants are stored in UTC and responses include both UTC values and clinic-local display times.
- A schedule window must fit within clinic opening hours, use 15-minute duration increments, and not overlap another window for that doctor in the same clinic.
- Local times that are invalid or ambiguous during a daylight-saving transition are not offered as bookable slots.
- A clinic's time zone cannot be changed after an appointment has been created. Clinic opening hours cannot be narrowed past existing doctor schedules.
- Schedule windows repeat every week. V1 does not include holidays, one-off time off, or exception dates.

## API surface

| Purpose | Endpoint |
|---|---|
| Discover clinics | `GET /api/clinics/discover` |
| Create a clinic | `POST /api/clinics` |
| Create the current user's doctor profile | `POST /api/doctors/{doctorUserId}/profile` |
| Add an existing account to a clinic | `POST /api/clinics/{clinicId}/members` |
| List clinic doctors | `GET /api/clinics/{clinicId}/doctors` |
| Read a doctor's weekly schedule | `GET /api/clinics/{clinicId}/doctors/{doctorUserId}/availability` |
| Create a schedule window | `POST /api/clinics/{clinicId}/doctors/{doctorUserId}/availability` |
| Update a schedule window | `PUT /api/clinics/{clinicId}/doctors/{doctorUserId}/availability/{availabilityId}` |
| Delete a schedule window | `DELETE /api/clinics/{clinicId}/doctors/{doctorUserId}/availability/{availabilityId}` |
| List bookable slots for a local date | `GET /api/clinics/{clinicId}/doctors/{doctorUserId}/availability/slots?date=YYYY-MM-DD` |
| Book a slot returned by the availability API | `POST /api/clinics/{clinicId}/doctors/{doctorUserId}/appointments` |
| List the current patient's appointments | `GET /api/appointments/mine` |
| Cancel an appointment | `POST /api/appointments/{appointmentId}/cancel` |
| List clinic appointments for a required clinic-local date | `GET /api/clinics/{clinicId}/appointments?date=YYYY-MM-DD` |
| Complete an ended appointment | `POST /api/appointments/{appointmentId}/complete` |

Booking revalidates the submitted start instant against the current schedule and existing appointments. PostgreSQL uses a filtered unique index and serializable booking transactions to protect against concurrent double-booking.

## Data-minimization boundary

V1 does not collect a visit reason, diagnosis, clinical notes, or medical records. Appointment responses include patient name and email only for the patient and authorized clinic staff. This booking workflow is not a substitute for a reviewed medical-records system or a production privacy assessment.

## Not included in V1

- One-off availability exceptions, holidays, waitlists, and recurring appointments.
- Appointment reminders or transactional booking emails.
- Billing, online payments, insurance, and electronic health records.
- Automatic geocoding. The clinic must confirm or change the suggested time zone.
- Production hosting, operational monitoring, backup/restore procedures, and deployment-specific secret management.

## Verification commands

Run commands one at a time from the repository root:

```powershell
dotnet test backend\UnitTests\UnitTests.csproj --filter "FullyQualifiedName~DoctorWeeklyAvailabilityTests|FullyQualifiedName~AppointmentTests|FullyQualifiedName~ClinicTimeZoneTests|FullyQualifiedName~ClinicBookingRequestValidatorTests"
dotnet test backend\IntegrationTests\IntegrationTests.csproj --filter FullyQualifiedName~ClinicBookingApiIntegrationTests
Set-Location frontend
npm run test:once -- "src\tests\pages\Clinics.test.tsx" "src\tests\pages\Appointments.test.tsx" "src\tests\pages\ClinicSchedule.test.tsx"
npm run build
```

The API integration test host uses EF Core InMemory by default. InMemory tests do not verify PostgreSQL migrations, filtered unique indexes, or serializable booking races. Those checks require the opt-in PostgreSQL test mode below before production data is used.

### PostgreSQL integration tests

The integration test host uses InMemory by default. To exercise the ClinicBook API against PostgreSQL, first create a disposable database for this run with a name beginning `clinicbook_v1_test_`, then set the connection string only in the current PowerShell process:

```powershell
$env:CLINICBOOK_TEST_POSTGRES_CONNECTION_STRING = "Host=127.0.0.1;Port=55432;Database=clinicbook_v1_test_<run-id>;Username=clinicbook_test;Password=<local-secret>"
try {
    dotnet test backend\IntegrationTests\IntegrationTests.csproj --filter FullyQualifiedName~ClinicBookingApiIntegrationTests
}
finally {
    Remove-Item Env:CLINICBOOK_TEST_POSTGRES_CONNECTION_STRING -ErrorAction SilentlyContinue
}
```

`CustomWebApplicationFactory` rejects non-loopback hosts and database names outside that test prefix. It applies the EF migrations when the test host starts. The test host does not create or drop the PostgreSQL database; provision a fresh disposable database before the run and drop that run-specific database afterward. Never use a production connection string.

The concurrent double-booking test is PostgreSQL-only and is skipped when `CLINICBOOK_TEST_POSTGRES_CONNECTION_STRING` is not set.

The API applies pending EF Core migrations when it starts against a relational database. Review deployment-specific migration and backup procedures before using production data.
