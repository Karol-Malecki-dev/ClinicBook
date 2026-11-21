import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export default function Home() {
  const { isAuthenticated, user } = useAuth();

  return (
    <section className="hero">
      <div className="hero__copy">
        <p className="eyebrow">ClinicBook</p>
        <h1>Umów wizytę w klinice.</h1>
        <p>
          Znajdź klinikę, wybierz lekarza i zarezerwuj dostępny termin. Grafik i godziny wizyt są dopasowane do strefy czasowej placówki.
        </p>
        <div className="hero__actions">
          {isAuthenticated ? (
            <>
              <Link className="button" to="/clinics">Znajdź termin</Link>
              <Link className="button button--ghost" to="/appointments">Moje wizyty</Link>
            </>
          ) : (
            <>
              <Link className="button" to="/login">Zaloguj się</Link>
              <Link className="button button--ghost" to="/register">Utwórz konto</Link>
            </>
          )}
        </div>
      </div>

      <div className="hero__stack">
        <aside className="hero__panel">
          <h2>Konto</h2>
          {isAuthenticated && user ? (
            <>
              <p>{user.displayName}</p>
              <p>{user.email}</p>
            </>
          ) : (
            <p>Zaloguj się lub utwórz konto, aby rezerwować i zarządzać wizytami.</p>
          )}
        </aside>
        <aside className="hero__panel">
          <h2>Godziny kliniki</h2>
          <p>Terminy są przeliczane na UTC przy zapisie i prezentowane w lokalnej strefie czasowej kliniki.</p>
        </aside>
      </div>
    </section>
  );
}
