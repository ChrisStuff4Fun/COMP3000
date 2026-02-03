import React from "react";



function LogoutButton({refreshAuth}) {
    const handleLogout = async () => {
        await fetch("auth/logout", {
            method: "POST",
            credentials:"include"
        })
        refreshAuth();
    }
    return <button onClick={handleLogout}>Log out</button>
}














export default function Dashboard({ username }) {
  return (
    <div>
      <h1>Welcome, {username}</h1>
      <p> test test test</p>
    <LogoutButton refreshAuth={refreshAuth}/>
    </div>
  );
}
