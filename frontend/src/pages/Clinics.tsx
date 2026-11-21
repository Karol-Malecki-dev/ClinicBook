import { useCallback, useEffect, useRef, useState } from 'react';
import { clinicApi } from '../services/api';
import type {
  AvailabilitySlotDto,
  ClinicDoctorDto,
  ClinicDto,
  CreateClinicRequest,
} from '../types';

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

export default function Clinics() {
  const [clinics, setClinics] = useState<ClinicDto[]>([]);
  const [doctors, setDoctors] = useState<ClinicDoctorDto[]>([]);
  const [slots, setSlots] = useState<AvailabilitySlotDto[]>([]);
  const [clinicId, setClinicId] = useState('');
  const [doctorUserId, setDoctorUserId] = useState('');
  const [date, setDate] = useState(getLocalDateValue);
  const [loadingClinics, setLoadingClinics] = useState(true);
  const [loadingDoctors, setLoadingDoctors] = useState(false);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [savingClinic, setSavingClinic] = useState(false);
  const [bookingSlot, setBookingSlot] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const selectedClinic = clinics.find((clinic) => clinic.id === clinicId);
  const slotsRequestId = useRef(0);
  const slotSelection = useRef({ clinicId, doctorUserId, date });
  slotSelection.current = { clinicId, doctorUserId, date };

  const loadClinics = useCallback(async (preferredClinicId?: string) => {
    setLoadingClinics(true);
    setError('');
    try {
      const response = await clinicApi.getDiscoverableClinics();
      if (!response.data) {
        throw new Error('API nie zwróciło listy klinik.');
      }

      const clinicItems = response.data;
      setClinics(clinicItems);
      const clinic = clinicItems.find((item) => item.id === preferredClinicId) ?? clinicItems[0];
      setClinicId(clinic?.id ?? '');
      if (clinic) {
        setDate(getDateInTimeZone(clinic.timeZoneId));
      }
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoadingClinics(false);
    }
  }, []);

  useEffect(() => {
    void loadClinics();
  }, [loadClinics]);

  useEffect(() => {
    if (!clinicId) {
      setDoctors([]);
      setDoctorUserId('');
      setSlots([]);
      return;
    }

    let active = true;
    setLoadingDoctors(true);
    setError('');
    setDoctors([]);
    setDoctorUserId('');
    setSlots([]);

    void clinicApi.getDoctors(clinicId)
      .then((response) => {
        if (!response.data) {
          throw new Error('API nie zwróciło listy lekarzy.');
        }

        if (active) {
          setDoctors(response.data);
          setDoctorUserId(response.data[0]?.userId ?? '');
        }
      })
      .catch((loadError: unknown) => {
        if (active) {
          setError(getErrorMessage(loadError));
        }
      })
      .finally(() => {
        if (active) {
          setLoadingDoctors(false);
        }
      });

    return () => {
      active = false;
    };
  }, [clinicId]);

  const loadSlots = useCallback(async (preserveError = false) => {
    const requestSelection = { clinicId, doctorUserId, date };
    if (slotSelection.current.clinicId !== clinicId
      || slotSelection.current.doctorUserId !== doctorUserId
      || slotSelection.current.date !== date) {
      return;
    }

    const requestId = ++slotsRequestId.current;
    const isCurrentRequest = () => requestId === slotsRequestId.current
      && slotSelection.current.clinicId === requestSelection.clinicId
      && slotSelection.current.doctorUserId === requestSelection.doctorUserId
      && slotSelection.current.date === requestSelection.date;

    if (!clinicId || !doctorUserId || !date) {
      setSlots([]);
      setLoadingSlots(false);
      return;
    }

    setLoadingSlots(true);
    if (!preserveError) {
      setError('');
    }
    try {
      const response = await clinicApi.getAvailableSlots(clinicId, doctorUserId, date);
      if (!response.data) {
        throw new Error('API nie zwróciło listy dostępnych terminów.');
      }

      if (!isCurrentRequest()) {
        return;
      }

      setSlots(response.data);
    } catch (loadError) {
      if (!isCurrentRequest()) {
        return;
      }

      if (!preserveError) {
        setError(getErrorMessage(loadError));
      }
      setSlots([]);
    } finally {
      if (isCurrentRequest()) {
        setLoadingSlots(false);
      }
    }
  }, [clinicId, doctorUserId, date]);

  useEffect(() => {
    setSlots([]);
    void loadSlots();
    return () => {
      slotsRequestId.current += 1;
    };
  }, [loadSlots]);

  const createClinic = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = event.currentTarget;
    const formData = new FormData(form);
    const timeZoneId = String(formData.get('timeZoneId') ?? '').trim();
    const request: CreateClinicRequest = {
      name: String(formData.get('name') ?? '').trim(),
      address: {
        street: String(formData.get('street') ?? '').trim(),
        buildingNumber: String(formData.get('buildingNumber') ?? '').trim(),
        apartmentNumber: String(formData.get('apartmentNumber') ?? '').trim() || null,
        city: String(formData.get('city') ?? '').trim(),
        postalCode: String(formData.get('postalCode') ?? '').trim(),
        country: String(formData.get('country') ?? '').trim(),
      },
      openTime: `${String(formData.get('openTime'))}:00`,
      closeTime: `${String(formData.get('closeTime'))}:00`,
      timeZoneId,
    };

    setSavingClinic(true);
    setError('');
    setNotice('');
    try {
      const response = await clinicApi.createClinic(request);
      if (!response.data) {
        throw new Error('API nie zwróciło utworzonej kliniki.');
      }

      setNotice('Klinika została utworzona.');
      await loadClinics(response.data.id);
      form.reset();
    } catch (createError) {
      setError(getErrorMessage(createError));
    } finally {
      setSavingClinic(false);
    }
  };

  const bookSlot = async (slot: AvailabilitySlotDto) => {
    if (!clinicId || !doctorUserId) {
      return;
    }

    const requestSelection = { clinicId, doctorUserId, date };
    const isCurrentSelection = () => slotSelection.current.clinicId === requestSelection.clinicId
      && slotSelection.current.doctorUserId === requestSelection.doctorUserId
      && slotSelection.current.date === requestSelection.date;
    setBookingSlot(slot.startsAtUtc);
    setError('');
    setNotice('');
    try {
      const response = await clinicApi.bookAppointment(clinicId, doctorUserId, {
        startsAtUtc: slot.startsAtUtc,
      });
      if (!response.data) {
        throw new Error('API nie zwróciło potwierdzenia wizyty.');
      }

      if (!isCurrentSelection()) {
        return;
      }

      setNotice(`Wizyta zarezerwowana na ${slot.localStart.slice(0, 16).replace('T', ' ')} (${slot.timeZoneId}).`);
      await loadSlots();
    } catch (bookingError) {
      if (!isCurrentSelection()) {
        return;
      }

      const message = getErrorMessage(bookingError);
      await loadSlots(true);
      setError(message);
    } finally {
      if (isCurrentSelection()) {
        setBookingSlot('');
      }
    }
  };

  return (
    <section className="page-shell">
      <p className="eyebrow">ClinicBook</p>
      <h1>Kliniki i rezerwacja wizyt</h1>
      <p>Wybierz klinikę, lekarza i termin. Godziny są wyświetlane według strefy czasowej kliniki.</p>

      {error ? <div className="page-state" role="alert">{error}</div> : null}
      {notice ? <div className="page-state" role="status">{notice}</div> : null}

      <div className="card">
        <h2>Znajdź termin</h2>
        {loadingClinics ? <p>Ładowanie klinik...</p> : clinics.length === 0 ? (
          <p>Nie znaleziono klinik. Zarejestruj placówkę poniżej, aby rozpocząć konfigurację.</p>
        ) : (
          <>
            <div className="grid grid--2">
              <div className="field">
                <label className="field__label" htmlFor="clinic-select">Klinika</label>
                <select
                  id="clinic-select"
                  value={clinicId}
                  onChange={(event) => {
                    const selectedId = event.target.value;
                    slotsRequestId.current += 1;
                    setSlots([]);
                    setBookingSlot('');
                    setClinicId(selectedId);
                    const clinic = clinics.find((item) => item.id === selectedId);
                    if (clinic) {
                      setDate(getDateInTimeZone(clinic.timeZoneId));
                    }
                  }}
                >
                  {clinics.map((clinic) => (
                    <option key={clinic.id} value={clinic.id}>
                      {clinic.name} — {clinic.address.city}
                    </option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label className="field__label" htmlFor="doctor-select">Lekarz</label>
                <select
                  id="doctor-select"
                  value={doctorUserId}
                  onChange={(event) => {
                    slotsRequestId.current += 1;
                    setSlots([]);
                    setBookingSlot('');
                    setDoctorUserId(event.target.value);
                  }}
                  disabled={loadingDoctors || doctors.length === 0}
                >
                  {doctors.length === 0 ? <option value="">Brak lekarzy z profilem w tej klinice</option> : null}
                  {doctors.map((doctor) => (
                    <option key={doctor.userId} value={doctor.userId}>
                      {doctor.firstName} {doctor.lastName}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {selectedClinic ? (
              <p>
                Godziny kliniki: {selectedClinic.openTime.slice(0, 5)}
                {'–'}
                {selectedClinic.closeTime.slice(0, 5)}
                {' · Strefa: '}
                {selectedClinic.timeZoneId}
              </p>
            ) : null}

            <div className="field">
              <label className="field__label" htmlFor="appointment-date">Data wizyty</label>
              <p>Data jest interpretowana w lokalnej strefie kliniki.</p>
              <input
                id="appointment-date"
                type="date"
                min={selectedClinic ? getDateInTimeZone(selectedClinic.timeZoneId) : getLocalDateValue()}
                value={date}
                onChange={(event) => {
                  slotsRequestId.current += 1;
                  setSlots([]);
                  setBookingSlot('');
                  setDate(event.target.value);
                }}
              />
            </div>

            <div className="stack">
              <h3>Dostępne terminy</h3>
              {loadingSlots ? <p>Ładowanie terminów...</p> : slots.length === 0 ? (
                <p>Brak dostępnych terminów w wybranym dniu.</p>
              ) : (
                <div className="hero__actions">
                  {slots.map((slot) => (
                    <button
                      className="button button--ghost"
                      key={slot.startsAtUtc}
                      type="button"
                      disabled={Boolean(bookingSlot)}
                      onClick={() => void bookSlot(slot)}
                    >
                      {bookingSlot === slot.startsAtUtc
                        ? 'Rezerwowanie...'
                        : `${slot.localStart.slice(11, 16)}–${slot.localEnd.slice(11, 16)}`}
                    </button>
                  ))}
                </div>
              )}
            </div>
          </>
        )}
      </div>

      <details className="card">
        <summary>Rejestracja nowej kliniki</summary>
        <p>Proponowana strefa pochodzi z ustawień urządzenia. Przed zapisem potwierdź ją dla lokalizacji kliniki.</p>
        <form className="form" onSubmit={(event) => void createClinic(event)}>
          <div className="field">
            <label className="field__label" htmlFor="new-clinic-name">Nazwa</label>
            <input id="new-clinic-name" name="name" required maxLength={256} />
          </div>
          <div className="grid grid--2">
            <div className="field">
              <label className="field__label" htmlFor="clinic-street">Ulica</label>
              <input id="clinic-street" name="street" required maxLength={150} />
            </div>
            <div className="field">
              <label className="field__label" htmlFor="clinic-building">Numer budynku</label>
              <input id="clinic-building" name="buildingNumber" required maxLength={20} />
            </div>
            <div className="field">
              <label className="field__label" htmlFor="clinic-apartment">Numer lokalu (opcjonalnie)</label>
              <input id="clinic-apartment" name="apartmentNumber" maxLength={20} />
            </div>
            <div className="field">
              <label className="field__label" htmlFor="clinic-city">Miasto</label>
              <input id="clinic-city" name="city" required maxLength={100} />
            </div>
            <div className="field">
              <label className="field__label" htmlFor="clinic-postal-code">Kod pocztowy</label>
              <input id="clinic-postal-code" name="postalCode" required maxLength={20} />
            </div>
            <div className="field">
              <label className="field__label" htmlFor="clinic-country">Kraj</label>
              <input id="clinic-country" name="country" required maxLength={100} defaultValue="Polska" />
            </div>
          </div>
          <div className="grid grid--2">
            <div className="field">
              <label className="field__label" htmlFor="clinic-open-time">Otwarcie</label>
              <input id="clinic-open-time" name="openTime" type="time" required defaultValue="08:00" />
            </div>
            <div className="field">
              <label className="field__label" htmlFor="clinic-close-time">Zamknięcie</label>
              <input id="clinic-close-time" name="closeTime" type="time" required defaultValue="17:00" />
            </div>
          </div>
          <div className="field">
            <label className="field__label" htmlFor="clinic-time-zone">Strefa czasowa kliniki (IANA)</label>
            <input
              id="clinic-time-zone"
              name="timeZoneId"
              required
              maxLength={100}
              defaultValue={Intl.DateTimeFormat().resolvedOptions().timeZone || 'Europe/Warsaw'}
            />
          </div>
          <button className="button" type="submit" disabled={savingClinic}>
            {savingClinic ? 'Zapisywanie...' : 'Utwórz klinikę'}
          </button>
        </form>
      </details>
    </section>
  );
}
