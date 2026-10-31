# ClinicBook

Aplikacja full-stack do zarządzania kliniką i rezerwowania wizyt. Backend ASP.NET Core 9 udostępnia API z uwierzytelnianiem JWT, a frontend React + TypeScript obsługuje pacjentów i personel kliniki. PostgreSQL przechowuje grafik tygodniowy oraz wizyty jako terminy UTC wyświetlane w lokalnej strefie czasowej kliniki.

## Najważniejsze funkcje

- profile klinik, lekarzy i członkostw z rolami personelu;
- tygodniowa dostępność lekarzy oraz wyszukiwanie i rezerwowanie terminów;
- przegląd i anulowanie wizyt przez pacjenta oraz obsługa wizyt przez personel;
- strefa IANA wybierana dla kliniki, z propozycją na podstawie strefy urządzenia;
- podział backendu na warstwy `API`, `Application`, `Domain`, `Infrastructure` i `Shared`;
- JWT access token oraz rotacja refresh tokenów w ciasteczkach HttpOnly;
- potwierdzanie adresu e-mail i e-mailowe 2FA;
- autoryzacja oparta na rolach dla operacji administracyjnych, projektów i zadań;
- walidacja formularzy przez React Hook Form i Zod;
- centralna obsługa wyjątków, Serilog i health check API;
- PostgreSQL, Mailpit i reverse proxy w lokalnym środowisku Docker Compose;
- testy jednostkowe, integracyjne, frontendowe oraz Docker smoke/E2E;
- GitHub Actions dla walidacji kodu i publikowania obrazów do GHCR.

## Stack technologiczny

### Backend

- .NET 9 i ASP.NET Core Web API;
- Entity Framework Core i PostgreSQL;
- FluentValidation;
- Serilog;
- xUnit i Moq.

### Frontend

- React 19;
- TypeScript;
- React Router 6;
- React Hook Form;
- Zod;
- Testing Library, Vitest i Vite.

Aktywny manifest aplikacji frontendowej znajduje się w [frontend/package.json](frontend/package.json). Rootowe manifesty Node nie są wymagane do budowania aplikacji.

## Struktura projektu

```text
.
├── backend/
│   ├── API/                  # kontrolery, middleware i konfiguracja startowa
│   ├── Application/          # DTO, serwisy, walidatory i interfejsy
│   ├── Domain/               # encje, enumy, value objects i kontrakty domenowe
│   ├── Infrastructure/       # DbContext, migracje, repozytoria i usługi
│   ├── Shared/               # wspólne odpowiedzi, ustawienia i helpery
│   ├── UnitTests/            # testy jednostkowe
│   ├── IntegrationTests/     # testy integracyjne API
│   └── E2ETests/             # smoke testy uruchomionego środowiska
├── frontend/                 # aplikacja React i aktywny package.json
├── docker/                   # konfiguracja nginx i dokumentacja Compose
├── doc/                      # dokumentacja architektury i uruchamiania
├── docker-compose.yml        # główny lokalny stack kontenerów
└── .github/workflows/        # workflowy CI i publikacji obrazów
```

Szczegółowy zakres i ograniczenia V1 znajdują się w [doc/CLINICBOOK_V1.md](doc/CLINICBOOK_V1.md).

## Szybki start

### Wymagania

- .NET 9 SDK;
- Node.js 20 lub nowszy;
- Docker Desktop z obsługą Compose.

### Docker Compose

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Lokalne adresy:

- frontend: http://localhost:3000;
- API: http://localhost:5000;
- health check: http://localhost:5000/health;
- Swagger: http://localhost:5000/swagger;
- Mailpit: http://localhost:8025.

Compose uruchamia PostgreSQL, Mailpit, backend i frontend. Wartości domyślne są przeznaczone do lokalnego developmentu. Sekrety środowiska docelowego należy dostarczać przez konfigurację hostingu lub bezpieczny magazyn sekretów.

### Uruchomienie lokalne bez Dockera

Backend:

```powershell
Set-Location backend\API
dotnet run
```

Frontend:

```powershell
Set-Location frontend
npm install
npm start
```

Przy lokalnym uruchomieniu frontendu ustaw `VITE_API_URL=http://localhost:5000` w `frontend/.env.development.local`.

