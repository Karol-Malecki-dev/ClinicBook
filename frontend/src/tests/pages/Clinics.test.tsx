import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import Clinics from '../../pages/Clinics';
import { clinicApi } from '../../services/api';
import { AppointmentStatus } from '../../types';
import type {
  ApiResponse,
  AppointmentDto,
  AvailabilitySlotDto,
  ClinicDoctorDto,
  ClinicDto,
} from '../../types';
import { vi } from 'vitest';

vi.mock('../../services/api', () => ({
  clinicApi: {
    getDiscoverableClinics: vi.fn(),
    getDoctors: vi.fn(),
    getAvailableSlots: vi.fn(),
    createClinic: vi.fn(),
    bookAppointment: vi.fn(),
  },
}));

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
};

const doctor: ClinicDoctorDto = {
  doctorProfileId: 'profile-1',
  userId: 'doctor-1',
  firstName: 'Anna',
  lastName: 'Nowak',
  bio: '',
  specializations: [],
};

const slot: AvailabilitySlotDto = {
  startsAtUtc: '2026-11-02T08:00:00+00:00',
  endsAtUtc: '2026-11-02T08:30:00+00:00',
  localStart: '2026-11-02T09:00:00',
  localEnd: '2026-11-02T09:30:00',
  timeZoneId: 'Europe/Warsaw',
};

describe('Clinics page', () => {
  beforeEach(() => {
    jest.resetAllMocks();
    mockedClinicApi.getDiscoverableClinics.mockResolvedValue(response([clinic]));
    mockedClinicApi.getDoctors.mockResolvedValue(response([doctor]));
    mockedClinicApi.getAvailableSlots.mockResolvedValue(response([slot]));
    const appointment: AppointmentDto = {
      id: 'appointment-1',
      clinicId: clinic.id,
      clinicName: clinic.name,
      doctorProfileId: doctor.doctorProfileId,
      doctorUserId: doctor.userId,
      doctorName: `${doctor.firstName} ${doctor.lastName}`,
      patientUserId: 'patient-1',
      patientName: 'Pacjent',
      patientEmail: 'patient@example.com',
      startsAtUtc: slot.startsAtUtc,
      endsAtUtc: slot.endsAtUtc,
      localStart: slot.localStart,
      localEnd: slot.localEnd,
      timeZoneId: slot.timeZoneId,
      status: AppointmentStatus.Booked,
      createdAtUtc: '2026-10-30T08:00:00+00:00',
    };
    mockedClinicApi.bookAppointment.mockResolvedValue(response(appointment));
  });

  it('books a slot returned by the clinic availability API', async () => {
    render(<Clinics />);

    const bookButton = await screen.findByRole('button', { name: '09:00–09:30' });
    fireEvent.click(bookButton);

    await waitFor(() => {
      expect(mockedClinicApi.bookAppointment).toHaveBeenCalledWith(
        'clinic-1',
        'doctor-1',
        { startsAtUtc: slot.startsAtUtc },
      );
    });
    expect(await screen.findByRole('status')).toHaveTextContent(/Wizyta zarezerwowana na 2026-11-02 09:00/);
  });

  it('creates a clinic with the device time zone as an editable suggestion', async () => {
    const suggestedTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || 'Europe/Warsaw';
    const createdClinic: ClinicDto = {
      ...clinic,
      id: 'created-clinic',
      name: 'Nowa klinika',
      timeZoneId: suggestedTimeZone,
    };
    mockedClinicApi.getDiscoverableClinics
      .mockResolvedValueOnce(response([]))
      .mockResolvedValueOnce(response([createdClinic]));
    mockedClinicApi.createClinic.mockResolvedValue(response(createdClinic));

    render(<Clinics />);

    fireEvent.click(await screen.findByText('Rejestracja nowej kliniki'));
    fireEvent.change(screen.getByLabelText('Nazwa'), { target: { value: 'Nowa klinika' } });
    fireEvent.change(screen.getByLabelText('Ulica'), { target: { value: 'Leśna' } });
    fireEvent.change(screen.getByLabelText('Numer budynku'), { target: { value: '5' } });
    fireEvent.change(screen.getByLabelText('Miasto'), { target: { value: 'Kraków' } });
    fireEvent.change(screen.getByLabelText('Kod pocztowy'), { target: { value: '30-001' } });
    fireEvent.change(screen.getByLabelText('Strefa czasowa kliniki (IANA)'), {
      target: { value: suggestedTimeZone },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Utwórz klinikę' }));

    await waitFor(() => {
      expect(mockedClinicApi.createClinic).toHaveBeenCalledWith(expect.objectContaining({
        name: 'Nowa klinika',
        openTime: '08:00:00',
        closeTime: '17:00:00',
        timeZoneId: suggestedTimeZone,
        address: expect.objectContaining({
          street: 'Leśna',
          buildingNumber: '5',
          city: 'Kraków',
          postalCode: '30-001',
          country: 'Polska',
        }),
      }));
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Klinika została utworzona.');
  });

  it('shows an empty state when the selected doctor has no available slots', async () => {
    mockedClinicApi.getAvailableSlots.mockResolvedValue(response([]));

    render(<Clinics />);

    expect(await screen.findByText('Brak dostępnych terminów w wybranym dniu.')).toBeInTheDocument();
  });

  it('shows a booking conflict and refreshes the available slots', async () => {
    mockedClinicApi.bookAppointment.mockRejectedValue(new Error('Slot has just been booked'));

    render(<Clinics />);
    fireEvent.click(await screen.findByRole('button', { name: '09:00–09:30' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Slot has just been booked');
    await waitFor(() => {
      expect(mockedClinicApi.getAvailableSlots).toHaveBeenCalledTimes(2);
    });
  });
});
