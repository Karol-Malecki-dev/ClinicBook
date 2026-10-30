import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import ClinicSchedule from '../../pages/ClinicSchedule';
import { useAuth } from '../../hooks/useAuth';
import { clinicApi, HttpError } from '../../services/api';
import { ClinicRole } from '../../types';
import type {
  ApiResponse,
  ClinicDoctorDto,
  ClinicDto,
  ClinicMembershipDto,
  DoctorProfileDto,
  DoctorWeeklyAvailabilityDto,
} from '../../types';
import { vi } from 'vitest';

const clinicApiMocks = vi.hoisted(() => ({
  getMyClinics: vi.fn(),
  getDoctorProfile: vi.fn(),
  createDoctorProfile: vi.fn(),
  getDoctors: vi.fn(),
  getWeeklyAvailability: vi.fn(),
  createWeeklyAvailability: vi.fn(),
  updateWeeklyAvailability: vi.fn(),
  deleteWeeklyAvailability: vi.fn(),
  addClinicMember: vi.fn(),
}));

vi.mock('../../services/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../services/api')>();
  return {
    ...actual,
    clinicApi: clinicApiMocks,
  };
});

vi.mock('../../hooks/useAuth');

const mockedClinicApi = clinicApi as jest.Mocked<typeof clinicApi>;
const mockedUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;

function response<T>(data: T): ApiResponse<T> {
  return {
    statusCode: 200,
    message: 'Success',
    data,
    errors: null,
    timestamp: new Date().toISOString(),
  };
}

const clinic: ClinicDto = {
  id: 'clinic-1',
  name: 'Centrum Medyczne',
  address: {
    street: 'Główna',
    buildingNumber: '1',
    city: 'Warszawa',
    postalCode: '00-001',
    country: 'Polska',
  },
  openTime: '08:00:00',
  closeTime: '17:00:00',
  timeZoneId: 'Europe/Warsaw',
  membershipRole: ClinicRole.Owner,
};

const doctor: ClinicDoctorDto = {
  doctorProfileId: 'doctor-profile-1',
  userId: 'doctor-1',
  firstName: 'Anna',
  lastName: 'Nowak',
  bio: '',
  specializations: [],
};

const profile: DoctorProfileDto = {
  id: doctor.doctorProfileId,
  userId: doctor.userId,
  firstName: doctor.firstName,
  lastName: doctor.lastName,
  bio: 'Lekarka',
  phoneNumber: '',
  isActive: true,
};

const weeklyAvailability: DoctorWeeklyAvailabilityDto = {
  id: 'availability-1',
  clinicId: clinic.id,
  doctorProfileId: doctor.doctorProfileId,
  doctorUserId: doctor.userId,
  dayOfWeek: 1,
  startTime: '09:00:00',
  endTime: '10:00:00',
  slotDurationMinutes: 30,
};

