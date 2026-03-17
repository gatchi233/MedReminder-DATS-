# CareHub Project Reference Update

Last updated: 2026-03-17

This file is an updated implementation snapshot of the current repository, showing what is already done and what is still pending.

## 1) Current Status Overview

- API is no longer open-by-default; JWT auth and role-based authorization are active.
- Role naming has been aligned to the agreed model in most key areas:
  - `Admin`
  - `Nurse`
  - `General CareStaff`
  - `Observer`
- Web and Desktop role labels were aligned with the finalized taxonomy.
- Core documented endpoint mismatches (staff lookup and observation route alias) were added.
- Route conventions were normalized to explicit lowercase paths for core controllers.
- MAR quantity contract was updated to decimal types in API/Desktop models.

## 2) Done (Implemented)

### Authentication and authorization
- `POST /api/auth/login` is implemented and returns a bearer token payload.
- `GET /api/auth/me` is implemented and returns current token identity details.
- API JWT middleware is configured and enabled.
- Controller-level `[Authorize]` and role restrictions are active.

### Core API endpoints
- Residents API (`/api/residents`) is implemented with authorization.
- Medications API (`/api/medications`) including `lowstock` and `adjustStock` is implemented with authorization.
- Observations API (`/api/observations`) is implemented with:
  - `/by-resident/{residentId}`
  - `/byResident/{residentId}` (compatibility alias)
- MAR API (`/api/mar`) is implemented with:
  - list/filter
  - create with idempotency behavior
  - void endpoint
  - report and admin utility endpoints
- Staff API (`/api/staff`) includes:
  - list
  - get by username
  - create/update/delete

### Role and contract alignment fixes
- Seed account naming was aligned (`nurse1`, `carestaff1`, resident accounts as `Observer`).
- Web role mappings and permission checks were aligned to finalized names.
- Desktop staff role options/defaults were aligned to finalized names.
- Staff reads were restricted to admin-only.
- Observation update behavior now preserves existing `RecordedAt`.
- Explicit lowercase route attributes are in place for:
  - residents
  - medications
  - observations
  - staff
  - mar
  - medicationorders

### Desktop and Mobile auth integration
- Desktop API login path is integrated and token-aware.
- Desktop blocks Observer login for desktop usage.
- Mobile includes token handler and API login wiring.

### MAR numeric type alignment
- `DoseQuantity` changed to `decimal` in API and Desktop MAR models.
- `MedicationInventoryLedger.ChangeQty` changed to `decimal`.
- DTO/report surfaces were updated for decimal dose values.

## 3) Not Done / Still Pending

### A) Full role-constant cleanup in all API controllers
- Some controllers still use compatibility aliases (`Roles.Staff`, `Roles.Resident`) instead of direct finalized names.
- This works functionally due aliases, but should be fully migrated for clarity and long-term maintainability.

### B) Desktop staff management backend integration
- Desktop still registers local JSON staff service (`IStaffService -> StaffJsonService`) instead of API-backed staff management.
- This means staff CRUD in Desktop is not yet fully centralized through the API contract.

### C) Migration consistency after MAR decimal change
- Entity types were updated to decimal, but migration snapshot still shows integer columns for `DoseQuantity`/`ChangeQty`.
- A new EF migration is needed to keep database schema and model snapshot in sync.

### D) End-to-end verification and regression checks
- Full cross-platform validation (Desktop/Web/Mobile) after role/auth and MAR type changes is still required.
- Automated test coverage for role permissions and auth flows remains limited.

### E) Reference document refresh
- Original master reference text should be updated to match the now-implemented auth/RBAC reality and the latest endpoint set.

## 4) Recommended Next Steps (Order)

1. Replace alias usage (`Roles.Staff`, `Roles.Resident`) with explicit finalized roles in remaining controllers.
2. Add and apply EF migration for MAR decimal schema updates.
3. Move Desktop staff management from local JSON service to API-backed staff service.
4. Run integration checks for all roles across Web/Desktop/Mobile against one API instance.
5. Publish an updated “single source of truth” reference reflecting current implemented behavior.

