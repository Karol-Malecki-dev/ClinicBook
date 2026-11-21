import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { clinicApi } from '../services/api';
import {
  AppointmentStatus,
  type AppointmentDto,
  type ClinicDto,
} from '../types';

const clinicPatientRole = 1;

function getLocalDateValue() {
  const date = new Date();
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function getDateInTimeZone(timeZoneId: string) {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: timeZoneId,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date());
  const year = parts.find((part) => part.type === 'year')?.value;
  const month = parts.find((part) => part.type === 'month')?.value;
  const day = parts.find((part) => part.type === 'day')?.value;

  if (!year || !month || !day) {
    throw new Error('Nie udało się odczytać bieżącej daty w strefie kliniki.');
  }

  return `${year}-${month}-${day}`;
}

function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : 'Nie udało się wykonać operacji.';
}

function formatStatus(status: AppointmentStatus) {
  switch (status) {
    case AppointmentStatus.Booked:
      return 'Zarezerwowana';
    case AppointmentStatus.Cancelled:
      return 'Anulowana';
    case AppointmentStatus.Completed:
      return 'Zakończona';
    default:
      return 'Nieznany status';
  }
}

function AppointmentCard({
  appointment,
  staffView,
  busy,
  onCancel,
  onComplete,
}: {
  appointment: AppointmentDto;
  staffView: boolean;
  busy: boolean;
  onCancel: (appointmentId: string) => void;
  onComplete: (appointmentId: string) => void;
}) {
  const startsInFuture = new Date(appointment.startsAtUtc).getTime() > Date.now();
  const hasEnded = new Date(appointment.endsAtUtc).getTime() <= Date.now();

  return (
    <article className="card">
      <div className="grid grid--2">
        <div>
          <h3>{appointment.clinicName}</h3>
          <p>Lekarz: {appointment.doctorName}</p>
          {staffView ? (
            <p>Pacjent: {appointment.patientName} · {appointment.patientEmail}</p>
          ) : null}
          <p>
            {appointment.localStart.slice(0, 16).replace('T', ' ')}
            {' · '}
            {appointment.timeZoneId}
          </p>
          <p>Status: {formatStatus(appointment.status)}</p>
        </div>
        {appointment.status === AppointmentStatus.Booked ? (
          <div className="hero__actions">
            {startsInFuture ? (
              <button
                className="button button--danger"
                type="button"
                disabled={busy}
                onClick={() => onCancel(appointment.id)}
              >
                {busy ? 'Zapisywanie...' : 'Anuluj wizytę'}
              </button>
            ) : null}
            {staffView && hasEnded ? (
              <button
                className="button"
                type="button"
                disabled={busy}
                onClick={() => onComplete(appointment.id)}
              >
                {busy ? 'Zapisywanie...' : 'Oznacz jako zakończoną'}
              </button>
            ) : null}
          </div>
        ) : null}
      </div>
    </article>
  );
}

