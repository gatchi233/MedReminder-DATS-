import { useEffect, useMemo, useState } from "react";
import { API_BASE, api } from "./api";

const SECTIONS = ["Dashboard", "Residents", "Medications", "Observations", "Staff"];

function App() {
  const [activeSection, setActiveSection] = useState("Dashboard");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [residents, setResidents] = useState([]);
  const [medications, setMedications] = useState([]);
  const [observations, setObservations] = useState([]);

  useEffect(() => {
    async function loadDashboard() {
      try {
        setLoading(true);
        setError("");
        const [resData, medData, obsData] = await Promise.all([
          api.get("/residents"),
          api.get("/medications"),
          api.get("/observations")
        ]);
        setResidents(Array.isArray(resData) ? resData : []);
        setMedications(Array.isArray(medData) ? medData : []);
        setObservations(Array.isArray(obsData) ? obsData : []);
      } catch (err) {
        setError(`Failed to load API data from ${API_BASE}. ${err.message}`);
      } finally {
        setLoading(false);
      }
    }

    loadDashboard();
  }, []);

  const lowStock = useMemo(() => {
    return medications.filter((m) => {
      const unassigned =
        !m.residentId || m.residentId === "00000000-0000-0000-0000-000000000000";
      return unassigned && Number(m.stockQuantity) <= Number(m.reorderLevel);
    });
  }, [medications]);

  function renderSectionCard() {
    if (activeSection === "Residents") {
      return (
        <section className="card">
          <h3>Residents</h3>
          {residents.length === 0 && <p>No residents found.</p>}
          {residents.map((resident) => {
            const name =
              resident.fullName ||
              `${resident.firstName || ""} ${resident.lastName || ""}`.trim() ||
              resident.name ||
              "Unnamed resident";
            return (
              <div className="list-row" key={resident.id}>
                <span>{name}</span>
                <small>Room {resident.roomNumber || resident.room || "N/A"}</small>
              </div>
            );
          })}
        </section>
      );
    }

    if (activeSection === "Medications") {
      return (
        <section className="card">
          <h3>Medications</h3>
          {medications.length === 0 && <p>No medications found.</p>}
          {medications.map((med) => (
            <div className="list-row" key={med.id}>
              <span>{med.medName || med.name || "Unnamed medication"}</span>
              <small>
                {med.stockQuantity ?? 0} in stock
                {med.reorderLevel != null ? ` | Reorder at ${med.reorderLevel}` : ""}
              </small>
            </div>
          ))}
        </section>
      );
    }

    if (activeSection === "Observations") {
      return (
        <section className="card">
          <h3>Observations</h3>
          {observations.length === 0 && <p>No observations found.</p>}
          {observations.map((obs) => (
            <div className="list-row" key={obs.id}>
              <span>{obs.summary || obs.note || "Observation entry"}</span>
              <small>{obs.observedAt || obs.createdAt || "No timestamp"}</small>
            </div>
          ))}
        </section>
      );
    }

    return (
      <section className="card">
        <h3>Staff</h3>
        <p>This section is planned next. Dashboard data is already live.</p>
      </section>
    );
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <h1>CareHub</h1>
        <p>Retirement medication operations</p>
        <nav>
          {SECTIONS.map((section) => (
            <button
              key={section}
              className={activeSection === section ? "active" : ""}
              onClick={() => setActiveSection(section)}
            >
              {section}
            </button>
          ))}
        </nav>
      </aside>

      <main className="content">
        <header className="topbar">
          <h2>{activeSection}</h2>
          <button onClick={() => window.location.reload()}>Refresh</button>
        </header>

        {activeSection !== "Dashboard" && !loading && !error && renderSectionCard()}
        {activeSection !== "Dashboard" && loading && (
          <section className="card">
            <p>Loading {activeSection.toLowerCase()}...</p>
          </section>
        )}
        {activeSection !== "Dashboard" && error && (
          <section className="card error">
            <p>{error}</p>
          </section>
        )}

        {activeSection === "Dashboard" && (
          <section className="dashboard-grid">
            {loading && <article className="card">Loading dashboard...</article>}
            {error && <article className="card error">{error}</article>}

            {!loading && !error && (
              <>
                <article className="card metric">
                  <h3>Total Residents</h3>
                  <strong>{residents.length}</strong>
                </article>
                <article className="card metric">
                  <h3>Total Medications</h3>
                  <strong>{medications.length}</strong>
                </article>
                <article className="card metric">
                  <h3>Observations Logged</h3>
                  <strong>{observations.length}</strong>
                </article>
                <article className="card metric warning">
                  <h3>Low Stock Alerts</h3>
                  <strong>{lowStock.length}</strong>
                </article>
                <article className="card">
                  <h3>Inventory Reorder List</h3>
                  {lowStock.length === 0 && <p>No low-stock items.</p>}
                  {lowStock.slice(0, 6).map((m) => (
                    <div className="list-row" key={m.id}>
                      <span>{m.medName}</span>
                      <small>
                        {m.stockQuantity} / {m.reorderLevel}
                      </small>
                    </div>
                  ))}
                </article>
              </>
            )}
          </section>
        )}
      </main>
    </div>
  );
}

export default App;
