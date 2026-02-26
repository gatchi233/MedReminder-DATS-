# CareHub Weekly Sprint Plan

---

## Week 1 – M0 + M1 (Foundation + Auth)

### Team A

**Backend & Infra**

- [ ] Setup repo structure
- [ ] Docker compose for PostgreSQL
- [ ] Create ASP.NET Core API project
- [ ] Configure EF Core
- [ ] Create initial migration
- [ ] Seed initial data (Staff, sample Residents)

**Authentication**

- [ ] Staff table with roles (Admin/Nurse/CareAide)
- [ ] Implement JWT login endpoint
- [ ] Role-based authorization policies
- [ ] Login audit logging

**Deliverables**

- [ ] Swagger working
- [ ] Login endpoint working
- [ ] Role protection enforced

### Team B

**Web**

- [ ] React project setup
- [ ] Routing setup
- [ ] Login page UI
- [ ] Connect login to API
- [ ] Protected routes

**Mobile (Member 2 supports Team A)**

- [ ] React Native project setup
- [ ] Login screen
- [ ] Navigation structure

### End of Week 1 Goal

- [ ] Both Web and Mobile login successfully
- [ ] Roles enforced
- [ ] SRD v1 + SDD v1 drafted

---

## Week 2 – M2 Core CRUD

### Team A

**Residents**

- [ ] `GET /residents`
- [ ] `POST /residents`
- [ ] `PUT /residents/{id}`
- [ ] `DELETE /residents/{id}`

**Medications**

- [ ] CRUD endpoints
- [ ] Schedule endpoints
- [ ] Audit logging for create/update

### Team B

**Web**

- [ ] Residents list page
- [ ] Residents create/edit form
- [ ] Medications page
- [ ] Schedule management UI

**Mobile**

- [ ] Residents list
- [ ] Resident detail (read-only)

### End of Week 2 Goal

- [ ] Admin creates resident + schedule on Web
- [ ] Mobile displays residents + meds

---

## Week 3 – M3 MAR Workflow (Core Healthcare Feature)

### Team A

- [ ] Generate "Today Tasks" from schedules
- [ ] MAR action endpoint
- [ ] MAR history endpoint
- [ ] Audit logs for MAR actions

### Team B

**Mobile**

- [ ] Today screen (due meds list)
- [ ] MAR action UI (Given/Refused/Held/Missed)
- [ ] Notes input

**Web**

- [ ] MAR history viewer page
- [ ] Filters by date/resident

### End of Week 3 Goal

- [ ] End-to-end MAR workflow fully functional (online)

---

## Week 4 – M4 Offline + Observations + AI

### Team A

**Offline Server Side**

- [ ] `ProcessedActions` table (idempotency)
- [ ] `POST /sync/actions`
- [ ] `GET /sync/changes?since=timestamp`

**Observations**

- [ ] `POST /observations`
- [ ] `GET /observations`

**AI**

- [ ] `POST /ai/shift-summary`
- [ ] `POST /ai/medication-explain`
- [ ] Add safety disclaimer

### Team B

**Mobile**

- [ ] SQLite local storage
- [ ] Outbox queue table
- [ ] Offline detection
- [ ] Sync trigger
- [ ] Observation screen

**Web**

- [ ] Sync indicator UI
- [ ] AI dashboard panel
- [ ] Display AI results

### End of Week 4 Goal

- [ ] Airplane mode → record MAR + Observation → reconnect → sync
- [ ] AI summary working

---

## Week 5 – M5 Buffer + Polish (NO NEW FEATURES)

### Team A

- [ ] Fix bugs only
- [ ] Improve error handling
- [ ] Improve sync stability
- [ ] Performance cleanup
- [ ] Finalize documentation diagrams

### Team B

- [ ] UI polish
- [ ] Loading states
- [ ] Empty states
- [ ] Accessibility improvements
- [ ] Final styling adjustments
