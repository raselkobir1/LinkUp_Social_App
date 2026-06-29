# CLAUDE.md

Guidance for working in this repository. **LinkUp** — a Facebook-style social platform (Connect. Share. Chat.). `Documentation.txt` is the original spec.

## Tech stack

- **Backend**: .NET 10 Web API (C#), solution `LinkUp.slnx` at repo root
- **DB**: PostgreSQL via EF Core 10 (Npgsql) — each module owns its own `DbContext` + PostgreSQL schema
- **Auth**: ASP.NET Identity + JWT Bearer + refresh tokens
- **Real-time**: SignalR — `ChatHub`, `NotificationHub`, `VideoCallHub`
- **Media**: Cloudinary (creds are placeholders in dev — uploads won't work without real keys)
- **Other**: AutoMapper, FluentValidation, Asp.Versioning, Serilog
- **Frontend**: Angular 22 (standalone components, signals, lazy routes) + Tailwind + Angular Material + `@microsoft/signalr`, in `linkup-web/`

## Architecture

Controller → Manager (business logic) → Repository/DbContext → EF Core → PostgreSQL.

**Do NOT introduce** MediatR, CQRS, or Event Sourcing — this is a deliberate modular monolith.

Cross-module dependencies are fine in this monolith (e.g. inject `UserManager<ApplicationUser>` where user data is needed). Each module is a project under `src/Modules/<Name>` with: `Controllers`, `Managers`, `Interfaces`, `DTOs`, `Entities`, `Configuration` (DbContext + `Add<Module>Module` DI extension), `Mappings`, `Validators`, `Migrations`.

```
LinkUp.slnx
src/
  API/            → host (Program.cs wires every module, JWT, SignalR, Swagger, migrations)
  BuildingBlocks/ → BaseEntity, AuditableEntity, IRepository/UnitOfWork, ApiResponse, PagedResult,
                    GlobalExceptionMiddleware, BaseApiController
  SharedKernel/   → enums + AppConstants
  Modules/        → Identity, UserProfile, Friend, Post, Comment, Reaction, Chat,
                    Notification, VideoCall, Media, Search, Admin
linkup-web/       → Angular client
```

### Conventions
- Entities extend `AuditableEntity` (CreatedAt/UpdatedAt/CreatedById/UpdatedById/IsDeleted/DeletedAt — soft delete).
- Controllers extend `BaseApiController` (`CurrentUserId`, `IsAuthenticated`, `ApiOk`/`ApiCreated`/`ApiNotFound`/`ApiOkPaged`).
- Routes: `api/v{version:apiVersion}/[controller]` (some controllers set an explicit `[Route]`).
- Responses are wrapped in `ApiResponse<T>` `{ success, message, data, errors, statusCode }`; lists in `PagedResult<T>`.
- Managers throw domain exceptions (`NotFoundException`, `UnauthorizedException`, `ForbiddenException`, `ConflictException`, `ValidationException`); `GlobalExceptionMiddleware` maps them to 404/401/403/409/400.

## Build / run / test

**Database** (Docker): a `postgres` container on `:5432`. App expects role `linkup` / `linkup@123`, db `LinkUpDb` (see `appsettings.Development.json`). Migrations + role/admin seeding run automatically on API startup (in `Program.cs`).

**Run the API** (from repo root):
```bash
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS="https://localhost:5001;http://localhost:5000" \
dotnet run --project src/API/LinkUp.API.csproj --no-launch-profile
```
⚠️ **You must pass `--no-launch-profile`** — otherwise `dotnet run` uses `launchSettings.json` ports (5126/7041), which the Angular env does not expect. Swagger: `https://localhost:5001/swagger`.

**Run the frontend**: `cd linkup-web && ng serve` (port 4200). It calls `https://localhost:5001/api/v1`; hubs at `…/hubs`. CORS is pre-configured for `http://localhost:4200`.

**Seeded admin**: `admin@linkup.com` / `Admin@123` (Admin role). Regular registration also returns a token immediately (no email-verification gate on login).

**Tests** (Node, repo root unless noted):
- `node api-smoke-test.mjs` — REST smoke test across all modules (currently 34 checks).
- `linkup-web/ui-login-test.mjs` — real-browser login → feed (puppeteer-core, uses installed Chrome).
- `linkup-web/video-call-test.mjs` — two-browser WebRTC video-call e2e.
- These use `puppeteer-core` against the installed Chrome and `NODE_TLS_REJECT_UNAUTHORIZED=0` for the dev cert. For headless WebRTC, Chrome needs `--use-fake-device-for-media-stream --use-fake-ui-for-media-stream --disable-features=WebRtcHideLocalIpsWithMdns`.

## Gotchas learned the hard way (read before debugging these areas)

- **JWT auth returns 401 on everything**: `AddIdentity()` (inside `AddIdentityModule`) resets the default auth scheme to the Identity cookie, and it runs *after* the JWT `AddAuthentication` in `Program.cs`. Fix already in place: JWT default scheme is **re-asserted after all module registrations**. If `[Authorize]` starts 401-ing with valid tokens, check this ordering first.
- **Enums are serialized as strings**: `Program.cs` registers `JsonStringEnumConverter`. The Angular models use string-union enums (`'Public'`, `'Like'`, `'Text'`…). Input accepts both string and int. Don't remove the converter or the UI's create/display flows break.
- **Migrations blocked by `PendingModelChangesWarning`**: `IdentityDbContext.SeedRoles` `HasData` must use **static `ConcurrencyStamp`** values (matching the snapshot) — `ApplicationRole` otherwise regenerates a random stamp each model build.
- **Swagger 500 on file uploads**: `IFormFile` actions need `[Consumes("multipart/form-data")]` and no `[FromForm]` on the `IFormFile` param.
- **Profiles are lazily created** in `ProfileManager.Get/UpdateProfile` (not on registration).
- **DTO shape for the client**: backend DTOs expose **flat fields the Angular client reads** (e.g. `MessageDto.SenderId/SenderName/SenderProfilePicture`, `CommentDto.AuthorName/IsLiked`, `NotificationDto.SenderName`) as computed properties over the nested objects. `ChatListItemDto` exposes flat `OtherUser*` + string `LastMessage`/`LastMessageAt`. Keep frontend and backend field names aligned (camelCase).
- **SignalR sends positional args**, not single objects. Frontend `.on(...)` handlers must take positional params. Hub invoke argument order must match the server method signature exactly.
- **Video call** (`VideoCallHub` + `call.service.ts` + `video-call.component.ts`): 1:1 WebRTC works and is e2e-tested. SDP/ICE are relayed as JSON strings. Caller creates the offer on `CallAccepted`; callee answers the offer; ICE candidates are buffered until the remote description is set. The hub is connected app-wide from the shell so incoming calls ring anywhere. Group calls + call-history UI are not built yet.

## Known gaps (not yet implemented)
Profile picture/cover upload (no controller endpoint + needs real Cloudinary), block-user, notification settings, several unwired comment/post action buttons (reply/like/edit/delete, share, edit post), group-chat management UI, chat attachments/voice/emoji/@mentions/last-seen, search-posts/groups, email-verification frontend, video group calls + call history.

## Working agreements
- Match the existing module/file layout and naming exactly when adding features.
- When you change a frontend service route or a DTO shape, verify the *other* side — frontend↔backend contract mismatches are the most common bug class here.
- Commit/push only when asked. Default branch is `main`.