describe('ClinicSchedule page', () => {
  beforeEach(() => {
    jest.resetAllMocks();
    mockedUseAuth.mockReturnValue({
      user: { id: doctor.userId, displayName: 'Anna Nowak', role: 'User' },
    } as any);
    mockedClinicApi.getMyClinics.mockResolvedValue(response([clinic]));
    mockedClinicApi.getDoctorProfile.mockResolvedValue(response(profile));
    mockedClinicApi.createDoctorProfile.mockResolvedValue(response(profile));
    mockedClinicApi.getDoctors.mockResolvedValue(response([doctor]));
    mockedClinicApi.getWeeklyAvailability.mockResolvedValue(response([]));
    mockedClinicApi.createWeeklyAvailability.mockResolvedValue(response(weeklyAvailability));
    mockedClinicApi.updateWeeklyAvailability.mockResolvedValue(response(weeklyAvailability));
    mockedClinicApi.deleteWeeklyAvailability.mockResolvedValue(response(true));
    mockedClinicApi.addClinicMember.mockResolvedValue(response({
      id: 'membership-1',
      userId: 'new-doctor',
      clinicId: clinic.id,
      role: ClinicRole.Doctor,
      isActive: true,
      createdAt: new Date().toISOString(),
    } satisfies ClinicMembershipDto));
  });

  it('creates a weekly schedule through the clinic API', async () => {
    render(<ClinicSchedule />);

    expect(await screen.findByText('Brak skonfigurowanych zakresów.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Dodaj zakres' }));

    await waitFor(() => {
      expect(mockedClinicApi.createWeeklyAvailability).toHaveBeenCalledWith(
        clinic.id,
        doctor.userId,
        {
          dayOfWeek: 1,
          startTime: '09:00:00',
          endTime: '17:00:00',
          slotDurationMinutes: 30,
        },
      );
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Zakres grafiku został dodany.');
  });

  it('edits and deletes an existing schedule window', async () => {
    mockedClinicApi.getWeeklyAvailability.mockResolvedValue(response([weeklyAvailability]));

    render(<ClinicSchedule />);

    expect(await screen.findByText('09:00–10:00 · slot 30 min')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Edytuj' }));
    fireEvent.change(screen.getByLabelText('Od'), { target: { value: '09:30' } });
    fireEvent.click(screen.getByRole('button', { name: 'Zapisz zmiany' }));

    await waitFor(() => {
      expect(mockedClinicApi.updateWeeklyAvailability).toHaveBeenCalledWith(
        clinic.id,
        doctor.userId,
        weeklyAvailability.id,
        {
          dayOfWeek: 1,
          startTime: '09:30:00',
          endTime: '10:00:00',
          slotDurationMinutes: 30,
        },
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Usuń' }));
    await waitFor(() => {
      expect(mockedClinicApi.deleteWeeklyAvailability).toHaveBeenCalledWith(
        clinic.id,
        doctor.userId,
        weeklyAvailability.id,
      );
    });
  });

  it('creates a doctor profile when the current account has none', async () => {
    mockedClinicApi.getDoctorProfile.mockRejectedValue(
      new HttpError(404, 'Doctor profile not found', null),
    );

    render(<ClinicSchedule />);

    fireEvent.click(await screen.findByText('Profil lekarza'));
    fireEvent.change(await screen.findByLabelText('Imię'), { target: { value: 'Jan' } });
    fireEvent.change(screen.getByLabelText('Nazwisko'), { target: { value: 'Kowalski' } });
    fireEvent.click(screen.getByRole('button', { name: 'Utwórz profil lekarza' }));

    await waitFor(() => {
      expect(mockedClinicApi.createDoctorProfile).toHaveBeenCalledWith(doctor.userId, {
        firstName: 'Jan',
        lastName: 'Kowalski',
        bio: undefined,
        phoneNumber: undefined,
      });
    });
    expect(await screen.findByRole('status')).toHaveTextContent(/Profil lekarza został utworzony/);
  });

  it('adds an existing clinic account with the selected clinic role', async () => {
    render(<ClinicSchedule />);

    fireEvent.change(await screen.findByLabelText('E-mail konta'), {
      target: { value: 'new-doctor@example.com' },
    });
    fireEvent.change(screen.getByLabelText('Rola w klinice'), {
      target: { value: String(ClinicRole.Doctor) },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Dodaj do kliniki' }));

    await waitFor(() => {
      expect(mockedClinicApi.addClinicMember).toHaveBeenCalledWith(clinic.id, {
        email: 'new-doctor@example.com',
        role: ClinicRole.Doctor,
      });
    });
    expect(await screen.findByRole('status')).toHaveTextContent(/Konto zostało dodane do kliniki/);
  });

  it('shows availability API errors to the clinic user', async () => {
    mockedClinicApi.getWeeklyAvailability.mockRejectedValue(new Error('Schedule API unavailable'));

    render(<ClinicSchedule />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Schedule API unavailable');
  });
});
