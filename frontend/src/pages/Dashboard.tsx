import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export default function Dashboard() {
  const { user } = useAuth();

  return (
    <section className="page-shell">
      <p className="eyebrow">ClinicBook</p>
      <h1>Witaj, {user?.displayName ?? 'użytkowniku'}</h1>
      <p>Wybierz działanie, aby zarządzać swoimi wizytami lub grafikiem kliniki.</p>
      <div className="grid grid--2">
        <article className="card">
          <h2>Pacjent</h2>
          <p>Znajdź klinikę, zarezerwuj termin albo sprawdź i anuluj wizytę.</p>
          <div className="hero__actions">
            <Link className="button" to="/clinics">Znajdź termin</Link>
            <Link className="button button--ghost" to="/appointments">Moje wizyty</Link>
          </div>
        </article>
        <article className="card">
          <h2>Personel kliniki</h2>
          <p>Skonfiguruj powtarzalny grafik lekarza lub przejdź do obsługi wizyt.</p>
          <div className="hero__actions">
            <Link className="button" to="/clinic-schedule">Grafik lekarzy</Link>
            <Link className="button button--ghost" to="/appointments">Wizyty kliniki</Link>
          </div>
        </article>
      </div>
    </section>
  );
}
