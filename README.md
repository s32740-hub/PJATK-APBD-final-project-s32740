# System Uznawania Przychodów

Projekt APBD. Aplikacja REST API wspierająca korporację ABC w obsłudze klientów, sprzedaży oprogramowania, podpisywaniu kontraktów, obsłudze subskrypcji oraz obliczaniu przychodu.

## Pierwsze uruchomienie - konto admina

Endpoint `POST /auth/sign-up` jest zabezpieczony rolą Admin (aby nikt z zewnątrz nie mógł samodzielnie zakładać kont). Przy pierwszym uruchomieniu, gdy tabela `Employees` jest pusta, aplikacja automatycznie tworzy knto admina w `Program.cs`:

```csharp
if (!await db.Employees.AnyAsync())
{
    var hasher = new PasswordHasher<object>();
    db.Employees.Add(new Employee
    {
        Login = "admin",
        PasswordHash = hasher.HashPassword(null!, "admin123"),
        Role = "Admin"
    });
    await db.SaveChangesAsync();
}
```

Hasło jest hashowane przez `PasswordHasher` z ASP.NET Identity - w bazie nigdy nie jest przechowywane w czystej postaci.

Po zalogowaniu admin może tworzyć kolejne konta przez `POST /auth/sign-up`.

## Autoryzacja w Swagger

1. Wywołaj `POST /auth/sign-in` z loginem i hasłem
2. Skopiuj wartość `accessToken` z odpowiedzi
3. Kliknij Authorize
4. Wpisz token
5. Kliknij Authorize

Token wygasa po **60 minutach**. Odśwież przez `POST /auth/refresh` (używa HttpOnly cookie z refresh tokenem ważnym 7 dni).

## Model domenowy

### Hierarchia klientów

Klasy `IndividualClient` i `CorporateClient` dziedziczą po abstrakcyjnej klasie `Client`. EF Core przechowuje wszystkich klientów w jednej tabeli `Clients` z koluną `ClientType` (`"Individual"` / `"Corporate"`).

### Kluczowe pola modelu `Contract`

```csharp
bool IsSigned // false = oferta, true = podpisany = przychód
bool IsActive // false = anulowany (po terminie lub ręcznie)
decimal TotalPrice // cena finalna po zniżkach
decimal AnnualLicensePriceSnapshot  // snapshot ceny w momencie tworzenia
string SoftwareVersion // snapshot wersji w momencie tworzenia
```

## Endpointy API

### Autoryzacja - `/auth`

| Metoda | Ścieżka | Rola | Opis                               |
|---|---|---|------------------------------------|
| POST | `/auth/sign-in` | Publiczny | Logowanie - zwraca AccessToken JWT |
| POST | `/auth/sign-up` | Admin | Rejestracja nowego pracownika      |
| POST | `/auth/refresh` | Publiczny | Odświeżenie tokenu przez cookie    |
| POST | `/auth/sign-out` | Publiczny | Wylogowanie - usuwa refresh token  |

### Klienci - `/clients`

Wszystkie endpointy wymagają roli **Admin**.

| Metoda | Ścieżka | Opis |
|---|---|---|
| POST | `/clients/individual` | Dodj klienta indywidualnego |
| PUT | `/clients/individual/{id}` | Aktualizuj klienta indywidualnego|
| DELETE | `/clients/individual/{id}` | Miękkie usunięcie klienta |
| POST | `/clients/corporate` | Dodaj firmę |
| PUT | `/clients/corporate/{id}` | Aktualizuj firmę|

**Reguły biznesowe:**
- PESEL musi mieć dokładnie 11 znaków (`[Length(11, 11)]`)
- PESEL i KRS mają indeks unikalny - nie można dodać duplikatu
- PESEL nie może być zmieniony
- `UpdateIndividualClientDto` nie zawiera tego pola
- KRS nie może być zminiony - `UpdateCorporateClientDto` nie zawiera tego pola
- Miękkie usunięcie ustawia `IsDeleted=true` i nadpisuje dane: `FirstName="DELETED"`, `LastName="DELETED"`, `Address="DELETED"`, `Email="deleted_{id}@deleted.invalid"`, `Phone="DELETED"`
- Firma nie może być usunięta - brak endpointu DELETE dla corporate

