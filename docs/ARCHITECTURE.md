# MVEC Backend — Architecture Standards & Rules

> **Status:** Mandatory. Every new service, and every new method in an existing service,
> MUST follow these rules. The **Identity** service (`Mvec.Identity.Api`) is the reference
> implementation — when in doubt, copy its structure.

---

## 0. Database-first (the database is the source of truth)

**Every service uses the database-first approach.** The SQL database schema (hand-written DDL,
owned by the team) is the single source of truth. EF Core **maps to existing tables** — it does
**not** generate, own, or alter them.

Rules:
- **No code-first migrations for business tables.** Do not run `dotnet ef migrations` to create or
  alter domain tables. Map entities to the existing schema instead.
- Entities are mapped explicitly to their real schema, table, and column names + SQL types via
  `IEntityTypeConfiguration<T>` (`ToTable("Users", "idn")`, `HasColumnName(...)`, `HasColumnType(...)`).
- Primary keys are **database-generated** (`IDENTITY`) → `ValueGeneratedOnAdd()`. The app never sets them.
- The project keeps **no migrations** for business tables — never run `dotnet ef migrations`. (Avoid
  `ToTable(t => t.ExcludeFromMigrations())`: it also disables `EnsureCreated`, which tests rely on.)
- The startup seeder does **not** call `MigrateAsync`; it only inserts seed/reference data (idempotent).
- The authoritative DDL scripts are kept in the repo under each service's `Database/` folder.

### 0.1 Missing table or column → STOP and inform (mandatory workflow)
If a feature needs a table or column that does not exist in the database:
1. **Do not invent or auto-create schema.** Stop implementation of the affected part.
2. **Inform the requester** with the exact DDL needed — table/column names, SQL types, nullability,
   keys, foreign keys, indexes.
3. Wait for the table/column to be **created in the database**.
4. **Then continue** — map the new entity/column and finish the feature.

---

## 1. Layered architecture (per service)

Each service is one project (`Mvec.<Service>.Api`) with four layers as folders. Dependencies
point **inward only**: `Api → Application → Domain`, and `Infrastructure → Application/Domain`.
Domain depends on nothing.

| Layer | Folder | Contains | May depend on |
|---|---|---|---|
| **Domain** | `Domain/` | Aggregate roots, entities, enums, value objects, domain logic/invariants | Nothing (POCOs mapped to DB tables; shared base optional) |
| **Application** | `Application/` | Use-case services + their interfaces, DTOs/contracts, validators, **abstractions (ports)**, options | Domain |
| **Infrastructure** | `Infrastructure/` | `DbContext`, EF configurations, **repository implementations**, security, messaging, seeding, external integrations | Application, Domain |
| **Api** | `Api/` | Thin controllers, Result→HTTP mapping | Application |

**Rule:** The Api layer NEVER references `Infrastructure` types or `Application.Services` concretions —
only `Application.Abstractions` interfaces.

---

## 2. Repository pattern + Unit of Work — for ALL database operations

This is the central rule. **All** persistence access goes through repositories.

### 2.1 Rules
- **All EF/LINQ queries live ONLY in repository implementations** (`Infrastructure/Repositories`).
  Application services (and the seeder) must contain **zero** `FirstOrDefaultAsync`, `AnyAsync`,
  `Where`, `Include`, `ToListAsync`, `CountAsync`, `DbSet`, or `DbContext` usage.
- Application services depend on **repository interfaces** + **`IUnitOfWork`**, never on `DbContext`.
- **One repository per aggregate root.** A pragmatic second repository is allowed when an entity
  is queried outside its aggregate's identity (e.g. `RefreshToken` looked up by hash without a user).
- **`SaveChanges` is called only via `IUnitOfWork`**. All repositories in a request share the same
  scoped `DbContext`, which is the transaction boundary. The `DbContext` implements `IUnitOfWork`.
- **Repositories return domain entities** (or `PagedResult<TEntity>`), never DTOs. Mapping to DTOs
  happens in the application layer.

### 2.2 Building blocks (shared, reusable)
Defined once in `Mvec.BuildingBlocks/Persistence`:
- `IRepository<T>` — `GetByIdAsync`, `AnyAsync`, `Add`, `Remove`.
- `EfRepository<T>` — abstract EF base; concrete repos derive and add query methods.
- `IUnitOfWork` — `SaveChangesAsync`.

