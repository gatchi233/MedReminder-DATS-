import React, { useEffect, useMemo, useState } from "react";
import { FlatList, SafeAreaView, Text, View } from "react-native";
import { useAuth } from "../context/AuthContext";
import { getMedications } from "../services/apiClient";

export default function MedicationsScreen() {
  const { token, user } = useAuth();
  const [items, setItems] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    async function load() {
      try {
        setError("");
        const data = await getMedications(token);
        setItems(Array.isArray(data) ? data : []);
      } catch (err) {
        setError(err?.message || "Failed to load medications.");
      }
    }
    load();
  }, [token]);

  const modeLabel = useMemo(() => {
    if (user?.role === "Observer") return "Read-only (own medications)";
    if (user?.role === "Nurse") return "CRUD target";
    return "Unavailable";
  }, [user]);

  return (
    <SafeAreaView style={{ flex: 1, padding: 16 }}>
      <Text style={{ fontSize: 20, marginBottom: 8 }}>Medications</Text>
      <Text style={{ marginBottom: 8 }}>Access mode: {modeLabel}</Text>
      {error ? <Text style={{ color: "red", marginBottom: 8 }}>{error}</Text> : null}
      <FlatList
        data={items}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <View style={{ paddingVertical: 10, borderBottomWidth: 1, borderBottomColor: "#eee" }}>
            <Text>{item.medName || "Unnamed medication"}</Text>
            <Text style={{ color: "#666" }}>{item.dosage || "No dosage"}</Text>
          </View>
        )}
      />
    </SafeAreaView>
  );
}
