# LinkUp — Connect. Share. Chat.

A Facebook-style social platform: posts & feed, friends, realtime chat, notifications,
and 1:1 / group **audio & video calling**. Built as a deliberate **modular monolith**.

- **Backend:** .NET 10 Web API (C#) · EF Core 10 (PostgreSQL) · ASP.NET Identity + JWT · SignalR · Cloudinary · AutoMapper · FluentValidation · Serilog
- **Frontend:** Angular 22 (standalone components, signals, lazy routes) · Tailwind · Angular Material · `@microsoft/signalr`
- **Realtime:** SignalR hubs (`ChatHub`, `NotificationHub`, `VideoCallHub`) + WebRTC for peer-to-peer media

## 📄 Documentation

**[LinkUp — Realtime Architecture (PDF)](docs/LinkUp-Realtime-Architecture.pdf)** —
a step-by-step technical guide to how **chat, notifications, and audio/video calling**
work internally over SignalR and WebRTC, with sample code from the source. It covers:

1. Architecture overview (what goes over REST vs SignalR vs WebRTC)
2. SignalR fundamentals — JWT-authenticated hubs, personal vs room groups
3. Realtime chat — presence, message flow, typing, delivery/read receipts
4. Realtime notifications — cross-module, settings-aware, per-user push
5. Audio & video calls — the WebRTC handshake (SDP/ICE), mesh signaling
6. Ringtone, call-end synchronization, and noise cancellation
7. Client wiring summary (server event → client handler → effect)

> The PDF is generated from [`docs/realtime-architecture.html`](docs/realtime-architecture.html).

## Architecture

```
Controller → Manager (business logic) → Repository/DbContext → EF Core → PostgreSQL
```

Each feature is a project under `src/Modules/<Name>` (Identity, UserProfile, Friend, Post,
Comment, Reaction, Chat, Notification, VideoCall, Media, Search, Admin) with its own
`DbContext` and PostgreSQL schema. Realtime data is pushed over SignalR; audio/video media
streams peer-to-peer via WebRTC (the server only relays signaling).

```
LinkUp.slnx
src/
  API/            → host (Program.cs wires every module, JWT, SignalR, Swagger, migrations)
  BuildingBlocks/ → BaseEntity, ApiResponse, PagedResult, GlobalExceptionMiddleware, ...
  SharedKernel/   → enums + AppConstants + helpers (e.g. MentionParser)
  Modules/        → feature modules (see above)
linkup-web/       → Angular client
docs/             → architecture PDF + source HTML
```

## Getting started

### Prerequisites
- .NET 10 SDK, Node 20+, and a PostgreSQL instance (Docker: a `postgres` container on `:5432`).

### Run the API (from repo root)
```bash
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS="https://localhost:5001;http://localhost:5000" \
dotnet run --project src/API/LinkUp.API.csproj --no-launch-profile
```
> You **must** pass `--no-launch-profile` — otherwise `dotnet run` uses `launchSettings.json`
> ports (5126/7041), which the Angular env does not expect. Swagger: `https://localhost:5001/swagger`.
> Migrations and admin/role seeding run automatically on startup.

### Run the frontend
```bash
cd linkup-web
npm install
npm start        # ng serve on http://localhost:4200
```
> Use `npm start` (or `npx ng serve`) — the Angular CLI is a local dev dependency, so a bare
> `ng serve` only works if the CLI is installed globally. The client calls `https://localhost:5001/api/v1`
> and connects hubs at `…/hubs`; CORS is pre-configured for `http://localhost:4200`.

### Seeded admin
`admin@linkup.com` / `Admin@123` (Admin role). Regular registration returns a token immediately.

## Features

- **Feed & posts** — create/share/report posts, reactions with live counts, comments & replies, **infinite scroll**.
- **Friends** — requests (send/accept/reject/cancel), suggestions ("People You May Know" shows everyone for new users), block/unblock, mutual-friend counts.
- **Profiles** — full profile (bio, DOB, education, experience, social links), friendship-aware action buttons, avatar/cover upload.
- **Realtime chat** — 1:1 and group, presence (online/last-seen), typing, delivery/read receipts, @mentions, attachments, emoji, voice notes.
- **Notifications** — cross-module, per-category settings, live unread badge.
- **Audio & video calls** — 1:1 and group (WebRTC mesh), incoming-call **ringtone**, synchronized call-end on decline/no-answer, **noise cancellation**, screen share, call history.
- **Admin** — dashboard stats, user management (suspend/delete), report moderation.

## Tests

```bash
node api-smoke-test.mjs                       # REST smoke test across all modules
node linkup-web/ui-login-test.mjs             # browser login → feed
node linkup-web/ui-infinite-scroll-test.mjs   # feed infinite scroll
node linkup-web/ui-profile-test.mjs           # other-user profile shows full info
node linkup-web/ui-ringtone-test.mjs          # incoming-call ringtone (two browsers)
node linkup-web/ui-call-decline-test.mjs      # call ends on both sides
node linkup-web/video-call-test.mjs           # two-browser WebRTC video-call e2e
```
> Browser tests use `puppeteer-core` against installed Chrome and `NODE_TLS_REJECT_UNAUTHORIZED=0` for the dev cert.
