import logo from './logo.svg';
import './App.css';

import { useEffect } from "react";

function GoogleLoginButton() {
  useEffect(() => {
    if (window.google && window.google.accounts) {
      window.google.accounts.id.initialize({
        client_id: "824007775368-un5ifrgrig8c904rj9cgvu302rkkin9t.apps.googleusercontent.com ",
        callback: handleCredentialResponse,
      });
      window.google.accounts.id.renderButton(
        document.getElementById("google-signin"),
        { theme: "outline", size: "large" }
      );
    }
  }, []);

  async function handleCredentialResponse(response) {
    const idToken = response.credential;
    const res = await fetch("/auth/google", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token: idToken }),
    });

    if (res.ok) console.log("Logged in successfully");
    else console.error("Authentication failed");
  }

  return <div id="google-signin"></div>;
}

function App() {
  return (
    <div className="App">
      <GoogleLoginButton />

      <header className="App-header">
        <p>Edit <code>src/App.js</code> and save to reload.</p>
      </header>
    </div>
  );
}

export default App;
