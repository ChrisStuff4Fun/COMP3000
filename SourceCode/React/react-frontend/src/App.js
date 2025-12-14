
import './App.css';

import { GoogleOAuthProvider, GoogleLogin } from "@react-oauth/google";




function CreateUserButton() {
  const createUser = async () => {
    try {
      const res = await fetch("/user/create/chris", {
        method: "POST",
        credentials: "include" // IMPORTANT: sends auth cookie
      });

      const text = await res.text();

      if (!res.ok) {
        console.error("Create user failed:", text);
        alert(`Error: ${text}`);
        return;
      }

      console.log("User created successfully");
      alert("User created!");
    } catch (err) {
      console.error("Request error:", err);
    }
  };

  return (
    <button onClick={createUser}>
      Create User
    </button>
  );
}









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
    <div>
    <GoogleOAuthProvider clientId="824007775368-un5ifrgrig8c904rj9cgvu302rkkin9t.apps.googleusercontent.com">
      <div className="App">
        <h1>Login with Google</h1>
        <GoogleLogin
          onSuccess={handleLoginSuccess}
          onError={handleLoginError}
        />
      </div>
    </GoogleOAuthProvider>

    <CreateUserButton />
    </div>
  );

}

export default App;