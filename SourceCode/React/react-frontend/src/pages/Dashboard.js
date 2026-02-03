import React, { useState } from "react";



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





function TopBar({ activeTab, setActiveTab, refreshAuth }) {


    return (
        <div className="top-bar">
        <button onClick={() => setActiveTab("overview")}>Overview</button>
        <button onClick={() => setActiveTab("devices")}>Devices</button>
        <button onClick={() => setActiveTab("geofences")}>Geofences</button>
        <button onClick={() => setActiveTab("groups")}>Device Groups</button>
        <button onClick={() => setActiveTab("map")}>Map</button>
        <button onClick={() => setActiveTab("users")}>Users</button>
        <button onClick={() => setActiveTab("policies")}>Policies</button>
        <LogoutButton refreshAuth={refreshAuth} />
        </div>
    );
}


function Overview() {
  return (
    <div className="dashboard-overview">
      <div className="box"> <Devices/> </div>
      <div className="box"> <Geofences/> </div>
      <div className="box"> <DeviceGroups/> </div>
      <div className="box"> <Map/> </div>
    </div>
  );
}

function Devices() {
    return(
        <p> devices </p>
    )
}
function Geofences() {
    return(
        <p> Geofences </p>
    )
}
function DeviceGroups() {
    return(
        <p> DeviceGroups </p>
    )
}
function Map() {
    return(
        <p> Map </p>
    )
}
function Users() {
    return(
        <p> Users </p>
    )
}
function Policies() {
    return(
        <p> Policies </p>
    )
}



export default function Dashboard({ username, refreshAuth }) {

    const [activeTab, setActiveTab] = useState("overview")

  return (
    <div>
      <h1>Welcome, {username}</h1>
      <TopBar activeTab={activeTab} setActiveTab={setActiveTab} refreshAuth={refreshAuth} />
      <div className="dashboard-panel">
        {activeTab === "overview" && <Overview />}
        {activeTab === "devices" && <Devices />}
        {activeTab === "geofences" && <Geofences />}
        {activeTab === "groups" && <DeviceGroups />}
        {activeTab === "map" && <Map />}
        {activeTab === "users" && <Users />}
        {activeTab === "policies" && <Policies />}
    </div>
    </div>
  );
}
