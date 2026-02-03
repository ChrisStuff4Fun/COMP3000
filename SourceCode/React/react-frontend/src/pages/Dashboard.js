import React, { useState } from "react";

const ACCESS = {
  USER: 1,
  ESCALATED: 2,
  ADMIN: 3,
  ROOT: 4
};


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





function TopBar({ accessLevel, setActiveTab, refreshAuth }) {


    return (
        <div className="top-bar">
        <button onClick={() => setActiveTab("overview")}>Overview</button>
        <button onClick={() => setActiveTab("devices")}>Devices</button>
        <button onClick={() => setActiveTab("geofences")}>Geofences</button>
        <button onClick={() => setActiveTab("groups")}>Device Groups</button>
        <button onClick={() => setActiveTab("map")}>Map</button>
        <button onClick={() => setActiveTab("users")}>Users</button>
        <button onClick={() => setActiveTab("policies")}>Policies</button>

        <button disabled={accessLevel < ACCESS.ADMIN} onClick={() => setActiveTab("organisation")}>Organisation</button>

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

// ----------------------------------------------------------------------------------------------
function Devices() {
    return(
        <p> devices </p>
    )
}

// ----------------------------------------------------------------------------------------------

function Geofences() {
    return(
        <p> Geofences </p>
    )
}

// ----------------------------------------------------------------------------------------------

function DeviceGroups() {
    return(
        <p> DeviceGroups </p>
    )
}

// ----------------------------------------------------------------------------------------------

function Map() {
    return(
        <p> Map </p>
    )
}

// ----------------------------------------------------------------------------------------------

function Users() {
    return(
        <p> Users </p>
    )
}

// ----------------------------------------------------------------------------------------------

function Policies() {
    return(
        <p> Policies </p>
    )
}

// ----------------------------------------------------------------------------------------------

function DeleteOrgButton({ accessLevel, refreshAuth }) {
  const handleDelete = async () => {
    if (!window.confirm("This will permanently delete the organisation. Continue?")) {
      return;
    }

    const res = await fetch("/orgs/delete", {
      method: "DELETE",
      credentials: "include",
    });

    if (!res.ok) {
      alert("Failed to delete organisation");
      return;
    }

    refreshAuth();
  };

  return (
    <button
      className="danger-btn"
      disabled={accessLevel < ACCESS.ROOT}
      title={accessLevel < ACCESS.ROOT ? "Root access required" : ""}
      onClick={handleDelete}
    >
      Delete Organisation
    </button>
  );
}

function Organisation({accessLevel, refreshAuth}) {
    return(
        <div>
            <p> Organisation </p>
            <DeleteOrgButton accessLevel={accessLevel} refreshAuth={refreshAuth}/>
        </div>
    )
}

// ----------------------------------------------------------------------------------------------


export default function Dashboard({ username, accessLevel, refreshAuth }) {

    const [activeTab, setActiveTab] = useState("overview")

  return (
    <div>
      
        <div className="top-bar-header">
            <h2 className="header-item">Welcome</h2>
            <h1 className="header-item">CyberTrack Geofencing</h1>
            <h2 className="header-item">{orgName || "No Organisation"}</h2>
        </div>


      <TopBar accessLevel={accessLevel} activeTab={activeTab} setActiveTab={setActiveTab} refreshAuth={refreshAuth} />
      <div className="dashboard-panel">
        {activeTab === "overview" && <Overview />}
        {activeTab === "devices" && <Devices />}
        {activeTab === "geofences" && <Geofences />}
        {activeTab === "groups" && <DeviceGroups />}
        {activeTab === "map" && <Map />}
        {activeTab === "users" && <Users />}
        {activeTab === "policies" && <Policies />}
        {activeTab === "organisation" && <Organisation accessLevel={accessLevel} refreshAuth={refreshAuth}/>}
    </div>
    </div>
  );
}
