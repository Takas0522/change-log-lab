# ToDo App System Spec

> Version control is handled by Git. No revision history or cover page or anything. Abbreviations aren't defined on first use either. This is for people who already know the stack.

A microservices-based ToDo app. 4 backends + Angular SPA. Each service has its own PG database, all tied together through a BFF.

---

## Architecture

```
┌──────────────────┐      ┌─────────────────────┐
│   Angular SPA    │─────▶│    BFF Service       │
│  (localhost:4200)│      │  (localhost:5000)    │
└────────┬─────────┘      └──────┬──┬──┬────────┘
         │                       │  │  │
         │ SignalR WebSocket     │  │  └──▶ User Service ──▶ user-db
         │                       │  │       (localhost:5003)
         │                       │  └─────▶ Todo Service ──▶ todo-db
         ▼                       │          (localhost:5001)
┌──────────────────┐             └────────▶ Auth Service ──▶ auth-db
│ Realtime Service │                        (localhost:5002)
│ (localhost:5004) │
└────────▲─────────┘
         │
┌────────┴─────────┐
│ Functions Service │◀── LISTEN/NOTIFY ── todo-db
│ (localhost:7071)  │    (outbox_events)
└──────────────────┘
```

## Tech Stack

- Frontend: Angular v20+ / TypeScript
- Backend: ASP.NET Core / C#
- Realtime: SignalR
- Event listener: .NET Azure Functions
- DB: PostgreSQL
- Auth: JWT (custom implementation)
- Storage emulator: Azurite
- Version pinning is done via package.json, global.json, Docker image tags, etc. as appropriate

## Services

| Service | Port | What it does | DB |
|---------|------|--------------|----|
| BFF | 5000 | API gateway sort of thing | none |
| Todo | 5001 | List/ToDo/Member CRUD + Outbox | todo-db |
| Auth | 5002 | Authentication, JWT issuance, session management | auth-db |
| User | 5003 | Profiles and user search | user-db |
| Realtime | 5004 | SignalR hub | none |
| Functions | 7071 | PG LISTEN → SignalR publish | none |

---

## DB

### auth-db

**users**

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| id | UUID | PK | User ID |
| email | VARCHAR(255) | UNIQUE, NOT NULL | Email address |
| password_hash | VARCHAR(255) | NOT NULL | bcrypt hash |
| display_name | VARCHAR(100) | NOT NULL | Display name |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

**device_sessions**

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| id | UUID | PK | |
| user_id | UUID | FK → users(id) CASCADE | |
| device_id | VARCHAR(255) | NOT NULL | Device identifier |
| session_version | INT | NOT NULL, DEFAULT 1 | Incremented on logout |
| last_login_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

UNIQUE(user_id, device_id)

### todo-db

**lists**

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| id | UUID | PK | |
| title | VARCHAR(200) | NOT NULL | List name |
| description | TEXT | | |
| owner_id | UUID | NOT NULL | Cross-service reference |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

**todos**

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| id | UUID | PK | |
| list_id | UUID | FK → lists(id) CASCADE | |
| title | VARCHAR(200) | NOT NULL | |
| description | TEXT | | |
| is_completed | BOOLEAN | NOT NULL, DEFAULT FALSE | |
| due_date | TIMESTAMPTZ | | Deadline |
| position | INT | NOT NULL, DEFAULT 0 | Sort order |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

**list_members**

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| id | UUID | PK | |
| list_id | UUID | FK → lists(id) CASCADE | |
| user_id | UUID | NOT NULL | Cross-service reference |
| role | VARCHAR(20) | NOT NULL, CHECK IN ('owner','editor','viewer') | |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

UNIQUE(list_id, user_id)

**outbox_events**

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| id | UUID | PK | |
| event_id | UUID | UNIQUE, NOT NULL | |
| event_type | VARCHAR(100) | NOT NULL | |
| aggregate_id | UUID | NOT NULL | |
| aggregate_type | VARCHAR(100) | NOT NULL | list or todo |
| payload | JSONB | NOT NULL | |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |
| processed_at | TIMESTAMPTZ | | Gets filled when processed |

