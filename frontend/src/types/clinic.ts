import type { ApiResponse } from './api';

export interface ClinicAddress {
  street: string;
  buildingNumber: string;
  apartmentNumber?: string | null;
  city: string;
  postalCode: string;
  country: string;
}

export interface ClinicDto {
  id: string;
  name: string;
  address: ClinicAddress;
  phoneNumber?: string | null;
  email?: string | null;
  website?: string | null;
  openTime: string;
  closeTime: string;
  timeZoneId: string;
  membershipRole?: number | null;
}

export interface CreateClinicRequest {
  name: string;
  address: ClinicAddress;
  phoneNumber?: string;
  email?: string;
  website?: string;
  openTime: string;
  closeTime: string;
  timeZoneId: string;
}

export enum ClinicRole {
  Patient = 1,
  Doctor = 2,
  Receptionist = 3,
  ClinicAdministrator = 4,
  Owner = 5,
}

export interface AddClinicMemberRequest {
  userId?: string;
  email?: string;
  role: ClinicRole;
}

export interface ClinicMembershipDto {
  id: string;
  userId: string;
  clinicId: string;
  role: ClinicRole;
  isActive: boolean;
  createdAt: string;
}

export interface ClinicDoctorDto {
  doctorProfileId: string;
  userId: string;
  firstName: string;
  lastName: string;
  bio: string;
  specializations: string[];
}

export interface DoctorProfileDto {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  bio: string;
  phoneNumber: string;
  isActive: boolean;
}

export interface CreateDoctorProfileRequest {
  firstName: string;
  lastName: string;
  bio?: string;
  phoneNumber?: string;
}

export interface DoctorWeeklyAvailabilityDto {
  id: string;
  clinicId: string;
  doctorProfileId: string;
  doctorUserId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
}

export interface SetDoctorWeeklyAvailabilityRequest {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
}

export interface AvailabilitySlotDto {
  startsAtUtc: string;
  endsAtUtc: string;
  localStart: string;
  localEnd: string;
  timeZoneId: string;
}

export enum AppointmentStatus {
  Booked = 1,
  Cancelled = 2,
  Completed = 3,
}

export interface AppointmentDto {
  id: string;
  clinicId: string;
  clinicName: string;
  doctorProfileId: string;
  doctorUserId: string;
  doctorName: string;
  patientUserId: string;
  patientName: string;
  patientEmail: string;
  startsAtUtc: string;
  endsAtUtc: string;
  localStart: string;
  localEnd: string;
  timeZoneId: string;
  status: AppointmentStatus;
  createdAtUtc: string;
}

export interface CreateAppointmentRequest {
  startsAtUtc: string;
}

export type ClinicsResponse = ApiResponse<ClinicDto[]>;
export type ClinicResponse = ApiResponse<ClinicDto>;
export type ClinicMembershipResponse = ApiResponse<ClinicMembershipDto>;
export type ClinicMembersResponse = ApiResponse<ClinicMembershipDto[]>;
export type ClinicDoctorsResponse = ApiResponse<ClinicDoctorDto[]>;
export type DoctorProfileResponse = ApiResponse<DoctorProfileDto>;
export type DoctorWeeklyAvailabilityResponse = ApiResponse<DoctorWeeklyAvailabilityDto[]>;
export type AvailabilitySlotsResponse = ApiResponse<AvailabilitySlotDto[]>;
export type AppointmentResponse = ApiResponse<AppointmentDto>;
export type AppointmentsResponse = ApiResponse<AppointmentDto[]>;
export type ClinicOperationResponse = ApiResponse<boolean>;
