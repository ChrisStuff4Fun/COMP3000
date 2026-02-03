import React, { useState, useEffect } from "react";
import { Routes, Route, Navigate, BrowserRouter } from "react-router-dom";
import LoginPage from "./pages/LoginPage";
import Dashboard from "./pages/Dashboard";
import OrgPage from "./pages/Org";
import "./App.css";

export default function App() {
  const [authState, setAuthState] = useState({
    loading: true,
    authenticated: false,
    username: null,
    registered: false,
    orgId: 0,
    accessLevel: 0
  });

  // Fetch /auth/me to check auth & registration 
  const refreshAuth = async () => {
    try {
      const res = await fetch("/auth/me", { credentials: "include" });
      const data = await res.json();

      setAuthState({
        loading: false,
        authenticated: data.authenticated || false,
        username: data.username || null,
        registered: data.registered || false,
        orgId: data.orgId || 0,
        accessLevel: data.accessLevel || 0
      });
    } catch (err) {
      console.error("Auth check failed", err);
      setAuthState({
        loading: false,
        authenticated: false,
        username: null,
        registered: false,
        orgId: 0,
        accessLevel: 0
      });
    }
  };

  useEffect(() => {
    refreshAuth();
  }, []);

  if (authState.loading) return <p>Loading...</p>;

  return (
   <BrowserRouter>
    <Routes>

      {/* LOGIN */}
      <Route
        path="/"
        element={
          authState.authenticated
            ? authState.registered
              ? authState.orgId > 0
                ? <Navigate to="/dashboard" />
                : <Navigate to="/org" />
              : <LoginPage refreshAuth={refreshAuth} requireUsername={true} />
            : <LoginPage refreshAuth={refreshAuth} requireUsername={false} />
        }
      />

      {/* ORG CREATE / JOIN */}
      <Route
        path="/org"
        element={
          authState.authenticated && authState.registered && authState.orgId === 0
            ? <OrgPage refreshAuth={refreshAuth} />
            : <Navigate to="/dashboard" />
        }
      />

      {/* DASHBOARD */}
      <Route
        path="/dashboard"
        element={
          authState.authenticated && authState.registered && authState.orgId > 0
            ? (
              <Dashboard
                username={authState.username}
                accessLevel={authState.accessLevel}
                refreshAuth={refreshAuth}
              />
            )
            : <Navigate to="/" />
        }
      />

    </Routes>
  </BrowserRouter>
);
}
