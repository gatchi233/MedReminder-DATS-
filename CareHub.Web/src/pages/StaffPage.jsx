import { useState } from "react";
import PageTabs from "../components/PageTabs";

const STAFF_TABS = [
  { key: "directory", label: "Directory" },
  { key: "shifts", label: "Shifts" },
  { key: "assignments", label: "Assignments" }
];

function StaffPage({ loading, error, authRole, currentResident }) {
  const [activeTab, setActiveTab] = useState("directory");

  if (loading) {
    return (
      <section className="card">
        <p>Loading staff workspace...</p>
      </section>
    );
  }

  if (error) {
    return (
      <section className="card error">
        <p>{error}</p>
      </section>
    );
  }

  if (authRole === "Resident") {
    return (
      <section className="card">
        <h3>My Assigned Care Team</h3>
        <div className="list-row">
          <span>Primary Doctor / Nurse Practitioner</span>
          <small>{currentResident?.doctorName || "Not assigned"}</small>
        </div>
        <div className="list-row">
          <span>Contact</span>
          <small>{currentResident?.doctorContact || "Not available"}</small>
        </div>
      </section>
    );
  }

  return (
    <section className="page-shell">
      <PageTabs tabs={STAFF_TABS} activeTab={activeTab} onChange={setActiveTab} />

      {activeTab === "directory" && (
        <section className="staff-grid">
          <article className="card">
            <h3>Registered Nurses</h3>
            <p>Planned: contact cards, credentials, and active coverage windows.</p>
          </article>
          <article className="card">
            <h3>Care Aides</h3>
            <p>Planned: shift assignments, certifications, and handoff ownership.</p>
          </article>
          <article className="card">
            <h3>On-Call Contacts</h3>
            <p>Planned: emergency escalation chain and replacement pool.</p>
          </article>
        </section>
      )}

      {activeTab === "shifts" && (
        <section className="staff-grid">
          <article className="card">
            <h3>Morning Shift</h3>
            <p>Planned: med rounds, vitals checks, and breakfast observation tasks.</p>
          </article>
          <article className="card">
            <h3>Evening Shift</h3>
            <p>Planned: administration checks, follow-ups, and end-of-day logs.</p>
          </article>
          <article className="card">
            <h3>Night Shift</h3>
            <p>Planned: incident handling and overnight monitoring coverage.</p>
          </article>
        </section>
      )}

      {activeTab === "assignments" && (
        <section className="staff-grid">
          <article className="card">
            <h3>Medication Tasks</h3>
            <p>Planned: who is assigned to each medication route and pass window.</p>
          </article>
          <article className="card">
            <h3>Follow-Up Tasks</h3>
            <p>Planned: flagged observations and required follow-through owners.</p>
          </article>
          <article className="card">
            <h3>Escalation Tasks</h3>
            <p>Planned: unresolved alerts and SLA tracking by role.</p>
          </article>
        </section>
      )}
    </section>
  );
}

export default StaffPage;
