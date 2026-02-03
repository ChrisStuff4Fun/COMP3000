import React, { useState } from "react";
import { GoogleOAuthProvider, GoogleLogin } from "@react-oauth/google";



export default function LoginPage({ refreshAuth, requireUsername }) {
  const [showUsernameForm, setShowUsernameForm] = useState(requireUsername);

  const handleLoginSuccess = async (credentialResponse) => {
    const idToken = credentialResponse.credential;

    // Send token to backend to issue JWT cookie
    const res = await fetch("/auth/google", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token: idToken }),
      credentials: "include",
    });

    if (!res.ok) {
      console.error("Google auth failed");
      return;
    }

    // Check if user has a username (new or existing)
    const me = await fetch("/auth/me", { credentials: "include" }).then(r => r.json());

    if (me.registered) {
      // Existing user, dashboard
      refreshAuth();
    } else {
      // New user, show username form
      setShowUsernameForm(true);
    }
  };

  const handleLoginError = () => console.error("Login Failed");

  const CreateUserForm = () => {
    const [username, setUsername] = useState("");

    const createUser = async () => {
      if (!username.trim()) return alert("Enter a username");

      const res = await fetch(`/user/create/${username}`, {
        method: "POST",
        credentials: "include" // JWT cookie identifies user
      });

      if (!res.ok) {
        const text = await res.text();
        alert(`Error: ${text}`);
        return;
      }

      alert("User created!");
      refreshAuth(); // re-fetch /auth/me
    };

    return (
      <div>
        <input
          type="text"
          placeholder="Choose a username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
        />
        <button onClick={createUser}>Create Username</button>
      </div>
    );
  };

  return (
    <div className="login-page">
        <h1> CyberTrack Geofencing </h1>
        <div className="login-card">

            <h2>Login with Google</h2>
             <div className="google-login-wrapper">
                <GoogleOAuthProvider clientId="824007775368-un5ifrgrig8c904rj9cgvu302rkkin9t.apps.googleusercontent.com">
                    <GoogleLogin
                    onSuccess={handleLoginSuccess}
                    onError={handleLoginError}
                    />
                    {showUsernameForm && <CreateUserForm />}
                </GoogleOAuthProvider>
            </div>
        </div>
    </div>
  );
}
