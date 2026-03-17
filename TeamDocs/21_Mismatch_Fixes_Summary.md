# CareHub Mismatch Fix Summary (21 Items)

This document records the 21 mismatches that were fixed against the project reference and team-aligned contracts.

## Fixed Mismatches (21)

### A) Role taxonomy and seed account alignment
1. Replaced the legacy API role constant `Staff` with the finalized role name `Nurse`, while keeping backward-compatible aliases so existing checks did not break immediately.
2. Added the finalized API role `General CareStaff` so role checks and user assignments can use the agreed team-wide naming.
3. Updated seeded auth username `staff1` to `nurse1` to match the planned account naming.
4. Updated seeded auth username `observer1` to `carestaff1` to match the planned account naming.
5. Updated seeded auth role values from `Staff` to `Nurse` so token claims and authorization logic align with the final role model.
6. Updated resident account role values from `Resident` to `Observer` (`resident1..resident16`) so observer-only behavior is enforced consistently.

### B) Web and Desktop role label alignment
7. Updated the Web `ROLE_SECTIONS` mapping to use finalized roles (`Admin`, `Nurse`, `General CareStaff`, `Observer`) so navigation visibility reflects the current access matrix.
8. Updated Web role-based read permission checks to the finalized role names, removing reliance on legacy labels.
9. Updated Web Staff page role-edit dropdown values so account edits use only the approved role vocabulary.
10. Updated Desktop Staff Management role options from mixed operational categories (`Admin/Care/Kitchen/Facilities`) to the actual system roles.
11. Updated Desktop staff-role defaults from `Care`/`Staff` to `General CareStaff` so new records and blank-state forms start with valid role values.

### C) API access and behavior contract fixes
12. Updated Residents read authorization to the finalized role permissions, including observer-scoped access behavior.
13. Updated Medications read and low-stock authorization to the finalized role permissions, including observer-scoped data behavior.
14. Updated Observations read/write authorization so clinical write actions align with `Nurse` and `General CareStaff` responsibilities.
15. Added the missing duplicate observations endpoint alias `GET /api/observations/byResident/{residentId}` to preserve compatibility with clients expecting that route.
16. Added the missing `GET /api/staff/{username}` endpoint to complete the documented staff endpoint set.
17. Restricted staff read endpoints (`GET /api/staff`, `GET /api/staff/{username}`) to Admin-only so staff directory data is not exposed to non-admin roles.
18. Fixed observations update behavior so `PUT /api/observations/{id}` no longer rewrites historical `RecordedAt` timestamps for existing records.

### D) Route and naming conventions
19. Converted API controller route attributes to explicit lowercase paths for `residents`, `medications`, `observations`, `staff`, `mar`, and `medicationorders` to match documented route conventions.
20. Updated Desktop remote API calls from PascalCase-style paths to lowercase endpoints (`api/mar`, `api/medicationorders`) so client and server contracts are consistent.

### E) MAR numeric contract alignment
21. Changed MAR dose and ledger quantity types to decimal across API/Desktop surfaces:
   - `MarEntry.DoseQuantity` -> `decimal`
   - `MedicationInventoryLedger.ChangeQty` -> `decimal`
   - Updated MAR DTO and report types to keep serialization/validation consistent.
   - Updated MAR controller stock-adjustment handling so decimal dose values are processed safely while inventory stock remains integer-based.
