import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import Appointments from '../../pages/Appointments';
import { clinicApi } from '../../services/api';
import {
  AppointmentStatus,
  ClinicRole,
} from '../../types';
import type {
  ApiResponse,
  AppointmentDto,
  ClinicDto,
} from '../../types';
import { vi } from 'vitest';

const clinicApiMocks = vi.hoisted(() => ({
  getMyAppointments: vi.fn(),
  getMyClinics: vi.fn(),
  getClinicAppointments: vi.fn(),
  cancelAppointment: vi.fn(),
  completeAppointment: vi.fn(),
}));

vi.mock('../../services/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../services/api')>();
  return {
    ...actual,
    clinicApi: clinicApiMocks,
  };
});

const mockedClinicApi = clinicApi as jest.Mocked<typeof clinicApi>;

function response<T>(data: T): ApiResponse<T> {
  return {
    statusCode: 200,
    message: 'Success',
    data,
    errors: null,
    timestamp: new Date().toISOString(),
  };
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((resolvePromise) => {
    resolve = resolvePromise;
  });
  return { promise, resolve };
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
  membershipRole: ClinicRole.Receptionist,
};

function createAppointment(overrides: Partial<AppointmentDto> = {}): AppointmentDto {
  const startsAtUtc = new Date(Date.now() + 60 * 60 * 1000).toISOString();
  const endsAtUtc = new Date(Date.now() + 90 * 60 * 1000).toISOString();

  return {
    id: 'appointment-1',
    clinicId: clinic.id,
    clinicName: clinic.name,
    doctorProfileId: 'doctor-profile-1',
    doctorUserId: 'doctor-1',
    doctorName: 'Anna Nowak',
    patientUserId: 'patient-1',
    patientName: 'Jan Kowalski',
    patientEmail: 'patient@example.com',
    startsAtUtc,
    endsAtUtc,
    localStart: '2030-01-07T09:00:00',
    localEnd: '2030-01-07T09:30:00',
    timeZoneId: 'Europe/Warsaw',
    status: AppointmentStatus.Booked,
    createdAtUtc: new Date().toISOString(),
    ...overrides,
  };
}

describe('Appointments page', () => {
  beforeEach(() => {
    jest.resetAllMocks();
    mockedClinicApi.getMyAppointments.mockResolvedValue(response([]));
    mockedClinicApi.getMyClinics.mockResolvedValue(response([]));
    mockedClinicApi.getClinicAppointments.mockResolvedValue(response([]));
    mockedClinicApi.cancelAppointment.mockResolvedValue(response(createAppointment({
      status: AppointmentStatus.Cancelled,
    })));
    mockedClinicApi.completeAppointment.mockResolvedValue(response(createAppointment({
      status: AppointmentStatus.Completed,
    })));
  });

  it('shows an empty state and links to clinic discovery when the patient has no visits', async () => {
    render(
      <MemoryRouter>
        <Appointments />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Nie masz jeszcze wizyt.')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Znajdź termin' })).toHaveAttribute('href', '/clinics');
  });

  it('cancels the current patient future appointment', async () => {
    const appointment = createAppointment();
    mockedClinicApi.getMyAppointments.mockResolvedValue(response([appointment]));

    render(
      <MemoryRouter>
        <Appointments />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Lekarz: Anna Nowak')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Anuluj wizytę' }));

    await waitFor(() => {
      expect(mockedClinicApi.cancelAppointment).toHaveBeenCalledWith(appointment.id);
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Wizyta została anulowana.');
  });

  it('shows clinic appointments to staff and marks an ended visit completed', async () => {
    const appointment = createAppointment({
      startsAtUtc: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
      endsAtUtc: new Date(Date.now() - 60 * 60 * 1000).toISOString(),
    });
    mockedClinicApi.getMyClinics.mockResolvedValue(response([clinic]));
    mockedClinicApi.getClinicAppointments.mockResolvedValue(response([appointment]));

    render(
      <MemoryRouter>
        <Appointments />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Pacjent: Jan Kowalski · patient@example.com')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Oznacz jako zakończoną' }));

    await waitFor(() => {
      expect(mockedClinicApi.completeAppointment).toHaveBeenCalledWith(appointment.id);
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Wizyta została oznaczona jako zakończona.');
  });

  it('ignores clinic appointment responses for an earlier filter and allows past dates', async () => {
    const previousRequest = deferred<ApiResponse<AppointmentDto[]>>();
    const previousAppointment = createAppointment({ id: 'previous-appointment', patientName: 'Old patient' });
    const currentAppointment = createAppointment({ id: 'current-appointment', patientName: 'Current patient' });
    mockedClinicApi.getMyClinics.mockResolvedValue(response([clinic]));
    mockedClinicApi.getClinicAppointments
      .mockReturnValueOnce(previousRequest.promise)
      .mockResolvedValueOnce(response([currentAppointment]));

    render(
      <MemoryRouter>
        <Appointments />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(mockedClinicApi.getClinicAppointments).toHaveBeenCalledTimes(1);
    });
    const dateInput = await screen.findByLabelText('Data kliniki');
    expect(dateInput).not.toHaveAttribute('min');
    fireEvent.change(dateInput, { target: { value: '2000-01-01' } });

    expect(await screen.findByText(/Pacjent: Current patient/)).toBeInTheDocument();
    previousRequest.resolve(response([previousAppointment]));

    await waitFor(() => {
      expect(screen.queryByText(/Pacjent: Old patient/)).not.toBeInTheDocument();
      expect(screen.getByText(/Pacjent: Current patient/)).toBeInTheDocument();
    });
  });

  it('shows API errors instead of hiding failed appointment requests', async () => {
    mockedClinicApi.getMyAppointments.mockRejectedValue(new Error('Appointments API unavailable'));

    render(
      <MemoryRouter>
        <Appointments />
      </MemoryRouter>,
    );

    expect(await screen.findByRole('alert')).toHaveTextContent('Appointments API unavailable');
  });
});
