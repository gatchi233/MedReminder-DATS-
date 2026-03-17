import { Platform } from "react-native";

const API_BASE_BY_PLATFORM = {
  android: "http://10.0.2.2:5001/api",
  ios: "http://localhost:5001/api",
  default: "http://localhost:5001/api"
};

let apiBaseUrl =
  (typeof process !== "undefined" && process?.env?.CAREHUB_API_BASE_URL) ||
  API_BASE_BY_PLATFORM[Platform.OS] ||
  API_BASE_BY_PLATFORM.default;

export function setApiBaseUrl(url) {
  const trimmed = (url || "").trim();
  if (trimmed) {
    apiBaseUrl = trimmed.replace(/\/+$/, "");
  }
}

export function getApiBaseUrl() {
  return apiBaseUrl;
}

export async function apiRequest(path, options = {}, token = "") {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    },
    ...options
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `${response.status} ${response.statusText}`);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

export async function login(username, password) {
  return apiRequest("/auth/login", {
    method: "POST",
    body: JSON.stringify({ username, password })
  });
}

export async function me(token) {
  return apiRequest("/auth/me", { method: "GET" }, token);
}

export async function getResidents(token) {
  return apiRequest("/residents", { method: "GET" }, token);
}

export async function getObservations(token) {
  return apiRequest("/observations", { method: "GET" }, token);
}

export async function createObservation(observation, token) {
  return apiRequest(
    "/observations",
    {
      method: "POST",
      body: JSON.stringify(observation)
    },
    token
  );
}

export async function getMedications(token) {
  return apiRequest("/medications", { method: "GET" }, token);
}
