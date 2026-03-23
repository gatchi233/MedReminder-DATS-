import React, { useEffect, useMemo, useState } from "react";
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
import { aiCareQuery, aiDetectTrends, aiShiftSummary, getResidents } from "../services/apiClient";

export default function AiScreen() {
  const { token, user } = useAuth();
  const [residents, setResidents] = useState([]);
  const [selectedResidentId, setSelectedResidentId] = useState("");
  const [query, setQuery] = useState("");
  const [responseText, setResponseText] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const canUseAi = useMemo(() => user?.role === "Nurse", [user]);

  useEffect(() => {
    async function loadResidents() {
      if (!canUseAi) return;
      try {
        const data = await getResidents(token);
        const list = Array.isArray(data) ? data : [];
        setResidents(list);
        if (list.length > 0) {
          setSelectedResidentId(String(list[0].id || list[0].Id));
        }
      } catch (err) {
        setError(err?.message || "Failed to load residents.");
      }
    }
    loadResidents();
  }, [canUseAi, token]);

  async function runShiftSummary() {
    if (!selectedResidentId) {
      setError("Choose a resident first.");
      return;
    }
    setLoading(true);
    setError("");
    try {
      const result = await aiShiftSummary(selectedResidentId, token);
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI shift summary failed.");
    } finally {
      setLoading(false);
    }
  }

  async function runTrendDetect() {
    if (!selectedResidentId) {
      setError("Choose a resident first.");
      return;
    }
    setLoading(true);
    setError("");
    try {
      const result = await aiDetectTrends(selectedResidentId, token);
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI trend detection failed.");
    } finally {
      setLoading(false);
    }
  }

  async function runCareQuery() {
    if (!query.trim()) {
      setError("Enter a care query.");
      return;
    }
    setLoading(true);
    setError("");
    try {
      const result = await aiCareQuery(query.trim(), selectedResidentId || null, token);
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI care query failed.");
    } finally {
      setLoading(false);
    }
  }

  if (!canUseAi) {
    return (
      <SafeAreaView style={{ flex: 1, padding: 16 }}>
        <Text style={{ fontSize: 20, marginBottom: 8 }}>AI Assistant</Text>
        <Text>AI tools are currently available on mobile for Nurse role only.</Text>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={{ flex: 1, padding: 16 }}>
      <Text style={{ fontSize: 20, marginBottom: 8 }}>AI Assistant</Text>
      <Text style={{ marginBottom: 8 }}>Run quick resident summaries and care queries.</Text>

      <Text style={{ marginBottom: 4 }}>Resident Context</Text>
      <FlatList
        horizontal
        data={residents}
        keyExtractor={(item) => String(item.id || item.Id)}
        renderItem={({ item }) => {
          const id = String(item.id || item.Id);
          const selected = id === selectedResidentId;
          const name = `${item.residentFName || item.ResidentFName || ""} ${item.residentLName || item.ResidentLName || ""}`.trim();
          return (
            <TouchableOpacity
              onPress={() => setSelectedResidentId(id)}
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
              <Text>{name || "Resident"}</Text>
            </TouchableOpacity>
          );
        }}
      />

      <View style={{ flexDirection: "row", marginBottom: 8 }}>
        <TouchableOpacity onPress={runShiftSummary} style={{ marginRight: 12 }}>
          <Text style={{ color: "#2a7" }}>Shift Summary</Text>
        </TouchableOpacity>
        <TouchableOpacity onPress={runTrendDetect}>
          <Text style={{ color: "#2a7" }}>Detect Trends</Text>
        </TouchableOpacity>
      </View>

      <TextInput
        value={query}
        onChangeText={setQuery}
        placeholder="Ask a care question..."
        style={{ borderWidth: 1, borderColor: "#ccc", marginBottom: 8, padding: 10, borderRadius: 6 }}
      />
      <TouchableOpacity
        onPress={runCareQuery}
        style={{ backgroundColor: "#2a7", paddingVertical: 10, borderRadius: 6, alignItems: "center", marginBottom: 10 }}
      >
        <Text style={{ color: "white", fontWeight: "600" }}>Run Care Query</Text>
      </TouchableOpacity>

      {loading ? <ActivityIndicator style={{ marginBottom: 8 }} /> : null}
      {error ? <Text style={{ color: "red", marginBottom: 8 }}>{error}</Text> : null}

      <View style={{ borderWidth: 1, borderColor: "#ddd", borderRadius: 8, padding: 10 }}>
        <Text style={{ fontWeight: "600", marginBottom: 6 }}>AI Response</Text>
        <Text>{responseText || "No response yet."}</Text>
      </View>
    </SafeAreaView>
  );
}
