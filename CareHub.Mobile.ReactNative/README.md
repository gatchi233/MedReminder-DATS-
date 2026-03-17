# CareHub Mobile React Native (Migration Start)

This folder is the migration starting point from MAUI mobile (`CareHub.Mobile`) to React Native.

## Current Scope Implemented

- Login screen and auth context
- API login (`/api/auth/login`) + profile check (`/api/auth/me`)
- Role-based mobile access enforcement:
  - `Admin`: blocked from mobile
  - `Nurse`: dashboard + residents + observations + medications
  - `General CareStaff`: dashboard + residents + observations
  - `Observer`: dashboard + observations + medications
- Initial data screens (read scaffolds):
  - Residents
  - Observations (including create flow for Nurse/General CareStaff)
  - Medications

## Notes

- This is a scaffold to begin migration, not a full feature parity build.
- Existing MAUI mobile code is left untouched for rollback/reference.
- API base URL is configurable in `src/services/apiClient.js`.
  - Android emulator default: `http://10.0.2.2:5001/api`
  - iOS/default fallback: `http://localhost:5001/api`
  - Optional override: `CAREHUB_API_BASE_URL`
- Login token + profile persistence is enabled via AsyncStorage.

## Run (after installing deps)

1. `npm install`
2. `npm run start`
3. `npm run android` or `npm run ios`

## Next Steps

1. Build full role-specific flows (CRUD for nurse, read-only for observer).
2. Add MAR nurse flows.
3. Add error boundaries, retry UX, and loading skeletons.
4. Add integration tests for role matrix access.