### Kontrakty - `/contracts`

Wszystkie endpointy wymagają zalogowania

| Metoda | Ścieżka | Opis                              |
|---|---|-----------------------------------|
| GET | `/contracts/{id}` | Pobierz szczegóły kontraktu       |
| POST | `/contracts` | Utwórz kontramkt                  |
| DELETE | `/contracts/{id}` | Usuń kontrakt (tylko nieopłacony) |
| POST | `/contracts/payments` | Dodaj płatność za kontrakt        |

**Reguły biznesowe:**
- Kontrakt blokowany gdy klient ma już aktwny kontrakt lub subskrypcję na ten produkt
- Cena = `AnnualLicensePrice + additionalSupportYears × 1000 PLN`
- Stosowana najwyższa aktywna zniżka typu `Contract` (aktywna w dniu tworzenia)
- Powracający klient (ma min 1 podpisany kontrakt lub min 1 subskrypcję) = dodatkowe 5% (kumulowane z innymi zniżkami)
- Płatność po terminie (`EndDate`) = kontrakt anulowany automatycznie, błąd 400
- Nadpłata powyżej `TotalPrice` = błąd 400
- Po osiągnięciu 100% kwoty = `IsSigned=true` = kontrakt staje się przychodem
- Nie można usunąć podpisanego kontraktu
- `DurationDays` musi wynosić od **3 do 30** — walidacja przez atrybut `[Range(3, 30)]` na `CreateContractDto`, sprawdzana automatycznie przez ASP.NET Core (model validation) przed dotarciem żądania do `ContractService`; przekroczenie limitu skutkuje błędem 400
- `StartDate` = data dzisiejsza (UTC), `EndDate` = `StartDate + DurationDays` - klient nie podaje dat, tylko liczbę dni


### Subskrypcje - `/subscriptions`

Wszystkie endpointy wymagają zalogowania.

| Metoda | Ścieżka | Opis |
|---|---|---|
| POST | `/subscriptions` | Kup subskrypcję (pierwsza płatność od razu) |
| POST | `/subscriptions/renew` | Zapłać za kolejny okres odnowienia |

**Reguły biznesowe:**
- Zakup blokowany gdy klient ma już aktywną subskrypcję lub aktywny kontrakt na ten produkt
- Cena bazowa za okres = `AnnualLicensePrice / 12 * renewalPeriodMonths`
- **Pierwsza płatność:** najwyższa aktywna zniżka promocyjna + 5% lojalnościowa
- **Odnowienia:** tylko 5% zniżka lojalnościowa
- Płatność za odnowienie możliwa tylko po zakończeniu bieżącego okresu
- Grace period: 7 dni po końcu okresu - po tym czasie subskrypcja anulowana (`IsActive=false`)
- Kwota musi być dokładna - inaczej błąd 400

### Przychód - `/revenue`

Wszystkie endpointy wymagają zalogowania

| Metoda | Ścieżka | Parametry query | Opis |
|---|---|---|---|
| GET | `/revenue/current` | `softwareId?`, `currency?` | Aktualny przychód |
| GET | `/revenue/predicted` | `softwareId?`, `currency?` | Przewidywany przychód |
**Reguły biznesowe:**

Aktualny przychód:
```
= suma TotalPrice podpisanych konntraktów (IsSigned=true)
+ suma wszystkich wpłat SubscriptionPayments
```

Przewidywany przychód:
```
= aktualny przychód
+ suma brakujących kwot nieopłaconych aktywnych kontraktów
+ suma (BasePricePerPeriod × 0.95) dla każdej aktywnej subskrypcji
```

Przeliczanie walut: kurs średni z tabeli A NBP (`https://api.nbp.pl/api/exchangerates/rates/A/{currency}/`). Kwota PLN dzielona przez kurs 

## Logika biznesowa

### Obliczanie ceny kontraktu

```
basePrice = AnnualLicensePrice + (additionalSupportYears * 1000)
bestDiscount = max(aktywne zniżki typu Contract w dniu tworzenia)
returningDiscount = 5% jeśli klient ma min 1 podpisany kontrakt lub min 1 subskrypcję
totalDiscount = bestDiscount + returningDiscount
finalPrice = basePrice * (1 - totalDiscount / 100)
```

