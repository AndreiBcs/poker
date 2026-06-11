# Poker Arena — Test Plan (Manual, MVP)

## 1. Scope
Acest plan acoperă testare manuală pentru:
- UserService API
- GameService SignalR hub `/gamehub`
- Integrare event-driven prin RabbitMQ pentru creare user

## 2. Test Environment / Preconditions
- Proiectul rulează local.
- .NET 10 SDK este instalat.
- Serviciile sunt pornite în terminale separate:
  - `UserService` prin `dotnet watch run`
  - `GameService` prin `dotnet watch run`
- RabbitMQ este pornit local, dacă se testează integrarea.
- Configurația din `appsettings*.json` este validă:
  - `DefaultConnection` pentru SQLite
  - `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Secret`

## 3. Test Cases

### TC-01 — UserService starts successfully
**Steps:**
1. Pornește UserService.
2. Observă log-urile.

**Expected Result:**
- Serviciul pornește fără erori.

### TC-AUTH-01 — Sign-up valid => 201 Created
**Steps:**
1. Trimite `POST /api/auth/sign-up` cu date valide.
2. Observă răspunsul.

**Expected Result:**
- `201 Created`
- răspunsul conține date user și token.

### TC-AUTH-02 — Sign-up invalid => 400 Bad Request
**Steps:**
1. Trimite `POST /api/auth/sign-up` cu date invalide.
2. Observă răspunsul.

**Expected Result:**
- `400 Bad Request`

### TC-AUTH-03 — Sign-up conflict => 409 Conflict
**Precondition:** există deja un user cu același username/email.  
**Steps:**
1. Trimite `POST /api/auth/sign-up` cu username/email deja existente.
2. Observă răspunsul.

**Expected Result:**
- `409 Conflict`

### TC-AUTH-04 — Login valid => 200 OK + token
**Precondition:** există user creat.  
**Steps:**
1. Trimite `POST /api/auth/login` cu credentiale corecte.
2. Observă răspunsul.

**Expected Result:**
- `200 OK`
- răspunsul conține token JWT.

### TC-AUTH-05 — Login failed => 401 Unauthorized
**Steps:**
1. Trimite `POST /api/auth/login` cu credentiale greșite.
2. Observă răspunsul.

**Expected Result:**
- `401 Unauthorized`

### TC-AUTH-06 — Use token to connect to GameHub
**Precondition:** token JWT obținut din login.  
**Steps:**
1. Conectează-te la `/gamehub` cu token-ul.
2. Observă handshake-ul.

**Expected Result:**
- conexiune acceptată.

### TC-02 — GET user existing
**Precondition:** există user valid în DB.  
**Steps:**
1. Apelează `GET /api/users/{id}`.

**Expected Result:**
- `200 OK` cu `id` și `username`.

### TC-03 — GET user not found
**Steps:**
1. Apelează `GET /api/users/{randomGuid}`.

**Expected Result:**
- `404 NotFound`

### TC-04 — PUT update username valid
**Precondition:** există user cu `id`.  
**Steps:**
1. Apelează `PUT /api/users/{id}` cu `{ "username": "Alex123" }`.

**Expected Result:**
- `200 OK`
- username actualizat.

### TC-05 — PUT update username too short
**Precondition:** există user cu `id`.  
**Steps:**
1. Apelează `PUT /api/users/{id}` cu username de 2 caractere.

**Expected Result:**
- `400 ValidationProblem`

### TC-06 — PUT update username too long
**Precondition:** există user cu `id`.  
**Steps:**
1. Apelează `PUT /api/users/{id}` cu username de 15+ caractere.

**Expected Result:**
- `400 ValidationProblem`

### TC-07 — DELETE user existing
**Precondition:** există user cu `id`.  
**Steps:**
1. Apelează `DELETE /api/users/{id}`.

**Expected Result:**
- `204 NoContent`

### TC-08 — DELETE user not found
**Steps:**
1. Apelează `DELETE /api/users/{randomGuid}`.

**Expected Result:**
- `404 NotFound`

### TC-09 — GameService root endpoint
**Steps:**
1. Pornește GameService.
2. Deschide `GET /`.

**Expected Result:**
- răspuns text de tip „Poker GameService Backend is running and ready for connections!”

### TC-10 — SignalR connect without token
**Steps:**
1. Încearcă conectarea la `/gamehub` fără JWT.

**Expected Result:**
- conexiune refuzată cu `401 Unauthorized`

### TC-11 — SignalR connect with valid token
**Precondition:** există un JWT valid.  
**Steps:**
1. Conectează-te la `/gamehub` cu token valid.

**Expected Result:**
- conexiune acceptată.

### TC-12 — StateUpdated received after action
**Precondition:** jucători conectați și room valid.  
**Steps:**
1. Apelează o acțiune de joc, de exemplu `PlaceBet` sau `DiscardCards`.
2. Monitorizează mesajele primite de client.

**Expected Result:**
- clientul primește `StateUpdated` cu `TableStateDto`.

### TC-13 — RabbitMQ event creates user
**Precondition:** RabbitMQ rulează și consumer-ul este activ.  
**Steps:**
1. Publică un event `auth.user_registered` cu `UserId` și `Username`.
2. Apelează `GET /api/users/{UserId}`.

**Expected Result:**
- `200 OK` cu user creat.

### TC-14 — UI demo pages open correctly
**Steps:**
1. Deschide paginile demo: `home`, `login`, `lobbies`, `profile`, `game`.

**Expected Result:**
- paginile se afișează corect, fără erori majore de layout.

## 4. Exit Criteria
Pentru demo, trebuie trecute cel puțin:
- TC-01
- TC-02 / TC-03
- TC-04 / TC-05
- TC-09
- TC-10 / TC-11

Testul de integrare RabbitMQ este recomandat, dar nu blochează demo-ul dacă nu este disponibil.
