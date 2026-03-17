import React from "react";
import { ActivityIndicator, View } from "react-native";
import { NavigationContainer } from "@react-navigation/native";
import { createNativeStackNavigator } from "@react-navigation/native-stack";
import { createBottomTabNavigator } from "@react-navigation/bottom-tabs";
import { AuthProvider, useAuth } from "./src/context/AuthContext";
import LoginScreen from "./src/screens/LoginScreen";
import DashboardScreen from "./src/screens/DashboardScreen";
import ResidentsScreen from "./src/screens/ResidentsScreen";
import ObservationsScreen from "./src/screens/ObservationsScreen";
import MedicationsScreen from "./src/screens/MedicationsScreen";

const Stack = createNativeStackNavigator();
const Tabs = createBottomTabNavigator();

function AppTabs() {
  const { user } = useAuth();
  const role = user?.role || "";

  const canSeeResidents = role === "Nurse" || role === "General CareStaff";
  const canSeeObservations =
    role === "Nurse" || role === "General CareStaff" || role === "Observer";
  const canSeeMedications = role === "Nurse" || role === "Observer";

  return (
    <Tabs.Navigator>
      <Tabs.Screen name="Dashboard" component={DashboardScreen} />
      {canSeeResidents ? (
        <Tabs.Screen name="Residents" component={ResidentsScreen} />
      ) : null}
      {canSeeObservations ? (
        <Tabs.Screen name="Observations" component={ObservationsScreen} />
      ) : null}
      {canSeeMedications ? (
        <Tabs.Screen name="Medications" component={MedicationsScreen} />
      ) : null}
    </Tabs.Navigator>
  );
}

function RootNavigator() {
  const { isAuthenticated, initializing } = useAuth();

  if (initializing) {
    return (
      <View style={{ flex: 1, alignItems: "center", justifyContent: "center" }}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  return (
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        {isAuthenticated ? (
          <Stack.Screen name="AppTabs" component={AppTabs} />
        ) : (
          <Stack.Screen name="Login" component={LoginScreen} />
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <RootNavigator />
    </AuthProvider>
  );
}
