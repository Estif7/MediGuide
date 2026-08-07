```markdown
# Medi-Guide Ethiopia

Full-stack medical support platform that connects patients with support agents for consultations, treatment planning, document sharing, and chat.

Built as a real-world project aligned with a .NET 10 + Angular 22 full-stack curriculum (Clean Architecture, EF Core, JWT Identity, standalone Angular with signals).

---

## Table of contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Tech stack](#tech-stack)
- [Solution structure](#solution-structure)
- [Domain model](#domain-model)
- [API overview](#api-overview)
- [Authentication & roles](#authentication--roles)
- [Angular frontend](#angular-frontend)
- [Getting started](#getting-started)
- [Configuration](#configuration)
- [Testing the API (Scalar)](#testing-the-api-scalar)
- [Project status](#project-status)
- [Roadmap](#roadmap)
- [Course module mapping](#course-module-mapping)

---

## Overview

Medi-Guide Ethiopia helps patients request medical support services (e.g. Mental Health, Pediatrics, Nutrition), communicate with agents, and share documents.

**Current focus:** core booking, chat, documents, and role-based access.  
**Deferred:** CHAPA payment integration, SignalR real-time chat, production deployment.

The initiative is independent of any healthcare institution and is designed around confidentiality, accessibility, and clear support workflows.

---

## Features

### Backend (API)

- Service categories (bilingual name fields EN / Amharic)
- Patient & agent management
- Bookings with response-time preference and status workflow
- Assign agent to booking
- Document upload / list / download (local storage)
- Chat messages per booking (REST)
- ASP.NET Core Identity + JWT
- Roles: **Patient**, **Agent**, **Admin**
- Seed data (categories, sample patient/agent, default admin)
- OpenAPI + **Scalar** UI for interactive testing

### Frontend (Angular)

- Login with JWT (stored + restored on refresh)
- Role-based redirects (Patient / Agent / Admin)
- Patient dashboard: list categories, create booking, list own bookings
- Booking detail: chat + document upload
- Agent dashboard: list bookings, open detail, reply in chat
- Functional HTTP interceptor for `Authorization: Bearer`

---

## Architecture

Clean / layered architecture:

```
┌─────────────────────────────────────────┐
│  medi-guide-web (Angular 22)            │
│  Standalone components, signals, RxJS   │
└─────────────────┬───────────────────────┘
                  │ HTTP + JWT
┌─────────────────▼───────────────────────┐
│  MediGuide.API                          │
│  Controllers, auth, CORS, Scalar        │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│  MediGuide.Application                  │
│  DTOs                                   │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│  MediGuide.Infrastructure               │
│  EF Core, Identity, seeding, files      │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│  MediGuide.Domain                       │
│  Entities, enums, ApplicationUser       │
└─────────────────────────────────────────┘
                  │
            PostgreSQL
```

**Dependency rule:** Domain has no outward dependencies. API depends on Application + Infrastructure.

---

## Tech stack

| Layer        | Technology                                      |
|-------------|--------------------------------------------------|
| Runtime     | .NET 10 / C# 14                                  |
| API         | ASP.NET Core Web API                             |
| ORM         | Entity Framework Core 10                         |
| Database    | PostgreSQL                                       |
| Auth        | ASP.NET Core Identity + JWT Bearer               |
| API docs    | Microsoft.AspNetCore.OpenApi + Scalar            |
| Frontend    | Angular 22 (standalone, signals, zoneless)       |
| UI styling  | SCSS (minimal for now)                           |
| HTTP client | Angular `HttpClient` + functional interceptor    |

---

## Solution structure

```
Medi-Guide/
├── MediGuide.Domain/
│   ├── Common/BaseEntity.cs
│   ├── Entities/          (Patient, Agent, Booking, …)
│   └── Enums/
├── MediGuide.Application/
│   └── DTOs/
├── MediGuide.Infrastructure/
│   └── Persistence/
│       ├── MediGuideDbContext.cs
│       ├── Configurations/
│       ├── DataSeeder.cs
│       └── Migrations/
├── MediGuide.API/
│   ├── Controllers/
│   ├── Program.cs
│   ├── appsettings.json
│   └── Uploads/           (gitignored – uploaded files)
├── medi-guide-web/        (Angular app)
│   └── src/app/
│       ├── core/          (models, services, interceptors)
│       └── features/      (auth, patient, agent, admin)
└── README.md
```

---

## Domain model

### Core entities

| Entity            | Purpose |
|-------------------|---------|
| `ServiceCategory` | Type of support + base price |
| `Patient`         | Person requesting support |
| `Agent`           | Support staff |
| `Booking`         | Request linking patient + category (+ optional agent) |
| `Payment`         | Reserved for CHAPA (status currently `PendingPayment`) |
| `Document`        | File attached to a booking |
| `ChatMessage`     | Message on a booking |
| `InternalNote`    | Agent-only notes (domain ready) |
| `ApplicationUser` | Identity user; optional links to `PatientId` / `AgentId` |

### Enums

- `BookingStatus`: PendingPayment, Paid, Assigned, InProgress, Completed, Cancelled  
- `ResponseTime`: Priority (24h), Expedited (2 days), Standard (5 days)  
- `PaymentStatus`: Pending, Successful, Failed, Refunded  

### Important relationships

- Patient 1 → N Bookings  
- ServiceCategory 1 → N Bookings  
- Agent 1 → N Bookings (optional until assigned)  
- Booking 1 → 1 Payment  
- Booking 1 → N Documents / ChatMessages  

Money uses `decimal`. IDs use `Guid`.

---

## API overview

Base URL (development): `http://localhost:5015/api`

