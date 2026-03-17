import React, { useEffect, useState } from "react";
import { FlatList, SafeAreaView, Text, View } from "react-native";
import { useAuth } from "../context/AuthContext";
import { getResidents } from "../services/apiClient";

export default function ResidentsScreen() {
  const { token, user } = useAuth();
  const [items, setItems] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    async function load() {
      try {
        setError("");
        const data = await getResidents(token);
        setItems(Array.isArray(data) ? data : []);
      } catch (err) {
        setError(err?.message || "Failed to load residents.");
      }
    }
    load();
  }, [token]);

  return (
    <SafeAreaView style={{ flex: 1, padding: 16 }}>
      <Text style={{ fontSize: 20, marginBottom: 8 }}>Residents</Text>
      <Text style={{ marginBottom: 8 }}>
        Access mode: {user?.role === "Nurse" ? "CRUD target" : "Read-only target"}
      </Text>
      {error ? <Text style={{ color: "red", marginBottom: 8 }}>{error}</Text> : null}
      <FlatList
        data={items}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <View style={{ paddingVertical: 10, borderBottomWidth: 1, borderBottomColor: "#eee" }}>
            <Text>
              {item.ResidentFName} {item.ResidentLName}
            </Text>
            <Text style={{ color: "#666" }}>Room {item.roomNumber || "N/A"}</Text>
          </View>
        )}
      />
    </SafeAreaView>
  );
}