Has an AFTER INSERT trigger that fires pg_notify('outbox_events', event_id).

### user-db

**user_profiles**

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| user_id | UUID | PK | |
| email | VARCHAR(255) | UNIQUE, NOT NULL | |
| display_name | VARCHAR(100) | NOT NULL | |
| bio | TEXT | | About me sort of thing |
| avatar_url | VARCHAR(500) | | |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

GIN index on display_name and email (for full-text search).

### ER-ish diagram

```
auth-db:  users ──1:N──▶ device_sessions
todo-db:  lists ──1:N──▶ todos
          lists ──1:N──▶ list_members
          outbox_events (standalone)
user-db:  user_profiles (standalone)

Cross-service: lists.owner_id, list_members.user_id → auth users (no actual FK)
```

---

## API

### Auth (/api/auth)

- `POST /api/auth/register` — Register. Send email, password, displayName. Returns 201. Email conflict returns 409.
- `POST /api/auth/login` — Login. Send email, password, deviceId. Returns a JWT (10-min expiry). Claims include userId, email, deviceId, sessionVersion. Auth failure returns 401.
- `POST /api/auth/logout` — Logout (auth required). Increments session_version, invalidating all JWTs for that device.
- `GET /api/auth/me` — Get your own info (auth required).

### Lists (/api/lists)

- `GET /api/lists` — Lists you're a member of
- `GET /api/lists/{id}` — List detail (includes ToDos and members). Member or above.
- `POST /api/lists` — Create a list. Send title and description. Automatically adds an owner record to list_members.
- `PUT /api/lists/{id}` — Update list. Owner/editor.
- `DELETE /api/lists/{id}` — Delete list. Owner only.
- `POST /api/lists/{id}/members` — Add member. Owner only. Send userId, role. 409 if already a member.
- `DELETE /api/lists/{id}/members/{memberId}` — Remove member. Owner only.

### ToDo (/api/lists/{listId}/todos)

- `GET /api/lists/{listId}/todos` — ToDo list (ordered by position). Member or above.
- `GET /api/lists/{listId}/todos/{id}` — ToDo detail. Member or above.
- `POST /api/lists/{listId}/todos` — Create ToDo. Owner/editor. Send title, description, dueDate. Position defaults to MAX+1 if not specified.
- `PUT /api/lists/{listId}/todos/{id}` — Update ToDo (partial update OK). Owner/editor.
- `DELETE /api/lists/{listId}/todos/{id}` — Delete ToDo. Owner/editor.

### User (/users)

- `GET /users/me` — Your profile
- `PUT /users/me` — Update profile (displayName, bio, avatarUrl)
- `GET /users/{userId}` — Specified user's profile
- `GET /users/search?q=&skip=&take=` — User search. Full-text search. Max 100 results.
- `POST /users` — Create profile (internal use)

### BFF

All just proxies. Forwards the Authorization header as-is. BFF itself does no auth.

