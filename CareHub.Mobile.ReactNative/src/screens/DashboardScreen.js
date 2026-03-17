import React from "react";
import { Button, SafeAreaView, Text, View } from "react-native";
import { useAuth } from "../context/AuthContext";

export default function DashboardScreen() {
  const { user, logout } = useAuth();

  return (
    <SafeAreaView style={{ flex: 1, padding: 16 }}>
      <Text style={{ fontSize: 20, marginBottom: 8 }}>Dashboard</Text>
      <Text style={{ marginBottom: 4 }}>User: {user?.username}</Text>
      <Text style={{ marginBottom: 12 }}>Role: {user?.role}</Text>
      <Text>
        Role-aware mobile dashboard scaffold is active. Next step is wiring role-specific
        summary widgets and live metrics.
      </Text>
      <View style={{ marginTop: 16 }}>
        <Button title="Logout" onPress={logout} />
      </View>
    </SafeAreaView>
  );
}
