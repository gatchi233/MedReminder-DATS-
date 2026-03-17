import React, { useState } from "react";
import { ActivityIndicator, Button, SafeAreaView, Text, TextInput, View } from "react-native";
import { useAuth } from "../context/AuthContext";

export default function LoginScreen() {
  const { login, error, loading } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  async function onLogin() {
    await login(username.trim(), password);
  }

  return (
    <SafeAreaView style={{ flex: 1, justifyContent: "center", padding: 20 }}>
      <Text style={{ fontSize: 24, marginBottom: 16 }}>CareHub Mobile Login</Text>
      <TextInput
        placeholder="Username"
        value={username}
        onChangeText={setUsername}
        style={{ borderWidth: 1, borderColor: "#ccc", marginBottom: 12, padding: 10 }}
        autoCapitalize="none"
      />
      <TextInput
        placeholder="Password"
        value={password}
        onChangeText={setPassword}
        secureTextEntry
        style={{ borderWidth: 1, borderColor: "#ccc", marginBottom: 12, padding: 10 }}
      />
      <Button title="Sign In" onPress={onLogin} />
      {loading ? <ActivityIndicator style={{ marginTop: 12 }} /> : null}
      {error ? <Text style={{ marginTop: 12, color: "red" }}>{error}</Text> : null}
      <View style={{ marginTop: 12 }}>
        <Text>Allowed roles on mobile: Nurse, General CareStaff, Observer.</Text>
      </View>
    </SafeAreaView>
  );
}
