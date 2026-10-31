import type {
  ApiResponse,
  AppointmentResponse,
  AppointmentsResponse,
  AddClinicMemberRequest,
  AvailabilitySlotsResponse,
  ClinicDoctorsResponse,
  ClinicResponse,
  ClinicMembersResponse,
  ClinicMembershipResponse,
  ClinicsResponse,
  CreateAppointmentRequest,
  CreateClinicRequest,
  CreateDoctorProfileRequest,
  DoctorProfileResponse,
  DoctorWeeklyAvailabilityDto,
  DoctorWeeklyAvailabilityResponse,
  SetDoctorWeeklyAvailabilityRequest,
} from '../../types';
import { httpClient, type HttpClient } from './HttpClient';

export class ClinicApi {
  constructor(private readonly client: HttpClient = httpClient) {}

  getDiscoverableClinics(): Promise<ClinicsResponse> {
    return this.client.get<ClinicsResponse>('/clinics/discover');
  }

  getMyClinics(): Promise<ClinicsResponse> {
    return this.client.get<ClinicsResponse>('/clinics');
  }

  getClinicMembers(clinicId: string): Promise<ClinicMembersResponse> {
    return this.client.get<ClinicMembersResponse>(`/clinics/${clinicId}/members`);
  }

  addClinicMember(clinicId: string, request: AddClinicMemberRequest): Promise<ClinicMembershipResponse> {
    return this.client.post<ClinicMembershipResponse, AddClinicMemberRequest>(
      `/clinics/${clinicId}/members`,
      request,
    );
  }

  createClinic(request: CreateClinicRequest): Promise<ClinicResponse> {
    return this.client.post<ClinicResponse, CreateClinicRequest>('/clinics', request);
  }

  getDoctorProfile(doctorUserId: string): Promise<DoctorProfileResponse> {
    return this.client.get<DoctorProfileResponse>(`/doctors/${doctorUserId}/profile`);
  }

  createDoctorProfile(
    doctorUserId: string,
    request: CreateDoctorProfileRequest,
  ): Promise<DoctorProfileResponse> {
    return this.client.post<DoctorProfileResponse, CreateDoctorProfileRequest>(
      `/doctors/${doctorUserId}/profile`,
      request,
    );
  }

  getDoctors(clinicId: string): Promise<ClinicDoctorsResponse> {
    return this.client.get<ClinicDoctorsResponse>(`/clinics/${clinicId}/doctors`);
  }

  getAvailableSlots(
    clinicId: string,
    doctorUserId: string,
    date: string,
  ): Promise<AvailabilitySlotsResponse> {
    const query = new URLSearchParams({ date });
    return this.client.get<AvailabilitySlotsResponse>(
      `/clinics/${clinicId}/doctors/${doctorUserId}/availability/slots?${query.toString()}`,
    );
  }

  getWeeklyAvailability(
    clinicId: string,
    doctorUserId: string,
  ): Promise<DoctorWeeklyAvailabilityResponse> {
    return this.client.get<DoctorWeeklyAvailabilityResponse>(
      `/clinics/${clinicId}/doctors/${doctorUserId}/availability`,
    );
  }

  createWeeklyAvailability(
    clinicId: string,
    doctorUserId: string,
    request: SetDoctorWeeklyAvailabilityRequest,
  ): Promise<ApiResponse<DoctorWeeklyAvailabilityDto>> {
    return this.client.post<ApiResponse<DoctorWeeklyAvailabilityDto>, SetDoctorWeeklyAvailabilityRequest>(
      `/clinics/${clinicId}/doctors/${doctorUserId}/availability`,
      request,
    );
  }

  updateWeeklyAvailability(
    clinicId: string,
    doctorUserId: string,
    availabilityId: string,
    request: SetDoctorWeeklyAvailabilityRequest,
  ): Promise<ApiResponse<DoctorWeeklyAvailabilityDto>> {
    return this.client.put<ApiResponse<DoctorWeeklyAvailabilityDto>, SetDoctorWeeklyAvailabilityRequest>(
      `/clinics/${clinicId}/doctors/${doctorUserId}/availability/${availabilityId}`,
      request,
    );
  }

  deleteWeeklyAvailability(
    clinicId: string,
    doctorUserId: string,
    availabilityId: string,
  ): Promise<ApiResponse<boolean>> {
    return this.client.delete<ApiResponse<boolean>>(
      `/clinics/${clinicId}/doctors/${doctorUserId}/availability/${availabilityId}`,
    );
  }

  bookAppointment(
    clinicId: string,
    doctorUserId: string,
    request: CreateAppointmentRequest,
  ): Promise<AppointmentResponse> {
    return this.client.post<AppointmentResponse, CreateAppointmentRequest>(
      `/clinics/${clinicId}/doctors/${doctorUserId}/appointments`,
      request,
    );
  }

  getMyAppointments(): Promise<AppointmentsResponse> {
    return this.client.get<AppointmentsResponse>('/appointments/mine');
  }

  getClinicAppointments(clinicId: string, date: string): Promise<AppointmentsResponse> {
    const query = `?${new URLSearchParams({ date }).toString()}`;
    return this.client.get<AppointmentsResponse>(`/clinics/${clinicId}/appointments${query}`);
  }

  cancelAppointment(appointmentId: string): Promise<AppointmentResponse> {
    return this.client.post<AppointmentResponse>(`/appointments/${appointmentId}/cancel`);
  }

  completeAppointment(appointmentId: string): Promise<AppointmentResponse> {
    return this.client.post<AppointmentResponse>(`/appointments/${appointmentId}/complete`);
  }
}

export const clinicApi = new ClinicApi();