## Uwierzytelnianie

Projekt zawiera następujący przepływ:

1. `POST /api/auth/register` tworzy konto i wysyła wiadomość potwierdzającą adres.
2. `POST /api/auth/confirm-email` aktywuje konto.
3. `POST /api/auth/login` wydaje tokeny albo rozpoczyna wyzwanie e-mailowego 2FA.
4. `POST /api/auth/verify-2fa` kończy logowanie po poprawnej weryfikacji kodu.
5. `POST /api/auth/refresh-token` rotuje refresh token i wydaje nowy access token.
6. `POST /api/auth/logout` unieważnia refresh token i usuwa ciasteczko.
7. `GET /api/auth/me` zwraca profil zalogowanego użytkownika.

Frontend przechowuje lokalnie wyłącznie access token. Refresh token pozostaje w ciasteczku HttpOnly i nie jest dostępny dla JavaScriptu. W lokalnym Compose wiadomości trafiają do Mailpit.

## Testy

Backend:

```powershell
dotnet test backend\UnitTests\UnitTests.csproj
dotnet test backend\IntegrationTests\IntegrationTests.csproj
```

Test przepływu rezerwacji:

```powershell
dotnet test backend\IntegrationTests\IntegrationTests.csproj --filter FullyQualifiedName~ClinicBookingApiIntegrationTests
```

Smoke testy E2E wymagają uruchomionego środowiska:

```powershell
.\scripts\Invoke-E2ETests.ps1
```

Frontend:

```powershell
Set-Location frontend
npm ci
npm run test:once
npm run build
```

E2E w tym repozytorium są testami smoke uruchomionego stacku. Sprawdzają podstawową dostępność API i frontendu, a nie pełne pokrycie wszystkich przepływów biznesowych.

## CI/CD

GitHub Actions obejmuje build, testy jednostkowe i integracyjne backendu, testy i build frontendu, Docker Compose smoke tests oraz publikowanie obrazów backendu i frontendu do GitHub Container Registry.

Workflow CD publikuje obrazy z tagiem SHA oraz `latest`. Nie wykonuje jeszcze wdrożenia na konkretną platformę hostingową. Konfiguracja produkcyjnych sekretów, migracji, hostingu, monitoringu i rollbacku pozostaje decyzją środowiska docelowego. Szczegóły znajdują się w [doc/CI_CD.md](doc/CI_CD.md).

## Znane ograniczenia

- grafik tygodniowy nie obsługuje wyjątków, urlopów ani świąt;
- V1 nie wysyła powiadomień o rezerwacji i nie przechowuje powodów wizyt, diagnoz ani dokumentacji medycznej;
- zmiana strefy kliniki po utworzeniu wizyty jest blokowana;
- frontend korzysta z Vite i Vitest; pozostałe podatności React Routera wymagają osobnej decyzji dotyczącej migracji do wersji 7;
- Docker Compose jest przede wszystkim lokalnym środowiskiem uruchomieniowym i testowym;
- domyślne hasła oraz sekrety Compose nie nadają się do środowiska produkcyjnego;
- publikacja obrazu do GHCR nie jest równoznaczna z deploymentem;
- pełne testy przepływów biznesowych wymagają dalszego rozszerzania testów integracyjnych i E2E.

## Dokumentacja

- [doc/CLINICBOOK_V1.md](doc/CLINICBOOK_V1.md)
- [doc/GETTING_STARTED.md](doc/GETTING_STARTED.md)
- [doc/ARCHITECTURE.md](doc/ARCHITECTURE.md)
- [doc/BACKEND_SETUP.md](doc/BACKEND_SETUP.md)
- [doc/FRONTEND_SETUP.md](doc/FRONTEND_SETUP.md)
- [doc/JWT_ARCHITECTURE.md](doc/JWT_ARCHITECTURE.md)
- [doc/EMAIL_2FA_FLOWS.md](doc/EMAIL_2FA_FLOWS.md)
- [doc/CI_CD.md](doc/CI_CD.md)
- [docker/DOCKER_COMPOSE.md](docker/DOCKER_COMPOSE.md)

## Licencja

MIT. Szczegóły znajdują się w [LICENSE](LICENSE).