**Przykład** (FinApp Pro, 1 rok wsparcia, Summer Sale 15%, powracający klient):
```
base = 10 000 + 1 000 = 11 000 PLN
discount = 15% + 5% = 20%
final = 11 000 * 0.80 = 8 800 PLN
```

### Obliczanie ceny pierwszej subskrypcji

```
basePricePerPeriod = AnnualLicensePrice / 12 × renewalPeriodMonths
bestPromoDiscount = max(aktywne zniżki typu Subscription w dniu zakupu)
loyaltyDiscount = 5% jeśli klient jest powracający
firstPayment = basePricePerPeriod * (1 - (bestPromoDiscount + loyaltyDiscount) / 100)
```

### Miękkie usunięcie klienta

Dane osobow są anonimizoane. Rekord pozostaje w bazie ponieważ może być powiązany z historycznymi kontraktami przez klucz obcy. PESEL nie jest nadpisywany.
## Testy jednostkowe

### ClientServiceTests

| Test | Co sprawdza |
|---|---|
| `CreateIndividualClient_ValidData_CreatesSuccessfully` | Poprawne tworzenie klienta |
| `CreateIndividualClient_DuplicatePesel_ThrowsDomainException` | Blokada duplikatu PESEL |
| `CreateCorporateClient_DuplicateKrs_ThrowsDomainException` | Blokada duplikatu KRS |
| `UpdateIndividualClient_CannotChangePesel_PeselRemainsOriginal` | PESEL niezmienialny |
| `DeleteIndividualClient_SoftDelete_AnonymizesData` | Anonimizacja danych |
| `DeleteIndividualClient_AlreadyDeleted_ThrowsDomainException` | Blokada podwójnego usunięcia |
| `UpdateIndividualClient_DeletedClient_ThrowsDomainException` | Blokada edycji usuniętego |

### ContractServiceTests

| Test | Co sprawdza                           |
|---|---------------------------------------|
| `CreateContract_NoDiscounts_PriceEqualsAnnualLicense` | Cena bez zniżek |
| `CreateContract_WithAdditionalSupportYears_IncreasesPrice` | +1000 PLN za rok wsparcia|
| `CreateContract_WithActiveDiscount_AppliesDiscount` | Stosowanie zniżki|
| `CreateContract_ReturningClient_Gets5PercentExtra` | +5% dla powracającego|
| `CreateContract_MultipleDiscounts_TakesHighest` | Wybór najwyższej zniżki|
| `CreateContract_AlreadyHasActiveContract_ThrowsDomainException` | Blokada duplikatu|
| `CreateContract_DeletedClient_ThrowsDomainException` | Blokada dla usuniętego klienta|
| `AddPayment_FullPaymentInOneInstalment_SignsContract` | Pełna wpłata = podpisanie|
| `AddPayment_Instalments_SignsWhenFull` | Raty - podpisanie po osiągnięciu 100% |
| `AddPayment_OverpaymentThrowsDomainException` | Blokada nadpłaty|
| `DeleteContract_SignedContract_ThrowsDomainException` | Blokada usunięcia podpisanego|

### SubscriptionServiceTests

| Test | Co sprawdza |
|---|---|
| `CreateSubscription_NoDiscounts_PriceEqualsMonthlyRate` | Cena bez zniżek |
| `CreateSubscription_ReturningClient_Gets5PercentLoyaltyDiscount` | 5% dla powracającego |
| `CreateSubscription_WithPromoDiscount_AppliesHighestDiscount` | Stosowanie promocji |
| `CreateSubscription_DuplicateActive_ThrowsDomainException` | Blokada duplikatu |
| `PayRenewal_CorrectAmount_UpdatesPeriod` | Odnowienie przesuwa okres |
| `PayRenewal_WrongAmount_ThrowsDomainException` | Blokada złej kwoty |
| `PayRenewal_TooLate_CancelsSubscription` | Anulowanie po 7 dniach |


### Author
Hanna Krechyk - s32740
