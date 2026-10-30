import { useCallback, useEffect, useMemo, useState } from 'react';
import { clinicApi, HttpError } from '../services/api';
import { useAuth } from '../hooks/useAuth';
import { ClinicRole } from '../types';
import type {
  ClinicDoctorDto,
  ClinicDto,
  DoctorProfileDto,
  DoctorWeeklyAvailabilityDto,
  SetDoctorWeeklyAvailabilityRequest,
} from '../types';

const dayLabels = [
  'Niedziela',
  'Poniedziałek',
  'Wtorek',
  'Środa',
  'Czwartek',
  'Piątek',
  'Sobota',
];

function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : 'Nie udało się wykonać operacji.';
}

export default function ClinicSchedule() {
  const { user } = useAuth();
  const [clinics, setClinics] = useState<ClinicDto[]>([]);
  const [doctors, setDoctors] = useState<ClinicDoctorDto[]>([]);
  const [availability, setAvailability] = useState<DoctorWeeklyAvailabilityDto[]>([]);
  const [doctorProfile, setDoctorProfile] = useState<DoctorProfileDto | null>(null);
  const [clinicId, setClinicId] = useState('');
  const [doctorUserId, setDoctorUserId] = useState('');
  const [dayOfWeek, setDayOfWeek] = useState(1);
  const [startTime, setStartTime] = useState('09:00');
  const [endTime, setEndTime] = useState('17:00');
  const [slotDurationMinutes, setSlotDurationMinutes] = useState(30);
  const [memberEmail, setMemberEmail] = useState('');
  const [memberRole, setMemberRole] = useState(ClinicRole.Doctor);
  const [editingId, setEditingId] = useState('');
  const [loadingClinics, setLoadingClinics] = useState(true);
  const [loadingProfile, setLoadingProfile] = useState(true);
  const [loadingDoctors, setLoadingDoctors] = useState(false);
  const [loadingAvailability, setLoadingAvailability] = useState(false);
  const [saving, setSaving] = useState(false);
  const [addingMember, setAddingMember] = useState(false);
  const [savingProfile, setSavingProfile] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const manageableClinics = useMemo(
    () => clinics.filter((clinic) => [2, 4, 5].includes(clinic.membershipRole ?? 0)),
    [clinics],
  );
  const selectedClinic = clinics.find((clinic) => clinic.id === clinicId);
  const mayManageOtherDoctors = selectedClinic?.membershipRole === 4 || selectedClinic?.membershipRole === 5;
  const selectableDoctors = mayManageOtherDoctors
    ? doctors
    : doctors.filter((doctor) => doctor.userId === user?.id);

  const loadClinics = useCallback(async () => {
    setLoadingClinics(true);
    try {
      const response = await clinicApi.getMyClinics();
      if (!response.data) {
        throw new Error('API nie zwróciło listy klinik.');
      }

      const clinicItems = response.data;
      setClinics(clinicItems);
      setClinicId((current) => {
        if (current && clinicItems.some((clinic) => clinic.id === current)) {
          return current;
        }

        return clinicItems.find((clinic) => [2, 4, 5].includes(clinic.membershipRole ?? 0))?.id ?? '';
      });
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoadingClinics(false);
    }
  }, []);

  useEffect(() => {
    void loadClinics();
  }, [loadClinics]);

  const loadDoctorProfile = useCallback(async () => {
    if (!user?.id) {
      setLoadingProfile(false);
      return;
    }

    setLoadingProfile(true);
    try {
      const response = await clinicApi.getDoctorProfile(user.id);
      if (!response.data) {
        throw new Error('API nie zwróciło profilu lekarza.');
      }

      setDoctorProfile(response.data);
    } catch (loadError) {
      if (loadError instanceof HttpError && loadError.status === 404) {
        setDoctorProfile(null);
      } else {
        setError(getErrorMessage(loadError));
      }
    } finally {
      setLoadingProfile(false);
    }
  }, [user?.id]);

  useEffect(() => {
    void loadDoctorProfile();
  }, [loadDoctorProfile]);

  const loadDoctors = useCallback(async () => {
    if (!clinicId) {
      setDoctors([]);
      setDoctorUserId('');
      setAvailability([]);
      return;
    }

    setLoadingDoctors(true);
    setError('');
    try {
      const response = await clinicApi.getDoctors(clinicId);
      if (!response.data) {
        throw new Error('API nie zwróciło listy lekarzy.');
      }

      const clinicDoctors = response.data;
      setDoctors(clinicDoctors);
      setDoctorUserId((current) => {
        if (current && clinicDoctors.some((doctor) => doctor.userId === current)) {
          return current;
        }

        const currentDoctor = clinicDoctors.find((doctor) => doctor.userId === user?.id);
        return currentDoctor?.userId ?? clinicDoctors[0]?.userId ?? '';
      });
    } catch (loadError) {
      setError(getErrorMessage(loadError));
      setDoctors([]);
      setDoctorUserId('');
    } finally {
      setLoadingDoctors(false);
    }
  }, [clinicId, user?.id]);

  useEffect(() => {
    void loadDoctors();
  }, [loadDoctors]);

  const loadAvailability = useCallback(async () => {
    if (!clinicId || !doctorUserId) {
      setAvailability([]);
      return;
    }

    setLoadingAvailability(true);
    try {
      const response = await clinicApi.getWeeklyAvailability(clinicId, doctorUserId);
      if (!response.data) {
        throw new Error('API nie zwróciło grafiku lekarza.');
      }

      setAvailability(response.data);
    } catch (loadError) {
      setAvailability([]);
      setError(getErrorMessage(loadError));
    } finally {
      setLoadingAvailability(false);
    }
  }, [clinicId, doctorUserId]);

  useEffect(() => {
    void loadAvailability();
  }, [loadAvailability]);

  const resetForm = () => {
    setEditingId('');
    setDayOfWeek(1);
    setStartTime('09:00');
    setEndTime('17:00');
    setSlotDurationMinutes(30);
  };

  const submitAvailability = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!clinicId || !doctorUserId) {
      return;
    }

    const request: SetDoctorWeeklyAvailabilityRequest = {
      dayOfWeek,
      startTime: `${startTime}:00`,
      endTime: `${endTime}:00`,
      slotDurationMinutes,
    };

    setSaving(true);
    setError('');
    setNotice('');
    try {
      if (editingId) {
        await clinicApi.updateWeeklyAvailability(clinicId, doctorUserId, editingId, request);
        setNotice('Zakres grafiku został zaktualizowany.');
      } else {
        await clinicApi.createWeeklyAvailability(clinicId, doctorUserId, request);
        setNotice('Zakres grafiku został dodany.');
      }

      resetForm();
      await loadAvailability();
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const addClinicMember = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!clinicId || !memberEmail.trim()) {
      return;
    }

    setAddingMember(true);
    setError('');
    setNotice('');
    try {
      const response = await clinicApi.addClinicMember(clinicId, {
        email: memberEmail.trim(),
        role: memberRole,
      });
      if (!response.data) {
        throw new Error('API nie zwróciło dodanego członkostwa.');
      }

      setMemberEmail('');
      setNotice('Konto zostało dodane do kliniki. Lekarz musi mieć aktywny profil, aby pojawić się na liście.');
      await loadDoctors();
    } catch (addError) {
      setError(getErrorMessage(addError));
    } finally {
      setAddingMember(false);
    }
  };

  const createDoctorProfile = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!user?.id) {
      return;
    }

    const formData = new FormData(event.currentTarget);
    const firstName = String(formData.get('firstName') ?? '').trim();
    const lastName = String(formData.get('lastName') ?? '').trim();
    const bio = String(formData.get('bio') ?? '').trim();
    const phoneNumber = String(formData.get('phoneNumber') ?? '').trim();

    setSavingProfile(true);
    setError('');
    setNotice('');
    try {
      const response = await clinicApi.createDoctorProfile(user.id, {
        firstName,
        lastName,
        bio: bio || undefined,
        phoneNumber: phoneNumber || undefined,
      });
      if (!response.data) {
        throw new Error('API nie zwróciło utworzonego profilu lekarza.');
      }

      setDoctorProfile(response.data);
      setNotice('Profil lekarza został utworzony. Administrator kliniki może teraz dodać to konto do zespołu.');
    } catch (profileError) {
      setError(getErrorMessage(profileError));
    } finally {
      setSavingProfile(false);
    }
  };

  const editAvailability = (item: DoctorWeeklyAvailabilityDto) => {
    setEditingId(item.id);
    setDayOfWeek(item.dayOfWeek);
    setStartTime(item.startTime.slice(0, 5));
    setEndTime(item.endTime.slice(0, 5));
    setSlotDurationMinutes(item.slotDurationMinutes);
  };

  const deleteAvailability = async (availabilityId: string) => {
    if (!clinicId || !doctorUserId) {
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');
    try {
      await clinicApi.deleteWeeklyAvailability(clinicId, doctorUserId, availabilityId);
      setNotice('Zakres grafiku został usunięty.');
      if (editingId === availabilityId) {
        resetForm();
      }
      await loadAvailability();
    } catch (deleteError) {
      setError(getErrorMessage(deleteError));
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="page-shell">
      <p className="eyebrow">ClinicBook</p>
      <h1>Grafik lekarzy</h1>
      <p>Godziny grafiku są lokalne dla kliniki. System pomija terminy niejednoznaczne podczas zmiany czasu.</p>
      {error ? <div className="page-state" role="alert">{error}</div> : null}
      {notice ? <div className="page-state" role="status">{notice}</div> : null}

      <details className="card">
        <summary>Profil lekarza</summary>
        {loadingProfile ? <p>Sprawdzanie profilu...</p> : doctorProfile ? (
          <div>
            <h2>{doctorProfile.firstName} {doctorProfile.lastName}</h2>
            <p>{doctorProfile.bio || 'Brak opisu profilu.'}</p>
            <p>Profil aktywny: {doctorProfile.isActive ? 'tak' : 'nie'}</p>
            <p>Dodaj konto do kliniki jako lekarza, aby zarządzać grafikiem i przyjmować rezerwacje.</p>
          </div>
        ) : (
          <form className="form" onSubmit={(event) => void createDoctorProfile(event)}>
            <p>Profil jest tworzony przez lekarza na jego koncie i może być używany w wielu klinikach.</p>
            <div className="grid grid--2">
              <div className="field">
                <label className="field__label" htmlFor="doctor-first-name">Imię</label>
                <input id="doctor-first-name" name="firstName" required maxLength={100} />
              </div>
              <div className="field">
                <label className="field__label" htmlFor="doctor-last-name">Nazwisko</label>
                <input id="doctor-last-name" name="lastName" required maxLength={100} />
              </div>
            </div>
            <div className="field">
              <label className="field__label" htmlFor="doctor-bio">Opis (opcjonalnie)</label>
              <textarea id="doctor-bio" name="bio" maxLength={1000} rows={3} />
            </div>
            <div className="field">
              <label className="field__label" htmlFor="doctor-phone">Telefon (opcjonalnie)</label>
              <input id="doctor-phone" name="phoneNumber" maxLength={20} />
            </div>
            <button className="button" type="submit" disabled={savingProfile}>
              {savingProfile ? 'Zapisywanie...' : 'Utwórz profil lekarza'}
            </button>
          </form>
        )}
      </details>

      {loadingClinics ? <p>Ładowanie klinik...</p> : manageableClinics.length === 0 ? (
        <div className="page-state">
          <p>Nie masz kliniki z uprawnieniami do zarządzania grafikiem. Lekarz musi mieć aktywny profil i członkostwo w klinice.</p>
        </div>
      ) : (
        <>
          <div className="grid grid--2">
            <div className="field">
              <label className="field__label" htmlFor="schedule-clinic-select">Klinika</label>
              <select id="schedule-clinic-select" value={clinicId} onChange={(event) => setClinicId(event.target.value)}>
                {manageableClinics.map((clinic) => (
                  <option key={clinic.id} value={clinic.id}>{clinic.name}</option>
                ))}
              </select>
            </div>
            <div className="field">
              <label className="field__label" htmlFor="schedule-doctor-select">Lekarz</label>
              <select
                id="schedule-doctor-select"
                value={doctorUserId}
                onChange={(event) => setDoctorUserId(event.target.value)}
                disabled={loadingDoctors || selectableDoctors.length === 0}
              >
                {selectableDoctors.length === 0 ? <option value="">Brak aktywnych lekarzy</option> : null}
                {selectableDoctors.map((doctor) => (
                  <option key={doctor.userId} value={doctor.userId}>
                    {doctor.firstName} {doctor.lastName}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {selectedClinic ? <p>Strefa kliniki: {selectedClinic.timeZoneId}</p> : null}

          {selectedClinic?.membershipRole === ClinicRole.Owner
            || selectedClinic?.membershipRole === ClinicRole.ClinicAdministrator ? (
            <div className="card">
              <h2>Dodaj istniejące konto</h2>
              <p>Konto musi być wcześniej zarejestrowane. Lekarz tworzy swój profil na własnym koncie.</p>
              <form className="form" onSubmit={(event) => void addClinicMember(event)}>
                <div className="grid grid--2">
                  <div className="field">
                    <label className="field__label" htmlFor="clinic-member-email">E-mail konta</label>
                    <input
                      id="clinic-member-email"
                      type="email"
                      required
                      maxLength={256}
                      value={memberEmail}
                      onChange={(event) => setMemberEmail(event.target.value)}
                    />
                  </div>
                  <div className="field">
                    <label className="field__label" htmlFor="clinic-member-role">Rola w klinice</label>
                    <select
                      id="clinic-member-role"
                      value={memberRole}
                      onChange={(event) => setMemberRole(Number(event.target.value) as ClinicRole)}
                    >
                      <option value={ClinicRole.Doctor}>Lekarz</option>
                      <option value={ClinicRole.Patient}>Pacjent</option>
                      <option value={ClinicRole.Receptionist}>Recepcja</option>
                      {selectedClinic.membershipRole === ClinicRole.Owner
                        ? <option value={ClinicRole.ClinicAdministrator}>Administrator kliniki</option>
                        : null}
                    </select>
                  </div>
                </div>
                <button className="button" type="submit" disabled={addingMember}>
                  {addingMember ? 'Dodawanie...' : 'Dodaj do kliniki'}
                </button>
              </form>
            </div>
          ) : null}

          {doctorUserId ? (
            <div className="grid grid--2">
              <div className="card">
                <h2>{editingId ? 'Edytuj grafik tygodniowy' : 'Dodaj grafik tygodniowy'}</h2>
                <form className="form" onSubmit={(event) => void submitAvailability(event)}>
                  <div className="field">
                    <label className="field__label" htmlFor="schedule-day">Dzień tygodnia</label>
                    <select id="schedule-day" value={dayOfWeek} onChange={(event) => setDayOfWeek(Number(event.target.value))}>
                      {dayLabels.map((label, index) => <option key={label} value={index}>{label}</option>)}
                    </select>
                  </div>
                  <div className="grid grid--2">
                    <div className="field">
                      <label className="field__label" htmlFor="schedule-start">Od</label>
                      <input id="schedule-start" type="time" required value={startTime} onChange={(event) => setStartTime(event.target.value)} />
                    </div>
                    <div className="field">
                      <label className="field__label" htmlFor="schedule-end">Do</label>
                      <input id="schedule-end" type="time" required value={endTime} onChange={(event) => setEndTime(event.target.value)} />
                    </div>
                  </div>
                  <div className="field">
                    <label className="field__label" htmlFor="schedule-slot-duration">Długość wizyty (min)</label>
                    <select
                      id="schedule-slot-duration"
                      value={slotDurationMinutes}
                      onChange={(event) => setSlotDurationMinutes(Number(event.target.value))}
                    >
                      {[15, 30, 45, 60, 90, 120].map((duration) => (
                        <option key={duration} value={duration}>{duration}</option>
                      ))}
                    </select>
                  </div>
                  <div className="hero__actions">
                    <button className="button" type="submit" disabled={saving}>
                      {saving ? 'Zapisywanie...' : editingId ? 'Zapisz zmiany' : 'Dodaj zakres'}
                    </button>
                    {editingId ? (
                      <button className="button button--ghost" type="button" onClick={resetForm}>
                        Anuluj edycję
                      </button>
                    ) : null}
                  </div>
                </form>
              </div>

              <div className="stack">
                <h2>Powtarzalne zakresy</h2>
                {loadingAvailability ? <p>Ładowanie grafiku...</p> : availability.length === 0 ? (
                  <p>Brak skonfigurowanych zakresów.</p>
                ) : availability.map((item) => (
                  <article className="card" key={item.id}>
                    <h3>{dayLabels[item.dayOfWeek]}</h3>
                    <p>{item.startTime.slice(0, 5)}–{item.endTime.slice(0, 5)} · slot {item.slotDurationMinutes} min</p>
                    <div className="hero__actions">
                      <button className="button button--ghost" type="button" onClick={() => editAvailability(item)}>
                        Edytuj
                      </button>
                      <button className="button button--danger" type="button" disabled={saving} onClick={() => void deleteAvailability(item.id)}>
                        Usuń
                      </button>
                    </div>
                  </article>
                ))}
              </div>
            </div>
          ) : null}
        </>
      )}
    </section>
  );
}
