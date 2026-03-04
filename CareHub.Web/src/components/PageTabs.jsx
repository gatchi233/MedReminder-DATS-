function PageTabs({ tabs, activeTab, onChange }) {
  return (
    <div className="page-tabs" role="tablist" aria-label="Page sub navigation">
      {tabs.map((tab) => (
        <button
          key={tab.key}
          type="button"
          role="tab"
          aria-selected={activeTab === tab.key}
          className={activeTab === tab.key ? "active" : ""}
          onClick={() => onChange(tab.key)}
        >
          {tab.label}
        </button>
      ))}
    </div>
  );
}

export default PageTabs;
