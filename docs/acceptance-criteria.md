# Poker Arena — Acceptance Criteria 

## 1. UserService — API `/api/users`

### AC-US-GET-01 — Get user by id

**Given** există un user cu `id` în baza de date,  
**When** fac `GET /api/users/{id}`,  
**Then** primesc `200 OK` cu:

- `id` (GUID)
- `username` (string)

### AC-US-GET-02 — Get user not found

**Given** nu există user cu `id`,  
**When** fac `GET /api/users/{id}`,  
**Then** primesc `404 NotFound`.

### AC-US-PUT-01 — Update username valid

**Given** există un user cu `id`,  
**When** fac `PUT /api/users/{id}` cu body:

```json
{ "username": "Alex123" }
```

**Then** primesc `200 OK` și username-ul este actualizat.

### AC-US-PUT-02 — Update username too short/too long

**When** fac `PUT /api/users/{id}` cu username mai scurt de 3 caractere sau mai lung de 14 caractere,  
**Then** primesc `400 ValidationProblem` cu erori de validare.

### AC-US-PUT-03 — Update user not found

**Given** nu există user cu `id`,  
**When** fac `PUT /api/users/{id}`,  
**Then** primesc `404 NotFound`.

### AC-US-DEL-01 — Delete existing user

**Given** există un user cu `id`,  
**When** fac `DELETE /api/users/{id}`,  
**Then** primesc `204 NoContent`.

### AC-US-DEL-02 — Delete user not found

**Given** nu există user cu `id`,  
**When** fac `DELETE /api/users/{id}`,  
**Then** primesc `404 NotFound`.

## 2. Auth — `/api/auth`

### AC-AUTH-SIGNUP-01 — Sign-up valid => created

**Given** datele de sign-up sunt valide,  
**When** fac `POST /api/auth/sign-up`,  
**Then** primesc `201 Created` și un răspuns care conține datele user-ului și setează Cookie-ul de sesiune.

### AC-AUTH-SIGNUP-02 — Sign-up invalid => bad request

**When** fac `POST /api/auth/sign-up` cu date invalide,  
**Then** primesc `400 Bad Request`.

### AC-AUTH-SIGNUP-03 — Sign-up conflict => conflict

**Given** există deja un user cu același username sau email,  
**When** fac `POST /api/auth/sign-up`,  
**Then** primesc `409 Conflict`.

### AC-AUTH-LOGIN-01 — Login valid => ok

**Given** user-ul există și credentialele sunt corecte,  
**When** fac `POST /api/auth/login`,  
**Then** primesc `200 OK` și un răspuns cu header-ul `Set-Cookie` care conține session cookie-ul securizat.

### AC-AUTH-LOGIN-02 — Login invalid input => bad request

**When** fac `POST /api/auth/login` cu input invalid,  
**Then** primesc `400 Bad Request`.

### AC-AUTH-LOGIN-03 — Login failed => unauthorized

**Given** credentialele sunt greșite,  
**When** fac `POST /api/auth/login`,  
**Then** primesc `401 Unauthorized`.

## 3. SignalR (GameService) — `/gamehub`

### AC-HUB-01 — No cookie => unauthorized

**Given** un client încearcă să se conecteze la `/gamehub` fără un Cookie de autentificare valid,  
**When** se face handshake,  
**Then** conexiunea este refuzată cu `401 Unauthorized`.

### AC-HUB-02 — Valid cookie => connected

**Given** un client are un Cookie de sesiune valid,  
**When** se conectează la `/gamehub`,  
**Then** conexiunea este acceptată.

### AC-HUB-03 — Receive state updates

**Given** există o masă validă și jucători conectați,  
**When** se apelează o acțiune de joc,  
**Then** fiecare jucător primește un mesaj `StateUpdated` cu un `TableStateDto` care conține:

- `BoardCards`
- `Pot`
- `Phase`
- `Players[]`

## 4. RabbitMQ event — `auth.user_registered`

### AC-EVENT-01 — User created on event

**Given** UserService rulează și RabbitMQ este disponibil,  
**When** se publică un event `auth.user_registered` cu payload ce conține `UserId` și `Username`,  
**Then** UserService creează user în baza de date.

### AC-EVENT-02 — Duplicate event behavior

**Given** același eveniment este livrat de două ori,  
**When** UserService procesează event-ul,  
**Then** pentru MVP comportamentul este acceptat ca best effort, fără garanții de idempotency.
