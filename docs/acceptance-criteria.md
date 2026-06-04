# Poker Arena — Acceptance Criteria (MVP)

## 1) UserService — API `/api/users`

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

---

### AC-US-PUT-01 — Update username valid
**Given** există un user cu `id`,  
**When** fac `PUT /api/users/{id}` cu body:
```json
{ "username": "Alex123" }
```
**Then** primesc `200 OK` și username-ul este actualizat.

### AC-US-PUT-02 — Update username too short/too long
**When** fac `PUT /api/users/{id}` cu username < 3 caractere sau > 14 caractere,  
**Then** primesc `400 ValidationProblem` (cu erori de validare).

### AC-US-PUT-03 — Update user not found
**Given** nu există user cu `id`,  
**When** fac `PUT /api/users/{id}`,  
**Then** primesc `404 NotFound`.

---

### AC-US-DEL-01 — Delete existing user
**Given** există un user cu `id`,  
**When** fac `DELETE /api/users/{id}`,  
**Then** primesc `204 NoContent`.

### AC-US-DEL-02 — Delete user not found
**Given** nu există user cu `id`,  
**When** fac `DELETE /api/users/{id}`,  
**Then** primesc `404 NotFound`.

---

## 2) SignalR (GameService) — `/gamehub`

### AC-HUB-01 — No token => unauthorized
**Given** un client încearcă să se conecteze la `/gamehub` fără token JWT,  
**When** se face handshake,  
**Then** conexiunea este refuzată (`401 Unauthorized`).

### AC-HUB-02 — Valid token => connected
**Given** un client are token JWT valid,  
**When** se conectează la `/gamehub`,  
**Then** conexiunea este acceptată.

### AC-HUB-03 — Receive state updates
**Given** există o masă/room validă și jucători conectați,  
**When** se apelează o acțiune de joc (ex: PlaceBet/DiscardCards),  
**Then** fiecare jucător primește un mesaj `StateUpdated` cu un `TableStateDto`:
- `BoardCards`
- `Pot`
- `Phase`
- `Players[]` (cu datele corecte)

---

## 3) RabbitMQ event — `auth.user_registered`

### AC-EVENT-01 — User created on event
**Given** `UserService` rulează și RabbitMQ este disponibil,  
**When** se publică un event `auth.user_registered` cu payload ce conține `UserId` și `Username`,  
**Then** `UserService` creează user în baza de date (SQLite).

### AC-EVENT-02 — Idempotency/duplicates (nota MVP)
**Given** același eveniment este livrat de două ori (scenariu posibil în messaging),  
**When** UserService procesează event-ul,  
**Then** comportamentul trebuie clarificat pentru versiuni viitoare (MVP: se acceptă comportament “best effort”, fără garanții de idempotency).
