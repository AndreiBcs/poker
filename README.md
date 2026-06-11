# Poker Arena

Poker Arena is a demo poker platform built with a microservices-style architecture.  
The project includes user management, lobby management, and real-time gameplay using SignalR.  
It supports two poker variants: **Texas Hold'em** and **Five Card Draw**.

## Project Overview

Poker Arena is split into multiple services and layers:

- **UserService** — authentication, user profile management, and user-related persistence.
- **LobbyService** — lobby creation, lobby listing, and player management inside lobbies.
- **GameService** — real-time poker gameplay, game phases, state machines, and hand evaluation.
- **Messaging** — RabbitMQ-based event communication between services.
- **Frontend/Demo UI** — simple pages for login, lobby browsing, profile viewing, and gameplay flow.

## Features

### User management
- Sign up and login.
- JWT-based authentication.
- Get user profile by id.
- Update username.
- Delete user profile.
- Create user automatically from `auth.user_registered` event.

### Lobby management
- Create lobby.
- Delete lobby.
- Add and remove players from lobby.
- Persist lobby and player data with Entity Framework Core.

### Real-time gameplay
- SignalR hub for live game communication.
- Authenticated access to `/gamehub`.
- Real-time state updates for players.
- Game actions like bet and discard cards.
- Support for:
  - Texas Hold'em
  - Five Card Draw

### Game logic
- Deck generation and shuffle.
- Card, suit, rank, and game phase models.
- State machine for Texas Hold’em:
  - PreFlop
  - Flop
  - Turn
  - River
  - Showdown
- State machine for Five Card Draw:
  - InitialDeal
  - DrawCards
  - SecondBetting
  - Showdown
- Hand evaluation using an external evaluator library.

## Architecture

The project is structured around clear separation of responsibilities:

- **Domain / State** — core poker models like `Player`, `Table`, `Card`, `Deck`, `GamePhase`.
- **Core / GameLogic** — game state machines and hand evaluation.
- **Features** — API endpoints and service logic.
- **Infrastructure** — repositories, persistence, and messaging.
- **Hubs** — SignalR hub for live gameplay.
- **DTOs** — safe data transfer objects sent to clients.

## Tech Stack

- .NET 10
- ASP.NET Core Minimal APIs
- SignalR
- RabbitMQ
- Entity Framework Core
- SQLite
- FluentValidation
- JWT Bearer Authentication

## Services

### UserService
Handles authentication and user profile operations.

Main endpoints:
- `POST /api/auth/sign-up`
- `POST /api/auth/login`
- `GET /api/users/{id}`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`

### LobbyService
Handles lobby lifecycle and player management.

Main responsibilities:
- create lobby
- delete lobby
- add player
- remove player
- store data in database

### GameService
Handles the real-time poker game.

Main responsibilities:
- manage game rooms
- handle player actions
- advance game phases
- compute winners at showdown
- broadcast state updates through SignalR

## Authentication

The project uses JWT authentication.  
After successful login, the client receives a JWT token and uses it to access protected endpoints and the SignalR hub.

For SignalR, the token can be passed through the `access_token` query string when connecting to `/gamehub`.

## Messaging

Poker Arena uses RabbitMQ for asynchronous communication between services.

Example event:
- `auth.user_registered`

This event is used to create a user automatically in `UserService`.

## How the gameplay works

### Texas Hold'em
1. Players receive 2 hole cards.
2. Betting happens.
3. Community cards are revealed:
   - Flop
   - Turn
   - River
4. Showdown decides the winner.

### Five Card Draw
1. Players receive 5 cards.
2. First betting round.
3. Players discard selected cards.
4. Second betting round.
5. Showdown decides the winner.

## Running the project locally

### Prerequisites
- .NET 10 SDK
- SQLite
- RabbitMQ running locally if you want to test messaging
- A JWT configuration for `GameService`

### Run UserService
```bash
dotnet watch run
```

### Run GameService
```bash
dotnet watch run
```

### Optional: run RabbitMQ
Use a local RabbitMQ instance if you want to test event-based user creation.

## Configuration

Make sure the following settings are available in your `appsettings*.json` files:

- `DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Secret`
- RabbitMQ host, port, username, password

## Demo Pages

The project includes simple UI pages for:
- `home`
- `login`
- `lobbies`
- `profile`
- `game`

These pages are meant to demonstrate the application flow and the relationship between services.

## MVP Scope

### Included
- user sign-up and login
- JWT authentication
- user profile management
- lobby management
- real-time game updates
- Texas Hold’em and Five Card Draw logic
- manual testing for demo

### Not included
- password reset
- email verification
- refresh tokens
- advanced anti-cheat
- monetization
- matchmaking system
- full automated test suite

## Project Goals

The main goal of Poker Arena is to demonstrate:
- modular backend design,
- service separation,
- real-time communication,
- event-driven integration,
- and poker game state management.

## Notes

This project was built as a learning and demo application, with focus on:
- clean separation of responsibilities,
- practical software architecture,
- and hands-on implementation of backend and game logic.

## License

For educational/demo use.