| Area | Methods | Notes |
|------|---------|--------|
| **Auth** | `POST /auth/register-patient`, `POST /auth/login`, `POST /auth/register-agent` | Register-agent is Admin-only |
| **ServiceCategories** | `GET`, `GET/{id}`, `POST` | |
| **Patients** | `GET`, `GET/{id}`, `POST` | |
| **Agents** | `GET`, `GET/{id}`, `POST` | POST = Admin |
| **Bookings** | `GET`, `GET/{id}`, `POST`, `PATCH/{id}/assign`, `PATCH/{id}/status` | Create requires auth; assign = Admin/Agent |
| **Documents** | `GET/booking/{bookingId}`, `POST/booking/{bookingId}`, `GET/{id}/download` | Multipart upload |
| **ChatMessages** | `GET/booking/{bookingId}`, `POST/booking/{bookingId}` | |

Interactive docs: `http://localhost:5015/scalar`

---

## Authentication & roles

1. **Register patient** → creates `Patient` + Identity user + role `Patient` → returns JWT.  
2. **Login** → validates credentials → returns JWT + roles + optional `patientId` / `agentId`.  
3. **Register agent** (Admin only) → creates `Agent` + Identity user + role `Agent`.  

### Default seeded admin

| Field    | Value                |
|----------|----------------------|
| Email    | `admin@mediguide.et` |
| Password | `Admin123!`          |
| Role     | Admin                |

**JWT** is sent as:

```http
Authorization: Bearer <token>
```

Angular stores token + user snapshot in `localStorage` and restores them on refresh.

---

## Angular frontend

### Routes (high level)

| Path | Role | Purpose |
|------|------|---------|
| `/login` | Public | Sign in |
| `/patient` | Patient | Categories, create booking, list bookings |
| `/patient/bookings/:id` | Patient | Chat + documents |
| `/agent` | Agent | List bookings |
| `/agent/bookings/:id` | Agent | Same detail view (reply in chat) |
| `/admin` | Admin | Placeholder dashboard |

### Core Angular pieces

- `AuthService` – login, register, logout, signals for user/roles  
- `authInterceptor` – attaches Bearer token  
- `CategoryService`, `BookingService`, `ChatService`, `DocumentService`  
- Standalone components, signals, `@if` / `@for`, reactive login form  
- Zoneless change detection  

---

## Getting started

### Prerequisites

- .NET 10 SDK  
- Node.js 20+ and Angular CLI  
- PostgreSQL running locally  

### 1. Clone

```bash
git clone https://github.com/Estif7/MediGuide.git
cd MediGuide
```

### 2. Backend

```bash
# Update connection string in MediGuide.API/appsettings.json
# Example:
# "DefaultConnection": "Host=localhost;Port=5432;Database=MediGuideDb;Username=postgres;Password=YOUR_PASSWORD"

dotnet ef database update --project MediGuide.Infrastructure --startup-project MediGuide.API

cd MediGuide.API
dotnet run
```

API: `http://localhost:5015`  
Scalar: `http://localhost:5015/scalar`

### 3. Frontend

```bash
cd medi-guide-web
npm install
ng serve
```

App: `http://localhost:4200`

### 4. First login

- Admin: `admin@mediguide.et` / `Admin123!`  
- Or register a patient via Scalar (`POST /api/auth/register-patient`) then log in on the Angular app  

---

## Configuration

### API (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=MediGuideDb;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Key": "MediGuide-Super-Secret-Key-At-Least-32-Characters-Long!",
    "Issuer": "MediGuide",
    "Audience": "MediGuideApp",
    "ExpireMinutes": 60
  }
}
```

### Angular (`src/environments/environment.ts`)

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5015/api',
};
```

CORS is configured on the API to allow `http://localhost:4200`.

---

## Testing the API (Scalar)

1. Run the API.  
2. Open `http://localhost:5015/scalar`.  
3. Call `POST /api/auth/login` (or register).  
4. Copy the `token`.  
5. Paste it into Scalar’s **Bearer** authentication field.  
6. Call protected endpoints (e.g. create booking, upload document).  

---

## Project status

### Done

- Clean Architecture solution  
- Full domain model + EF Core migrations on PostgreSQL  
- REST APIs for categories, users, bookings, documents, chat  
- Identity + JWT + roles  
- Angular login and role-based shells  
- Patient booking flow + detail (chat & documents)  
- Agent list + chat replies  

### Deferred

- CHAPA payment  
- SignalR real-time messaging  
- Rich Admin UI  
- Amharic i18n  
- Automated tests (xUnit / Vitest / e2e)  
- Production hosting & hardening  

---

## Roadmap

1. Admin dashboard (users, categories, booking oversight)  
2. Human-readable status labels + shared layout  
3. SignalR for live chat  
4. CHAPA payment (initialize + verify webhook)  
5. Tests and CI  
6. Deploy (API + Angular + PostgreSQL)  

---

## Course module mapping

| Module | Topics reflected in this project |
|--------|----------------------------------|
| M1 | C# domain model, enums, decimal money, async |
| M4 | DI, configuration, middleware pipeline |
| M5 | EF Core Code-First, relationships, migrations, LINQ |
| M6 | REST controllers, DTOs, HTTP semantics |
| M10 | Identity, JWT, role-based authorization |
| M8–M9 | Angular standalone, signals, reactive forms |
| M11 | CORS, JWT interceptor, full-stack CRUD + upload + chat |

---

## License

Private / educational project unless otherwise stated by the repository owner.

---

## Author

Built as a hands-on full-stack implementation of the Medi-Guide Ethiopia concept using modern .NET and Angular practices.
```

---

### How to add it to the repo

1. Create/overwrite `README.md` at `C:\Users\Estifanose\Desktop\Qiyas\Medi-Guide\README.md` with the content above.  
2. Commit:

```powershell
git add README.md
git commit -m "Add detailed project README"
git push
```