Per service:
- Interfaces in `Application/Abstractions/` (e.g. `IUserRepository : IRepository<User>`).
- Implementations in `Infrastructure/Repositories/` (e.g. `UserRepository(DbContext) : EfRepository<User>`).
- Register in the service's Infrastructure DI:
  ```csharp
  services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<XxxDbContext>());
  services.AddScoped<IUserRepository, UserRepository>();
  ```

### 2.3 What the repository pattern deliberately does NOT wrap
These are **allowed** to use the `DbContext` directly because they are not data queries:
- **Schema** — applied out-of-band (database-first, §0). The seeder does **not** migrate; it only
  inserts seed data. No `MigrateAsync` for business schema.
- **Outbox publishing** — MassTransit writes outbox tables inside `SaveChanges` (framework-managed).
  Those tables must already exist in the DB (§8).
- **Aggregate child collections** — in-memory `List<>` on an aggregate root (e.g. `User.IssueRefreshToken`)
  are domain operations, not DB operations. EF tracks them on save.

### 2.4 Key generation & the "explicit Add" rule
- **Database-first identity keys** (`BIGINT IDENTITY`, mapped `ValueGeneratedOnAdd`) default to `0`
  for new entities, so EF correctly tracks them as inserts. Still call `repo.Add(entity)` explicitly.
- **Client-generated keys** (a `Guid.CreateVersion7()` base, if ever used): a new **child** entity
  attached to an already-tracked aggregate MUST be `repo.Add(child)`-ed explicitly, or EF mis-detects
  the non-empty key as "existing" and throws `DbUpdateConcurrencyException`.

---

## 3. Application services + interfaces

- Every application/use-case service has an **interface** in `Application/Abstractions/`
  (e.g. `IAuthService`, `IUserService`). Controllers depend on the interface.
- Register as `services.AddScoped<IAuthService, AuthService>();`.
- Services orchestrate; they do not contain query logic (see §2) or HTTP concerns.
- Private helpers stay off the interface.

---

## 4. Contracts, DTOs & mapping

- Request/response types are `record`s in `Application/Contracts/`.
- **Never expose domain entities over the API.** Map Domain → DTO in the application layer
  (e.g. `user.ToDto()`).
- Inbound DTOs are validated (see §6).

---

## 5. Result pattern (no throwing for expected failures)

- Application methods return `Result` / `Result<T>` (`BuildingBlocks.Common`) with an `Error`
  (`Code` + `Message`) for **expected** business failures (not-found, conflict, validation, etc.).
- Exceptions are reserved for **unexpected** faults (caught by the global handler → ProblemDetails).
- Controllers map `Result` → HTTP via the `ApiResults` helper (error code → status code). Failures
  are returned as RFC 7807 `ProblemDetails`.

---

## 6. Validation

- One FluentValidation `AbstractValidator<T>` per inbound request DTO, in `Application/Validators/`.
- Registered via `AddValidatorsFromAssemblyContaining<...>()` + `AddFluentValidationAutoValidation()`
  so binding auto-validates.

---

## 7. Configuration & Options

- Strongly-typed options classes bound from named config sections (`JwtOptions`, `SocialAuthOptions`, …),
  registered with `services.Configure<T>(config.GetSection(T.SectionName))`.
- **Connection strings come from `appsettings.json`** via `GetConnectionString("<Service>Db")`.
  Never hard-code them in code.
- Per-environment overrides via `appsettings.{Environment}.json`, user-secrets, or env vars
  (`ConnectionStrings__XxxDb`). Production secrets come from Key Vault.

---

## 8. Messaging & integration events

- Cross-service communication is via **integration events**, contracts in `Mvec.Contracts`.
- Publish through the **transactional outbox**: depend on the `IEventPublisher` abstraction
  (over MassTransit `IPublishEndpoint` + EF outbox). Events are committed **atomically** with the
  domain change in the same `SaveChanges`.
- The owning `DbContext` registers the outbox entities (`AddInboxStateEntity`,
  `AddOutboxMessageEntity`, `AddOutboxStateEntity`).
