# Clinic Users, Roles, and Access

## Account and profile model

`User` is the authentication account. It stores login data, global account status, contact data, and the global system role.

`PatientProfile` and `DoctorProfile` are separate one-to-one profile records linked to `User`. A profile contains role-specific data and has its own active status.

`ClinicMembership` is the organization boundary. It links one user to one clinic and stores the user's clinic role, active status, and creation time. A user can belong to multiple clinics, but one user has at most one membership per clinic.

## Clinic roles

| Role | Intended responsibility |
| --- | --- |
| `Patient` | Access patient-facing clinic functionality and view permitted clinic data. |
| `Doctor` | Work with doctor-facing clinic functionality and manage rooms. |
| `Receptionist` | Operate clinic administration workflows and manage rooms. |
| `ClinicAdministrator` | Manage clinic members, doctors, rooms, and clinic settings. |
| `Owner` | Full clinic management access. The owner membership cannot be removed through the member-management endpoint. |

`SystemAdministrator` is a global system role and is separate from clinic membership roles. It must not be used as a substitute for membership in a clinic.

## Permission rules

- Every clinic endpoint requires an authenticated user.
- Clinic reads and writes require an active membership in the requested clinic.
- The account must also be active.
- `ClinicAdministrator` and `Owner` can manage clinic members.
- New or changed member assignments are currently limited to `Patient` and `Doctor`.
- `ClinicAdministrator` and `Owner` memberships cannot be removed through the member-management endpoint.
- A user cannot read membership data from another clinic.
- A user without an active membership receives `403 Forbidden` for clinic membership operations.
- A missing member or profile receives `404 Not Found` only after the caller has passed the relevant access check.

## Data isolation

All clinic queries must include the requested `ClinicId` and an active membership check for the current user. Do not authorize a clinic request using only the user ID or only the JWT role claim.

The clinic membership database constraint is unique on `(UserId, ClinicId)`. Deactivation is represented by `IsActive = false`, which preserves the membership history while preventing access.

## Required authorization tests

The test suite should cover unauthenticated requests, inactive accounts or memberships, users from another clinic, management by each clinic role, protected owner/administrator memberships, and successful patient/doctor assignments.
