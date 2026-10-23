# Poker Arena — Test Plan (Manual, MVP)

## 1) Scope
Acest plan acoperă testare manuală (smoke + scenarii de bază) pentru:
- UserService API
- GameService SignalR hub `/gamehub`
- Integrare event-driven (RabbitMQ) pentru creare user

## 2) Test Environment / Preconditions
- Proiectul este clonat și rulează pe aceeași mașină (local).
- .NET 10 SDK este instalat.
- Servicii pornite (în terminale separate):
  - `UserService` (`dotnet watch run`)
  - `GameService` (`dotnet watch run`)
- (Opțional) RabbitMQ pornit local (dacă se testează event integration).
- Există configurare corectă în `appsettings*.json`:
  - `DefaultConnection` pentru SQLite (UserService)
  - `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Secret` (GameService)

## 3) Test Cases

### TC-01 — UserService starts successfully + migrations
**Steps:**
1. Pornește `UserService`.
2. Observă log-urile din terminal.

**Expected Result:**
- serviciul pornește fără erori,
- migrations rulează fără excepții.


---
### TC-AUTH-01 — Sign-up valid => 201 Created
**Steps:**
1. Trimite `POST /api/auth/sign-up` cu date valide (username/email/password).
2. Observă răspunsul.

**Expected Result:**
- `201 Created`
- răspunsul conține date user și/sau token (în funcție de implementare).

---

### TC-AUTH-02 — Sign-up invalid => 400 Bad Request
**Steps:**
1. Trimite `POST /api/auth/sign-up` cu date invalide (ex: lipsă parolă / email invalid).
2. Observă răspunsul.

**Expected Result:**
- `400 Bad Request`

---

### TC-AUTH-03 — Sign-up conflict => 409 Conflict
**Precondition:** există deja un user cu același username/email.  
**Steps:**
1. Trimite `POST /api/auth/sign-up` cu username/email deja existente.
2. Observă răspunsul.

**Expected Result:**
- `409 Conflict`

---

### TC-AUTH-04 — Login valid => 200 OK + token
**Precondition:** există user creat (ex: din TC-AUTH-01).  
**Steps:**
1. Trimite `POST /api/auth/login` cu credentiale corecte.
2. Observă răspunsul.

**Expected Result:**
- `200 OK`
- răspunsul conține token JWT (ex: câmp `token`).

---

### TC-AUTH-05 — Login failed => 401 Unauthorized
**Steps:**
1. Trimite `POST /api/auth/login` cu user/parolă greșite.
2. Observă răspunsul.

**Expected Result:**
- `401 Unauthorized`

---

### TC-AUTH-06 — Use token to connect to GameHub
**Precondition:** token JWT obținut din TC-AUTH-04.  
**Steps:**
1. Conectează-te la `/gamehub` folosind token-ul (ex: `access_token` în query string).
2. Observă handshake-ul.

**Expected Result:**
- conexiune acceptată (nu primești 401).

---

### TC-02 — GET user (existing)
**Precondition:** există un user valid în DB.  
**Steps:**
1. Apelează `GET /api/users/{id}`.

**Expected Result:**
- `200 OK` + JSON cu `id` și `username`.

---

### TC-03 — GET user (not found)
**Steps:**
1. Apelează `GET /api/users/{randomGuid}`.

**Expected Result:**
- `404 NotFound`.

---

### TC-04 — PUT update username valid
**Precondition:** există user cu `id`.  
**Steps:**
1. Apelează `PUT /api/users/{id}` cu `{ "username": "Alex123" }`.

**Expected Result:**
- `200 OK` + username actualizat.

---

### TC-05 — PUT update username invalid (too short)
**Precondition:** există user cu `id`.  
**Steps:**
1. Apelează `PUT /api/users/{id}` cu `{ "username": "ab" }`.

**Expected Result:**
- `400 ValidationProblem`.

---

### TC-06 — PUT update username invalid (too long)
**Precondition:** există user cu `id`.  
**Steps:**
1. Apelează `PUT /api/users/{id}` cu username de 15+ caractere.

**Expected Result:**
- `400 ValidationProblem`.

---

### TC-07 — DELETE user existing
**Precondition:** există user cu `id`.  
**Steps:**
1. Apelează `DELETE /api/users/{id}`.

**Expected Result:**
- `204 NoContent`.

---

### TC-08 — DELETE user not found
**Steps:**
1. Apelează `DELETE /api/users/{randomGuid}`.

**Expected Result:**
- `404 NotFound`.

---

### TC-09 — GameService root endpoint
**Steps:**
1. Pornește `GameService`.
2. Deschide `GET /` în browser.

**Expected Result:**
- răspuns text: “Poker GameService Backend is running and ready for connections!”

---

### TC-10 — SignalR connect without token
**Steps:**
1. Încearcă conectarea la `/gamehub` fără JWT (client de test / tool).

**Expected Result:**
- conexiune refuzată (`401 Unauthorized`).

---

### TC-11 — SignalR connect with valid token
**Precondition:** există un JWT valid conform config.  
**Steps:**
1. Conectează-te la `/gamehub` cu token valid.

**Expected Result:**
- conexiune acceptată.

---

### TC-12 — StateUpdated received after action
**Precondition:** jucători conectați + room valid.  
**Steps:**
1. Apelează o acțiune de joc (ex: `PlaceBet` sau `DiscardCards`).
2. Monitorizează mesajele primite de client.

**Expected Result:**
- clientul primește `StateUpdated` cu `TableStateDto`.

---

### TC-13 — RabbitMQ event creates user (integration)
**Precondition:** RabbitMQ rulează + UserService consumer activ.  
**Steps:**
1. Publică un event `auth.user_registered` cu `UserId` și `Username`.
2. Apelează `GET /api/users/{UserId}`.

**Expected Result:**
- `200 OK` cu user creat.

---

### TC-14 — UI demo pages open correctly
**Steps:**
1. Deschide paginile demo (home/login/lobbies/profile/game).

**Expected Result:**
- paginile se afișează corect, fără layout issues majore.

---

## 4) Exit Criteria (pentru demo)
- TC-01, TC-02/03, TC-04/05, TC-09, TC-10/11 sunt trecute.
- Pentru integrări opționale (RabbitMQ), TC-13 este “nice to have”, dar nu blochează demo dacă nu e disponibil.
