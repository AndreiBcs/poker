Poker Arena — Requirements (MVP)
1. Obiectiv proiect
Scopul proiectului Poker Arena este realizarea unui demo de aplicație de poker bazată pe o arhitectură tip microservices, cu:

management utilizatori (profil + autentificare/integrare),
lobby-uri pentru organizarea jocurilor,
joc în timp real (real-time) folosind SignalR,
suport pentru două tipuri de joc: Texas Hold’em și Five Card Draw.
2. Scope MVP (In / Out)
In Scope (MVP)
## 2.X Auth (Autentificare) — Scope MVP

### In Scope
- Sistemul oferă endpoint-uri de autentificare pentru:
  - **Sign-up** (creare cont)
  - **Login** (autentificare)
- La autentificare reușită, sistemul generează un **JWT token** care va fi folosit pentru acces la componente protejate (ex: GameHub `/gamehub`).
- Sistemul validează input-ul (DTO + validator) și returnează răspunsuri standard pentru erori:
  - `400 Bad Request` pentru date invalide
  - `409 Conflict` pentru conflict (ex: username/email deja există)
  - `401 Unauthorized` pentru login eșuat

### Out of Scope (pentru MVP)
- Resetare parolă / email verification.
- Refresh tokens și sesiuni avansate.
- Rate limiting / brute-force protection.

User Management

Sistemul poate crea un utilizator (prin eveniment de tip user_registered).
Sistemul permite citirea / modificarea / ștergerea datelor de profil (ex: username).
Game (Real-time)

Utilizatorii se pot conecta la un hub SignalR (/gamehub) cu autentificare JWT.
Serverul trimite update-uri de stare către clienți (ex: StateUpdated).
Gameplay logic

Există logică de joc pentru:
Texas Hold’em: PreFlop → Flop → Turn → River → Showdown
Five Card Draw: InitialDeal → DrawCards → SecondBetting → Showdown
Există evaluare de mână (hand evaluation) folosind o librărie externă (ranking/category/description).

Există pagini UI/demo (home/login/lobbies/profile/game) pentru prezentarea flow-ului.
Out of Scope (pentru MVP / demo)
Matchmaking automat și clasificări (leaderboards).
Plăți / monetizare.
Anti-cheat / audit log complet.
Administrare avansată a lobby-urilor (ban/kick, invite links, lobby private/public complet).
Teste automate complete (unit/integration) — doar testare manuală pentru demo.
3. User Stories (MVP)
Legendă prioritate: Must (obligatoriu pentru demo), Should (important), Could (nice to have)

US-01 (Must) — Create User via event
Ca sistem,
vreau ca UserService să creeze automat un user când primește evenimentul auth.user_registered,
pentru ca utilizatorul să aibă profil în aplicație fără creare manuală.

US-02 (Must) — View user profile
Ca utilizator,
vreau să pot vedea datele de profil (id, username),
pentru ca să știu ce cont este activ.

US-03 (Must) — Update username
Ca utilizator,
vreau să pot schimba username-ul,
pentru ca să îmi personalizez profilul.

US-04 (Should) — Delete user profile
Ca utilizator,
vreau să pot șterge profilul,
pentru ca să îmi pot elimina contul din sistem.

US-05 (Must) — Connect to GameHub securely
Ca utilizator autentificat,
vreau să mă conectez la hub-ul SignalR /gamehub folosind JWT,
pentru ca doar userii autentificați să poată interacționa cu jocul.

US-06 (Must) — Receive game state updates
Ca jucător,
vreau să primesc update-uri de stare (board cards, pot, player state),
pentru ca să văd evoluția jocului în timp real.

US-07 (Should) — Place bet / trigger state update
Ca jucător,
vreau să pot plasa un pariu (bet),
pentru ca jocul să progreseze și starea să se actualizeze pentru toți jucătorii.

US-08 (Should) — Discard cards in Five Card Draw
Ca jucător (Five Card Draw),
vreau să pot discarda cărți și să primesc noua stare,
pentru ca să pot juca runda de Draw.

US-09 (Could) — Lobby basic lifecycle
Ca host,
vreau să pot crea/șterge un lobby,
pentru ca să pot organiza un joc cu alți jucători.

US-10 (Could) — Join/Leave lobby
Ca jucător,
vreau să pot intra/ieși dintr-un lobby,
pentru ca să pot participa la un joc.
