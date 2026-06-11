# Poker Arena (Web Project)

Proiect tip microservices pentru un joc de poker, cu backend-uri în **ASP.NET Core (.NET / C#)** și UI/frontend de test.

Repository-ul conține mai multe servicii (ex: `GameService`, `LobbyService`, `UserService`) + pagini UI/demo (HTML/CSS/JS).

---

## Tech stack

- **.NET (ASP.NET Core Minimal API / Web)** (`net10.0`)
- **SignalR** (real-time) – în `GameService` (`/gamehub`)
- **JWT Bearer Authentication** – în `GameService` (hub-ul este `[Authorize]`)
- **EF Core + SQLite** – în `UserService` (migrations la startup)
- **RabbitMQ** – messaging/event-driven (ex: eveniment `auth.user_registered`)
- **OpenAPI + Scalar** – documentație API în Development

---

## Repository structure (high-level)

- `GameService/`  
  Backend real-time pentru joc (SignalR), autentificare JWT, OpenAPI/Scalar, logică de joc (Texas Hold’em + Five Card Draw).

- `LobbyService/`  
  Gestionare lobby-uri (create/delete). Unele metode sunt încă neimplementate (`NotImplementedException`).

- `UserService/`  
  Minimal API pentru useri (SQLite + EF Core), plus consumer RabbitMQ pentru evenimentul de înregistrare.

- `Frontend/` / `Utils/`  
  UI demo (pagini HTML/CSS/JS + scripturi de test).

- `web-project.sln`  
  Soluția .NET.

---

## Requirements

- **.NET 10 SDK** (proiectele folosesc `net10.0`)
- (Opțional) **Node.js + npm** dacă rulezi partea de `Frontend/`
- (Opțional) **RabbitMQ** dacă vrei flow complet cu events între servicii

Verificare:
```bash
dotnet --version
dotnet --list-sdks
```

---

## Run services (local)

> Rulează fiecare serviciu din folderul lui (unde există `.csproj`).

### 1) GameService
```bash
cd GameService
dotnet restore
dotnet watch run
```

#### Ce face
- Expune un **SignalR Hub** la:
  - `/gamehub`
- Expune un root endpoint:
  - `GET /` → `Poker GameService Backend is running and ready for connections!`
- Are CORS permisiv (pentru testare locală).
- În Development mapează OpenAPI + Scalar:
  - `app.MapOpenApi()`
  - `app.MapScalarApiReference()`

#### JWT / Auth (important)
`GameHub` este protejat cu `[Authorize]`. `GameService` folosește JWT config din `appsettings*.json`:
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Secret`

Pentru SignalR, token-ul poate fi trimis și ca query string:
- `access_token=...` (pe `/gamehub`), conform `OnMessageReceived`.

---

### 2) UserService
```bash
cd UserService
dotnet restore
dotnet watch run
```

#### DB (SQLite + EF Core)
`UserService` folosește SQLite:
- `UseSqlite(config.GetConnectionString("DefaultConnection"))`

La startup rulează migrations automat:
- `db.Database.Migrate();`

#### API routes
Base path: `/api/users`

- `GET /api/users/{id:guid}`  
  Returnează user-ul după id (sau `404`).

- `PUT /api/users/{id:guid}`  
  Update user (ex: `username`), cu validare FluentValidation:
  - dacă `username` este trimis, trebuie să aibă **min 3** și **max 14** caractere.

- `DELETE /api/users/{id:guid}`  
  Șterge user-ul (sau `404` dacă nu există).

#### Messaging (RabbitMQ)
`UserService` consumă evenimentul `UserRegisteredEvent` din RabbitMQ:

- Exchange: `auth`
- Routing key: `auth.user_registered`
- Queue: `user-service.user-registered`

La primirea evenimentului:
- creează user în DB prin `IUserService.CreateUserAsync(userId, username)`.

---

### 3) LobbyService
`LobbyService` gestionează lobby-uri (create/delete) și conține modele:
- `Lobby` (max 6 jucători, min 2)
- `Player` (ready status etc.)

Unele metode sunt încă neimplementate:
- `AddPlayerToLobbyAsync`
- `RemovePlayerFromLobbyAsync`

Pornirea și endpoint-urile depind de setup-ul complet din proiect (fișierele de Program/Endpoints nu sunt incluse aici).

---

## GameService - Game Logic (overview)

### State
- `Table`:
  - `RoomId`, `Pot`, `Phase`, `BoardCards`, `Players`, `CurrentDeck`, `DealerIndex`, `GameType` etc.
- `Player`:
  - `Id`, `Name`, `Chips`, `CurrentBet`, `HasFolded`, `IsAllIn`, `HoleCards`
  - `ResetForNewHand()` resetează starea pentru o nouă mână.

### Deck & Cards
- `Deck` generează un pachet standard 52 cărți și face shuffle.
- `Card` este un record: `Card(Suit Suit, Rank Rank)`

### Game types
- `TexasHoldemStateMachine`
  - Deal 2 cărți / player
  - Phase flow: `PreFlop -> Flop -> Turn -> River -> Showdown`
  - La showdown folosește `IHandEvaluator` pentru a determina câștigătorul și împarte pot-ul (inclusiv caz de egalitate).

- `FiveCardDrawStateMachine`
  - Deal 5 cărți / player
  - Phase flow: `InitialDeal -> DrawCards -> SecondBetting -> Showdown`

### Hand evaluation
- `HandEvaluator` folosește `HoldemPoker.Evaluator` pentru:
  - ranking score
  - category
  - description

---

## UI / Demo pages

Există pagini HTML/CSS demo pentru UI:
- `home.html`, `login.html`, `lobbies.html`, `profile.html`, `game.html`
- `styles.css` – stilizare
- `app.js` – mock pentru lobby list + search

### API test script
Exemplu JS (`frontend.js`) care face request la:
- `API_URL = "https://localhost:5000/api"`
- `GET /players` (și loghează jucătorii în consola browser-ului)

> Notă: endpoint-ul și portul trebuie să corespundă cu backend-ul pornit local.

## License
TBD