- **Database-first:** the outbox/inbox tables must already exist in the database (created via the
  team's SQL scripts), since EF does not migrate them. If they're missing, request their creation (§0).

---

## 9. Persistence / EF conventions (database-first)

- **Map, don't migrate** (§0). One `IEntityTypeConfiguration<T>` per entity, mapping to the real
  table: `ToTable("<Table>", "<schema>")`, `HasColumnName`, `HasColumnType`, lengths, nullability, keys.
- `DbContext` implements `IUnitOfWork`; applies all configurations in `OnModelCreating`.
- Keys are DB-generated (`IDENTITY`) → `.ValueGeneratedOnAdd()`; the app never assigns them.
- Persist enums as the DB column type (typically a check-constrained `VARCHAR`) via `HasConversion<string>()`.
- Map DB audit columns to entity properties (`CreatedUtc`, `UpdatedUtc`, …) and set them in code on
  insert/update (DB defaults may also exist as a backstop).
- Keep **no migrations** for business tables; never auto-generate DDL. (Don't use
  `ExcludeFromMigrations()` — it also disables `EnsureCreated`, which tests use to build the schema.)
- Reference/lookup tables (e.g. `Roles`) are **seeded idempotently** by the app, not created by it.
- Keep the authoritative DDL under the service's `Database/` folder.

---

## 10. Security standards

- **Custom JWTs** (not cookie auth): access token ~15 min; refresh token random 256-bit, **rotating**,
  ~7-day expiry, **store only the hash**, revoke-on-use.
- Passwords hashed with **PBKDF2** (HMAC-SHA256, per-user salt, constant-time verify).
- High-entropy secrets (refresh tokens, OTP codes) hashed with **SHA-256** before storage.
- Admin 2FA via **TOTP** (RFC 6238). Admins are **seeded**, never self-registered.
- **Never store raw** tokens, codes, or secrets. Always compare hashes with fixed-time equality.

---

## 11. Cross-cutting (shared via `Mvec.BuildingBlocks`)

- `AddMvecDefaults(config)` / `UseMvecPipeline()` compose every service: controllers (+ string enum
  JSON), Swagger (with Bearer), ProblemDetails, health checks, **CORS** (configurable origins),
  JWT auth, role authorization policies.
- Middleware: correlation-id (`X-Correlation-Id`) and exception-handling → ProblemDetails.
- CORS origins come from `Cors:AllowedOrigins` (config array); `AllowCredentials` is on for the
  refresh cookie.
- `MvecRoles` constants (`Buyer`, `Vendor`, `Admin`) back `[Authorize(Policy = ...)]`.
- **`Mvec.BuildingBlocks` is a library** (`Microsoft.NET.Sdk` + `FrameworkReference
  Microsoft.AspNetCore.App`), **not** the Web SDK.

---

## 12. Testing

- xUnit + FluentAssertions. Service/unit tests use the EF **in-memory** provider via a test harness
  that wires the real repositories + `IUnitOfWork`.
- Cover each feature's Definition of Done (happy path + key failure paths).
- Keep tests green; if a behavior is intentionally changed, update the test to match (don't leave red).

---

## Checklist — adding a new service

1. **Confirm the DB schema exists** for the service (tables/columns). If anything is missing,
   request the DDL first (§0) before coding.
2. Create `Mvec.<Service>.Api` with `Domain/ Application/ Infrastructure/ Api/` folders.
3. Domain: aggregate root(s) as POCOs mapped to the existing tables (DB-generated keys), enums, invariants.
4. Application: `Contracts/` DTOs, `Abstractions/` (service interfaces + repository interfaces),
   `Services/`, `Validators/`, `Options/`, a `AddXxxApplication()` DI extension.
5. Infrastructure: `DbContext : IUnitOfWork`, `Configurations/` (map to existing tables, database-first —
   **no migrations**), `Repositories/`, messaging/outbox, seeding (insert reference/seed data only),
   `AddXxxInfrastructure(config)` DI extension.
6. Api: thin controllers depending on application interfaces; map `Result` via `ApiResults`.
7. `Program.cs`: `AddMvecDefaults` → `AddXxxInfrastructure` → `AddXxxApplication`; `UseMvecPipeline`.
8. `appsettings.json`: `ConnectionStrings:<Service>Db`, options sections, `Cors:AllowedOrigins`.
9. Keep the authoritative DDL under `Database/`.
10. Tests: xUnit project covering the DoD.

## Checklist — adding a method

- [ ] Required tables/columns exist in the DB — if not, requested the DDL first (§0).
- [ ] No EF/LINQ in the service — every query is a repository method.
- [ ] New repository query added to the **interface** + implementation.
- [ ] Writes go through repositories; commit via `IUnitOfWork.SaveChangesAsync`.
- [ ] New entities/children explicitly `Add`-ed.
- [ ] Returns `Result`/`Result<T>`; maps Domain → DTO.
- [ ] Inbound DTO has a validator.
- [ ] Exposed on the service **interface** if public.
- [ ] Test added/updated.
