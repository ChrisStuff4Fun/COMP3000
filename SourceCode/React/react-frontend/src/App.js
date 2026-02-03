import React, { useState, useEffect } from "react";
import { Routes, Route, Navigate, BrowserRouter } from "react-router-dom";
import LoginPage from "./pages/LoginPage";
import Dashboard from "./pages/Dashboard";
import "./App.css";

export default function App() {
  const [authState, setAuthState] = useState({
    loading: true,
    authenticated: false,
    username: null,
    registered: false,
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
      });
    } catch (err) {
      console.error("Auth check failed", err);
      setAuthState({
        loading: false,
        authenticated: false,
        username: null,
        registered: false,
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
        <Route
          path="/"
          element={
            authState.authenticated
              ? authState.registered
                ? <Navigate to="/dashboard" />
                : <LoginPage refreshAuth={refreshAuth} requireUsername={true} />
              : <LoginPage refreshAuth={refreshAuth} requireUsername={false} />
          }
        />

        <Route
          path="/dashboard"
          element={
            authState.authenticated && authState.registered
              ? <Dashboard 
              username={authState.username} 
              refreshAuth={refreshAuth}
              />
              : <Navigate to="/" />
          }
        />
      </Routes>
    </BrowserRouter>
  );
}
