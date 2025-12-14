
import './App.css';

import { GoogleOAuthProvider, GoogleLogin } from "@react-oauth/google";


function App() {

  



  const handleLoginSuccess = async (credentialResponse) => {
    const idToken = credentialResponse.credential;


    const res1 = await fetch("/auth/test", { method: "POST" });
    console.log(res1.status); 

    const res = await fetch("https://cybertrack.azurewebsites.net/auth/google", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token: idToken }),
      credentials: "include"
    });

    let data = {};
    try {
      data = await res.json();
    } catch {
      data.error = `Server returned status ${res.status} with empty body`;
    }



    if (res.ok && data.success !== false) {
      console.log("Logged in successfully");
    } else {
      console.error("Authentication failed", data.error);
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