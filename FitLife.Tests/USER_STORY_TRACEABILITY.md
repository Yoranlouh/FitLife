# User Story → Test traceability (FitLife)

Elke user story uit `FitLife_User_Stories.docx` is gekoppeld aan unit- en/of UI-tests.
De koppeling is in code vastgelegd met `[Trait("UserStory", "US-…")]` en `[Trait("Rol", "…")]`,
zodat je per story kunt draaien:

```bash
dotnet test FitLife.Tests --filter "UserStory=US-L02"     # alle reserveer-tests
dotnet test FitLife.Tests --filter "Rol=Instructeur"      # alle instructeur-tests
dotnet test FitLife.UITests --filter "UserStory=US-L01"   # UI-tests rooster (vereist Appium)
```

## Rolafspraak (code)

Voor nu heeft een **Beheerder (admin) exact dezelfde functie als een Trainer (instructeur)**:
trainers beheren zelf hun lessen, workouts en leden en houden alles bij in het webportaal.
In `AuthenticationService` is daarom `IsInstructor` `true` voor zowel `"instructor"` als
`"admin"`, en zien beide rollen dezelfde staf-weergave op de homepagina.

## Matrix

| US | Story | Prioriteit | Unit-test-klasse(n) | UI-test | Aantal unit |
|----|-------|-----------|---------------------|---------|-------------|
| **US-L01** | Lesrooster bekijken | Must | `LessonCalendarTests`, `PopupLessonDisplayTests` | `WeekPageTests`, `HomePageTests` (rooster-tegel) | 33 |
| **US-L02** | Les reserveren | Must | `ReserveLessonTests`, `ReserveCreditRulesTests`, `ReserveWindowRulesTests` | `LessonDetailPageTests` | 32 |
| **US-L03** | Reservering annuleren | Must | `CancelReservationTests`, `CancelCreditRulesTests`, `CancellationDeadlineRulesTests` | — | 24 |
| **US-L04** | Op de wachtlijst plaatsen | Should | `WaitlistLogicTests`, `JoinWaitlistTests` | — | 15 |
| **US-L05** | Mijn reserveringen bekijken | Should | `MyReservationsTests` | `ProfilePageTests` (Mijn lessen-knop) | 3 |
| **US-L06** | Abonnement en credits inzien | Should | `CreditDisplayRulesTests`, `AuthenticationServiceTests` (credits) | `ProfilePageTests`, `HomePageTests` (profiel-tegel) | 10 |
| **US-L07** | Profielfoto uploaden | Could | `AuthenticationServiceTests` (UpdatePhotoUrl) | `ProfilePageTests` (Foto-wijzigen-knop) | 2 |
| **US-I01** | Eigen rooster bekijken | Must | `InstructorScheduleTests` | — | 2 |
| **US-I02** | Deelnemerslijst bekijken | Should | `ParticipantServiceTests` | — | 6 |
| **US-I03** | Presentie registreren | Could | `AttendanceServiceTests`, `CheckInWindowRulesTests` | — | 30 |
| **US-B01** | Lessen beheren | Must | `LessonCrudTests` | — | 13 |
| **US-B02** | Zalen beheren | Should | `InstructorAndLocationDropdownTests` (GetLocations) | — | 1 |
| **US-B03** | Instructeurs beheren | Should | `InstructorAndLocationDropdownTests` (GetInstructors) | — | 3 |
| **US-B04** | Leden en abonnementen beheren | Must | `AddMemberToLessonTests`, `SubscriptionCreditAllotmentTests` | — | 6 |
| **US-B05** | Notificaties versturen | Could | `NotificationIconTests` | — | 20 |

`AuthenticationServiceTests` en `TranslatorTests` dragen daarnaast de tag
`UserStory=Cross-cutting`: inloggen/sessie en meertaligheid onderbouwen álle stories.

## Acceptatiecriteria die niet als unit getest zijn (en waarom)

Sommige criteria zijn integratie-/infrastructuurgedrag en zijn bewust níet als unit getest:

- **Eén databasetransactie** (L02), **plek vrijgeven naar wachtlijst** (L03/L04),
  **automatisch doorschuiven** (L04): dit is server-/DB-gedrag in `FitLife.API` en hoort
  in een integratietest tegen de database, niet in een MAUI-unittest.
- **Asynchroon laden zonder UI-blokkade** (L01): gedekt door de UI-tests
  (`WeekPageObject.WaitForLoadingComplete`).
- **Notificatie daadwerkelijk afgeleverd / push** (L04/B05): afhankelijk van het
  notificatiekanaal; de app-zijde (weergave per type) is wel getest.
- **Te groot/ongeldig bestand geweigerd** (L07): de validatie zit in de API-upload-endpoint,
  niet in de MAUI-`PhotoService`.
- **Zaal/instructeur niet verwijderbaar bij koppeling** (B02/B03): server-side
  integriteitsregel in de API/DB.