export default function Appointments() {
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [clinics, setClinics] = useState<ClinicDto[]>([]);
  const [clinicAppointments, setClinicAppointments] = useState<AppointmentDto[]>([]);
  const [staffClinicId, setStaffClinicId] = useState('');
  const [date, setDate] = useState(getLocalDateValue);
  const [loadingAppointments, setLoadingAppointments] = useState(true);
  const [loadingClinics, setLoadingClinics] = useState(true);
  const [loadingClinicAppointments, setLoadingClinicAppointments] = useState(false);
  const [busyAppointmentId, setBusyAppointmentId] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const clinicAppointmentsRequestId = useRef(0);
  const staffSelection = useRef({ staffClinicId, date });
  staffSelection.current = { staffClinicId, date };

  const staffClinics = useMemo(
    () => clinics.filter((clinic) => clinic.membershipRole && clinic.membershipRole !== clinicPatientRole),
    [clinics],
  );
  const loadPatientAppointments = useCallback(async () => {
    setLoadingAppointments(true);
    try {
      const response = await clinicApi.getMyAppointments();
      if (!response.data) {
        throw new Error('API nie zwróciło listy wizyt.');
      }

      setAppointments(response.data);
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoadingAppointments(false);
    }
  }, []);

  const loadClinics = useCallback(async () => {
    setLoadingClinics(true);
    try {
      const response = await clinicApi.getMyClinics();
      if (!response.data) {
        throw new Error('API nie zwróciło listy klinik.');
      }

      const clinicItems = response.data;
      setClinics(clinicItems);
      const clinic = clinicItems.find((item) =>
        item.membershipRole && item.membershipRole !== clinicPatientRole);
      setStaffClinicId(clinic?.id ?? '');
      if (clinic) {
        setDate(getDateInTimeZone(clinic.timeZoneId));
      }
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoadingClinics(false);
    }
  }, []);

  const loadClinicAppointments = useCallback(async () => {
    const requestSelection = { staffClinicId, date };
    if (staffSelection.current.staffClinicId !== staffClinicId
      || staffSelection.current.date !== date) {
      return;
    }

    const requestId = ++clinicAppointmentsRequestId.current;
    const isCurrentRequest = () => requestId === clinicAppointmentsRequestId.current
      && staffSelection.current.staffClinicId === requestSelection.staffClinicId
      && staffSelection.current.date === requestSelection.date;

    if (!staffClinicId) {
      setClinicAppointments([]);
      setLoadingClinicAppointments(false);
      return;
    }

    setLoadingClinicAppointments(true);
    try {
      const response = await clinicApi.getClinicAppointments(staffClinicId, date);
      if (!response.data) {
        throw new Error('API nie zwróciło listy wizyt kliniki.');
      }

      if (!isCurrentRequest()) {
        return;
      }

      setClinicAppointments(response.data);
    } catch (loadError) {
      if (!isCurrentRequest()) {
        return;
      }

      setError(getErrorMessage(loadError));
      setClinicAppointments([]);
    } finally {
      if (isCurrentRequest()) {
        setLoadingClinicAppointments(false);
      }
    }
  }, [date, staffClinicId]);

  useEffect(() => {
    void loadPatientAppointments();
    void loadClinics();
  }, [loadPatientAppointments, loadClinics]);

  useEffect(() => {
    void loadClinicAppointments();
    return () => {
      clinicAppointmentsRequestId.current += 1;
    };
  }, [loadClinicAppointments]);

  const changeAppointment = async (
    appointmentId: string,
    action: 'cancel' | 'complete',
  ) => {
    setBusyAppointmentId(appointmentId);
    setError('');
    setNotice('');
    try {
      if (action === 'cancel') {
        await clinicApi.cancelAppointment(appointmentId);
        setNotice('Wizyta została anulowana.');
      } else {
        await clinicApi.completeAppointment(appointmentId);
        setNotice('Wizyta została oznaczona jako zakończona.');
      }

      await Promise.all([loadPatientAppointments(), loadClinicAppointments()]);
    } catch (actionError) {
      setError(getErrorMessage(actionError));
    } finally {
      setBusyAppointmentId('');
    }
  };

  return (
    <section className="page-shell">
      <p className="eyebrow">ClinicBook</p>
      <h1>Wizyty</h1>
      {error ? <div className="page-state" role="alert">{error}</div> : null}
      {notice ? <div className="page-state" role="status">{notice}</div> : null}

      <div className="stack">
        <h2>Moje wizyty</h2>
        {loadingAppointments ? <p>Ładowanie wizyt...</p> : appointments.length === 0 ? (
          <div className="page-state">
            <p>Nie masz jeszcze wizyt.</p>
            <Link className="button" to="/clinics">Znajdź termin</Link>
          </div>
        ) : appointments.map((appointment) => (
          <AppointmentCard
            key={appointment.id}
            appointment={appointment}
            staffView={false}
            busy={busyAppointmentId === appointment.id}
            onCancel={(id) => void changeAppointment(id, 'cancel')}
            onComplete={(id) => void changeAppointment(id, 'complete')}
          />
        ))}
      </div>

      {!loadingClinics && staffClinics.length > 0 ? (
        <div className="stack">
          <h2>Wizyty w klinice</h2>
          <div className="grid grid--2">
            <div className="field">
              <label className="field__label" htmlFor="staff-clinic-select">Klinika</label>
              <select
                id="staff-clinic-select"
                value={staffClinicId}
                onChange={(event) => {
                  const selectedId = event.target.value;
                  clinicAppointmentsRequestId.current += 1;
                  setClinicAppointments([]);
                  setStaffClinicId(selectedId);
                  const clinic = staffClinics.find((item) => item.id === selectedId);
                  if (clinic) {
                    setDate(getDateInTimeZone(clinic.timeZoneId));
                  }
                }}
              >
                {staffClinics.map((clinic) => (
                  <option key={clinic.id} value={clinic.id}>{clinic.name}</option>
                ))}
              </select>
            </div>
            <div className="field">
              <label className="field__label" htmlFor="clinic-appointments-date">Data kliniki</label>
              <input
                id="clinic-appointments-date"
                type="date"
                value={date}
                onChange={(event) => {
                  clinicAppointmentsRequestId.current += 1;
                  setClinicAppointments([]);
                  setDate(event.target.value);
                }}
              />
            </div>
          </div>
          {loadingClinicAppointments ? <p>Ładowanie wizyt kliniki...</p> : clinicAppointments.length === 0 ? (
            <p>Brak wizyt w wybranym dniu.</p>
          ) : clinicAppointments.map((appointment) => (
            <AppointmentCard
              key={appointment.id}
              appointment={appointment}
              staffView
              busy={busyAppointmentId === appointment.id}
              onCancel={(id) => void changeAppointment(id, 'cancel')}
              onComplete={(id) => void changeAppointment(id, 'complete')}
            />
          ))}
        </div>
      ) : null}
    </section>
  );
}
