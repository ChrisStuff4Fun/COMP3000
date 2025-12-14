
import './App.css';

import { GoogleOAuthProvider, GoogleLogin } from "@react-oauth/google";


function App() {
  const handleLoginSuccess = async (credentialResponse) => {
    const idToken = credentialResponse.credential;

    const res = await fetch("https://cybertrack.azurewebsites.net/auth/google", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token: idToken }),
      credentials: "include"
    });

    if (res.ok) {
      console.log("Logged in successfully");
    } else {
      console.error("Authentication failed");
    }
  };


  const handleLoginError = () => {
    console.error("Login Failed");
  };

  return (
    <GoogleOAuthProvider clientId="824007775368-un5ifrgrig8c904rj9cgvu302rkkin9t.apps.googleusercontent.com">
      <div className="App">
        <h1>Login with Google</h1>
        <GoogleLogin
          onSuccess={handleLoginSuccess}
          onError={handleLoginError}
        />
      </div>
    </GoogleOAuthProvider>
  );
}

export default App;