import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  ActivityIndicator,
  FlatList,
  SafeAreaView,
  Text,
  TextInput,
  TouchableOpacity,
  View
} from "react-native";
import { useAuth } from "../context/AuthContext";
import { createObservation, getObservations, getResidents } from "../services/apiClient";

export default function ObservationsScreen() {
  const { token, user } = useAuth();
  const [items, setItems] = useState([]);
  const [residents, setResidents] = useState([]);
  const [selectedResidentId, setSelectedResidentId] = useState("");
  const [selectedResidentName, setSelectedResidentName] = useState("");
  const [type, setType] = useState("");
  const [value, setValue] = useState("");
  const [saving, setSaving] = useState(false);
  const [loadingList, setLoadingList] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const canRecord = user?.role === "Nurse" || user?.role === "General CareStaff";
  const isObserver = user?.role === "Observer";

  const loadObservations = useCallback(async () => {
    try {
      setLoadingList(true);
      setError("");
      const data = await getObservations(token);
      setItems(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err?.message || "Failed to load observations.");
    } finally {
      setLoadingList(false);
    }
  }, [token]);

  const loadResidents = useCallback(async () => {
    if (!canRecord) {
      setResidents([]);
      return;
    }

    try {
      const data = await getResidents(token);
      const list = Array.isArray(data) ? data : [];
      setResidents(list);
      if (list.length > 0) {
        const first = list[0];
        setSelectedResidentId(String(first.id || first.Id || ""));
        setSelectedResidentName(
          `${first.residentFName || first.ResidentFName || ""} ${first.residentLName || first.ResidentLName || ""}`.trim()
        );
      }
    } catch (err) {
      setError(err?.message || "Failed to load residents for observation entry.");
    }
  }, [canRecord, token]);

  useEffect(() => {
    loadObservations();
    loadResidents();
  }, [loadObservations, loadResidents]);

  function pickResident(resident) {
    setSelectedResidentId(String(resident.id || resident.Id || ""));
    const fullName =
      `${resident.residentFName || resident.ResidentFName || ""} ${resident.residentLName || resident.ResidentLName || ""}`.trim();
    setSelectedResidentName(fullName);
  }

  async function onSaveObservation() {
    setSuccess("");
    setError("");

    if (!selectedResidentId) {
      setError("Choose a resident.");
      return;
    }

    if (!type.trim() || !value.trim()) {
      setError("Type and value are required.");
      return;
    }

    try {
      setSaving(true);
      await createObservation(
        {
          residentId: selectedResidentId,
          residentName: selectedResidentName,
          type: type.trim(),
          value: value.trim(),
          recordedBy: user?.displayName || user?.username || "mobile-user"
        },
        token
      );

      setType("");
      setValue("");
      setSuccess("Observation saved.");
      await loadObservations();
    } catch (err) {
      setError(err?.message || "Failed to save observation.");
    } finally {
      setSaving(false);
    }
  }

  const modeLabel = useMemo(() => {
    if (user?.role === "Observer") return "Read-only (own observations)";
    if (user?.role === "Nurse" || user?.role === "General CareStaff") return "Record + view target";
    return "Unavailable";
  }, [user]);

  return (
    <SafeAreaView style={{ flex: 1, padding: 16 }}>
      <Text style={{ fontSize: 20, marginBottom: 8 }}>Observations</Text>
      <Text style={{ marginBottom: 8 }}>Access mode: {modeLabel}</Text>
      {canRecord ? (
        <View
          style={{
            marginBottom: 12,
            padding: 12,
            borderWidth: 1,
            borderColor: "#ddd",
            borderRadius: 8
          }}
        >
          <Text style={{ fontWeight: "600", marginBottom: 8 }}>Record Observation</Text>
          <Text style={{ marginBottom: 4 }}>Resident</Text>
          <FlatList
            horizontal
            data={residents}
            keyExtractor={(item) => String(item.id || item.Id)}
            renderItem={({ item }) => {
              const id = String(item.id || item.Id);
              const fullName =
                `${item.residentFName || item.ResidentFName || ""} ${item.residentLName || item.ResidentLName || ""}`.trim();
              const selected = id === selectedResidentId;
              return (
                <TouchableOpacity
                  onPress={() => pickResident(item)}
                  style={{
                    marginRight: 8,
                    marginBottom: 8,
                    paddingHorizontal: 10,
                    paddingVertical: 8,
                    borderWidth: 1,
                    borderColor: selected ? "#2a7" : "#ccc",
                    backgroundColor: selected ? "#e7fff4" : "#fff",
                    borderRadius: 8
                  }}
                >
                  <Text>{fullName || "Unknown"}</Text>
                </TouchableOpacity>
              );
            }}
          />
          <TextInput
            placeholder="Type (e.g., BP, Temp, Note)"
            value={type}
            onChangeText={setType}
            style={{ borderWidth: 1, borderColor: "#ccc", marginBottom: 8, padding: 10, borderRadius: 6 }}
          />
          <TextInput
            placeholder="Value (e.g., 120/80)"
            value={value}
            onChangeText={setValue}
            style={{ borderWidth: 1, borderColor: "#ccc", marginBottom: 8, padding: 10, borderRadius: 6 }}
          />
          <TouchableOpacity
            onPress={onSaveObservation}
            disabled={saving}
            style={{
              backgroundColor: saving ? "#8fbca8" : "#2a7",
              paddingVertical: 10,
              borderRadius: 6,
              alignItems: "center"
            }}
          >
            <Text style={{ color: "white", fontWeight: "600" }}>
              {saving ? "Saving..." : "Save Observation"}
            </Text>
          </TouchableOpacity>
        </View>
      ) : null}
      {error ? <Text style={{ color: "red", marginBottom: 8 }}>{error}</Text> : null}
      {success ? <Text style={{ color: "#2a7", marginBottom: 8 }}>{success}</Text> : null}
      {isObserver ? <Text style={{ marginBottom: 8 }}>Observer can only view their own records.</Text> : null}
      {loadingList ? <ActivityIndicator style={{ marginBottom: 8 }} /> : null}
      <FlatList
        data={items}
        keyExtractor={(item) => String(item.id || item.Id)}
        renderItem={({ item }) => (
          <View style={{ paddingVertical: 10, borderBottomWidth: 1, borderBottomColor: "#eee" }}>
            <Text>
              {(item.type || item.Type || "").toString()}: {(item.value || item.Value || "").toString()}
            </Text>
            <Text style={{ color: "#666" }}>{(item.recordedAt || item.RecordedAt || "").toString()}</Text>
          </View>
        )}
      />
    </SafeAreaView>
  );
}