| BFF Route | Forwarded to |
|-----------|-------------|
| api/auth/* | Auth Service |
| api/lists/* | Todo Service |
| api/lists/{listId}/todos/* | Todo Service |
| api/users/* | User Service |

Uses IHttpClientFactory with Named Clients.

### Errors

400 (validation), 401 (auth failure), 403 (insufficient permissions), 404 (not found), 409 (duplicate), 500 (something broke).

---

## How Auth Works

1. Login issues a JWT (valid for 10 minutes)
2. Claims: userId, email, deviceId, sessionVersion
3. Stored in sessionStorage (gone when the tab closes)
4. All requests get an Authorization header attached (interceptor)
5. Each service validates the JWT + checks sessionVersion against DB
6. Logout bumps sessionVersion, so all existing JWTs become invalid (for that device only)

deviceId is generated on the frontend and stored in localStorage. A single user can be logged in on multiple devices simultaneously.

Frontend guards:
- authGuard → Auth-required routes (/lists, /lists/:id)
- guestGuard → Login/register screens (redirects to /lists if already authenticated)

---

## Realtime Sync

Todo Service data change → outbox_events INSERT in the same transaction → pg_notify fires → Functions Service picks it up via LISTEN → POSTs to Realtime Service → SignalR over WebSocket → Angular SPA UI updates.

Event types: list_created, list_updated, list_deleted, todo_created, todo_updated, todo_deleted, member_added, member_removed

SignalR:
- Endpoint: localhost:5004/hubs/todo
- JWT auth, list-level groups, auto-reconnect
- Received events: ListUpdated, TodoUpdated

---

## Frontend

```
app/
├── app.component.ts
├── app.routes.ts
├── components/
│   ├── login/
│   ├── register/
│   ├── lists/
│   └── list-detail/
├── services/
│   ├── auth.service.ts
│   ├── list.service.ts
│   ├── todo.service.ts
│   ├── user.service.ts
│   └── realtime.service.ts
├── interceptors/
│   └── auth.interceptor.ts
├── guards/
│   ├── auth.guard.ts
│   └── guest.guard.ts
└── models/
    └── index.ts
```

Routing:

| Path | Component | Guard | Lazy loaded |
|------|-----------|-------|-------------|
| / | → /lists | - | - |
| /login | LoginComponent | guestGuard | yes |
| /register | RegisterComponent | guestGuard | yes |
| /lists | ListsComponent | authGuard | yes |
| /lists/:id | ListDetailComponent | authGuard | yes |
| ** | → /lists | - | - |

State management uses Angular Signals. signal() + asReadonly() + computed(). Consistent across all services.

Models: User, AuthResponse, LoginRequest, RegisterRequest, ListModel, TodoModel, CreateListRequest, UpdateListRequest, CreateTodoRequest, UpdateTodoRequest, InviteRequest, UserProfile — see models/index.ts for details.

---

## Backend Entities

Auth Service: User, DeviceSession
Todo Service: List, Todo, ListMember, OutboxEvent
User Service: UserProfile

Type and property details — refer to the source code. Writing it all here just creates dual maintenance, so it's omitted.

---

## Environment Variables

| Key | What | Dev value |
|-----|------|-----------|
| DB_CONNECTION_STRING | PG connection string | Host=localhost;Port=5432;Database=changelog;Username=postgres;Password=postgres |
| JWT_SIGNING_KEY | JWT signing key | your-secret-key-here-change-in-production |
| AzureWebJobsStorage | For Functions | UseDevelopmentStorage=true |
| REALTIME_PUBLISH_URL | Functions→Realtime | http://localhost:5002/api/publish |
| REALTIME_PUBLISH_SECRET | Internal auth | internal-publish-secret |

Add more as needed.

---

## Security stuff

- Passwords are bcrypt hashed. No plaintext storage.
- JWT is valid for 10 minutes. On the shorter side.
- SQL injection is prevented by EF Core's parameterized queries.
- Authorization is role-checked in each Controller.
- Inter-service communication is protected by a secret (REALTIME_PUBLISH_SECRET).
- FK CASCADE deletes prevent orphaned data.
- UNIQUE constraints prevent duplicate business keys.
- Other than that, keep OWASP Top 10 etc. in mind and handle as appropriate.

---

## Performance etc.

- API response: Aiming for under 200ms at P95 or so
- SignalR latency: Under 1 second
- Frontend initial load: Under 3 seconds
- N+1 avoided with Eager Loading (Include)
- Service independence: If one goes down, the others keep running
- Outbox pattern guarantees atomicity between transactions and event publishing
- SignalR auto-reconnect

## Scalability

- Microservices, so you can scale per service
- DBs are separated too, so schema changes stay localized
- Event-driven, so adding new consumers is easy
- Frontend lazy loading keeps bundle size in check

## Deployment

- DB migration → then service deploy, in that order
- When API response format changes, frontend and backend must be deployed simultaneously
- Version pinning via global.json / package.json / Docker image tags etc.
