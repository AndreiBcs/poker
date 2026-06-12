# Poker Arena — Requirements 

## 1. Obiectiv proiect

Scopul proiectului Poker Arena este realizarea unui demo de aplicație de poker bazată pe un framework scalabil și modular, cu:

- management utilizatori (profil + autentificare/integrări),
- lobby-uri pentru organizarea jocurilor,
- joc în timp real folosind SignalR,
- suport pentru două tipuri de joc: Texas Hold’em și Five Card Draw.

## 2. Scope 

### In scope

- Sistemul oferă endpoint-uri de autentificare pentru:
  - sign-up (creare cont),
  - login (autentificare).
- La autentificare reușită, sistemul generează un Cookie de sesiune securizat care va fi folosit pentru acces la componente protejate, de exemplu GameHub (`/gamehub`).
- Sistemul validează input-ul prin DTO-uri și validatori și returnează răspunsuri standard pentru erori:
  - `400 Bad Request` pentru date invalide,
  - `409 Conflict` pentru conflict, de exemplu username/email deja existent,
  - `401 Unauthorized` pentru login eșuat.
- Sistemul poate crea automat un utilizator prin eveniment de tip `user_registered`.
- Sistemul permite citirea, modificarea și ștergerea datelor de profil, de exemplu username.
- Utilizatorii se pot conecta la un hub SignalR (`/gamehub`) pe baza autentificării prin Cookies.
- Serverul trimite update-uri de stare către clienți, de exemplu `StateUpdated`.
- Există logică de joc pentru:
  - Texas Hold’em: `PreFlop -> Flop -> Turn -> River -> Showdown`
  - Five Card Draw: `InitialDeal -> DrawCards -> SecondBetting -> Showdown`
- Există evaluare de mână printr-o librărie externă pentru ranking, categorie și descriere.
- Există pagini UI/demo pentru flow-ul aplicației: `home`, `login`, `lobbies`, `profile`, `game`.

### Out of scope

- Resetare parolă.
- Email verification.
- Sesiuni avansate pe bază de tokens.
- Rate limiting și brute-force protection.
- Matchmaking automat și leaderboards.
- Plăți și monetizare.
- Anti-cheat și audit log complet.
- Administrare avansată a lobby-urilor, de exemplu ban/kick, invite links, lobby private/public complet.
- Teste automate complete; pentru MVP se acceptă testare manuală.

## 3. User Stories 

### US-00A — Sign-up

Ca utilizator nou, vreau să îmi pot crea un cont, pentru ca să mă pot autentifica și folosi aplicația.

### US-00B — Login

Ca utilizator, vreau să mă pot autentifica și să primesc un Cookie securizat de sesiune, pentru ca să pot accesa hub-ul SignalR și funcționalitățile protejate.

### US-01 — Create user via event

Ca sistem, vreau ca UserService să creeze automat un user când primește evenimentul `auth.user_registered`, pentru ca utilizatorul să aibă profil în aplicație fără creare manuală.

### US-02 — View user profile

Ca utilizator, vreau să pot vedea datele de profil, pentru ca să știu ce cont este activ.

### US-03 — Update username

Ca utilizator, vreau să pot schimba username-ul, pentru ca să îmi personalizez profilul.

### US-04 — Delete user profile

Ca utilizator, vreau să pot șterge profilul, pentru ca să îmi pot elimina contul din sistem.

### US-05 — Connect to GameHub securely

Ca utilizator autentificat, vreau să mă conectez la hub-ul SignalR `/gamehub` folosind Cookie-ul valid, pentru ca doar userii autentificați să poată interacționa cu jocul.

### US-06 — Receive game state updates

Ca jucător, vreau să primesc update-uri de stare, pentru ca să văd evoluția jocului în timp real.

### US-07 — Place bet

Ca jucător, vreau să pot plasa un pariu, pentru ca jocul să progreseze și starea să se actualizeze pentru toți jucătorii.

### US-08 — Discard cards in Five Card Draw

Ca jucător, vreau să pot discarda cărți și să primesc noua stare, pentru ca să pot juca runda de Draw.

### US-09 — Lobby basic lifecycle

Ca host, vreau să pot crea și șterge un lobby, pentru ca să pot organiza un joc 
