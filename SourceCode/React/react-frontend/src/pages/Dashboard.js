import React, { useEffect, useState } from "react";
import { MapContainer, TileLayer} from "react-leaflet";
import "leaflet/dist/leaflet.css";
import L from "leaflet";

const ACCESS = {
  USER: 1,
  ESCALATED: 2,
  ADMIN: 3,
  ROOT: 4
};

const ACCESS_LEVEL_NAME = {
  1: "User",
  2: "Escalated",
  3: "Admin",
  4: "Root",
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

        <button disabled={accessLevel < ACCESS.ESCALATED} onClick={() => setActiveTab("organisation")}>Organisation</button>

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

    delete L.Icon.Default.prototype._getIconUrl;
    L.Icon.Default.mergeOptions({
        iconRetinaUrl:
            "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
        iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
        shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
    });

/*
    const [devices, setDevices] = useState([]);
    const [geofences, setGeofences] = useState([]);

    useEffect(() => {
        const fetchData = async () => {
        try {
            const devicesRes = await fetch("/device/devices", { credentials: "include" });
            const geofencesRes = await fetch("geofence/geofences", { credentials: "include" });

            const devicesData = await devicesRes.json();
            const geofencesData = await geofencesRes.json();

            setDevices(devicesData);
            setGeofences(geofencesData);
        } catch (err) {
            console.error("Failed to load map data", err);
        }
        };

        fetchData();
    }, []);

*/


    return(
      <div className="map-wrapper">
        <MapContainer
          center={[50.375, -4.139]} 
          zoom={13}
          style={{ height: '100vh', width: '100%' }}
        >
        <TileLayer
          attribution='&copy; OpenStreetMap contributors'
          url='https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png'
        />
        </MapContainer>
      </div>
    )
}

// ----------------------------------------------------------------------------------------------

function Users(accessLevel) {
  const [users, setUsers] = useState([]);

  const fetchUsers = async () => {
    try {
      const res = await fetch("/user/users", { credentials: "include" });
      if (!res.ok) throw new Error("Failed to fetch users");
      const data = await res.json();
      setUsers(data);
    } catch (err) {
      console.error("Failed to fetch users", err);
    }
  };

  useEffect(() => {
    fetchUsers();
  }, []);

  const canModify = (targetLevel) => accessLevel > targetLevel;

  const releaseUser = async (userId) => {
    await fetch(`/release/${userId}`, {
      method: "POST"
    });
    await fetchUsers();
  };

  const updateAccessLevel = async (userId, newAL) => {
    try {
      const res = await fetch(`/update/${userId}/${newAL}`, {method: "PUT", credentials:"include"});
      if (!res.ok) throw new Error("Failed to update user");

      await fetchUsers();
    } catch {
      console.error("Failed to update user")
    }
  }

    

    return(
      <div>
      <h2>Organisation Users</h2>

      <div>
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Access Level</th>
              <th>Actions</th>
            </tr>
          </thead>

          <tbody>
          {users.map(user => {
            const targetLevel = user.accessLevel;
            const canAct = canModify(targetLevel);

            return (
              <tr key={user.id}>
                <td>{user.name}</td>

                <td>
                  {canAct ? (
                    <select
                      value={targetLevel}
                      onChange={(e) =>
                        updateAccessLevel(user.id, Number(e.target.value))
                      }
                    >
                      {/* ROOT can assign ADMIN + ESCALATED + USER */}
                      {accessLevel === ACCESS.ROOT && (
                        <>
                          <option value={ACCESS.ADMIN}>Admin</option>
                          <option value={ACCESS.ESCALATED}>Escalated</option>
                          <option value={ACCESS.USER}>User</option>
                        </>
                      )}

                      {/* ADMIN can assign ESCALATED + USER */}
                      {accessLevel === ACCESS.ADMIN && (
                        <>
                          <option value={ACCESS.ESCALATED}>Escalated</option>
                          <option value={ACCESS.USER}>User</option>
                        </>
                      )}
                    </select>
                  ) : (
                    targetLevel
                  )}
                </td>

                <td>
                  {canAct && (
                    <button
                      onClick={() => releaseUser(user)}
                      className="danger-btn"
                    >
                      Release
                    </button>
                  )}
                </td>
              </tr>
            );
          })}
        </tbody>
        </table>
      </div>
    </div>
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


const ORG_PANELS = {
    USERS: "users",
    DEVICES: "devices",
    DANGER: "danger",
};



function OrgSidebar({ active, setActive, accessLevel }) {
  return (
    <div className="org-sidebar">
      <button
        className={active === "users" ? "active" : ""}
        onClick={() => setActive("users")}
      >
        User Join Codes
      </button>

      <button
        className={active === "devices" ? "active" : ""}
        onClick={() => setActive("devices")}
      >
        Device Join Codes
      </button>

      <button
        disabled={accessLevel < ACCESS.ADMIN}
        className={active === "danger" ? "active danger" : "danger"}
        onClick={() => setActive("danger")}
      >
        Danger Zone
      </button>
    </div>
  );
}


function DeviceJoinCodeSection({accessLevel}) {
    const [codes, setCodes] = useState([]);
    const [loading, setLoading] = useState(false);
    const [validHours, setValidHours] = useState(24);


    const fetchCodes = async () => {
        try {
            const res = await fetch("/joincodes/getdevicecodes", { credentials: "include" });
            const data = await res.json();
            setCodes(data);
        } catch (err) {
            console.error("Failed to fetch join codes", err);
        }
    };

    useEffect(() => {
        fetchCodes();
    }, []);

    
    const generateCode = async () => {
        setLoading(true);

        try {
            const res = await fetch(`joincodes/createdevicecode/${encodeURI(validHours)}`, {
            method: "POST",
            credentials: "include",
            });

            if (res.ok) {
            await fetchCodes();
            } else {
            alert("Failed to generate code");
            }
        } catch (err) {
            console.error(err);
            alert("Error generating code");
        } finally {
            setLoading(false);
        }
    };



    const purgeCodes = async () => {
        if (!window.confirm("Are you sure you want to purge all used and expired device join codes?")) return;

        setLoading(true);
        try {
        const res = await fetch("joincodes/purgedevicecodes", {
            method: "DELETE",
            credentials: "include",
        });
        if (res.ok) {
            await fetchCodes(); 
        } else {
            alert("Failed to purge codes");
        }
        } catch (err) {
        console.error(err);
        alert("Error purging codes");
        } finally {
        setLoading(false);
        }
  };


    return (
    <div>
      <h3>Device Join Codes</h3>

      <div style={{ display: "flex", alignItems: "center", gap: "12px", marginBottom: "12px" }}>
        <label>
          Validity (hours):
          <select value={validHours} onChange={(e) => setValidHours(Number(e.target.value))}>
            <option value={1}>1</option>
            <option value={6}>6</option>
            <option value={12}>12</option>
            <option value={24}>24</option>
            <option value={48}>48</option>
          </select>
        </label>

        <button
          disabled={accessLevel < ACCESS.ADMIN || loading}
          onClick={generateCode}
        >
          {loading ? "Generating..." : "Generate Code"}
        </button>

        <button
          disabled={accessLevel < ACCESS.ADMIN || loading}
          onClick={purgeCodes}
        >
          {loading ? "Purging..." : "Purge Codes"}
        </button>

      </div>

      <table>
        <thead>
          <tr>
            <th>Code</th>
            <th>Expiry</th>
            <th>Used</th>
          </tr>
        </thead>
        <tbody>
          {codes.map((c) => (
            <tr key={c.code}>
              <td>{c.code}</td>
              <td>{c.expiryDate}</td>
              <td>{c.isUsed ? "Yes" : "No"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );


};




function UserJoinCodeSection({accessLevel}) {
    const [codes, setCodes] = useState([]);
    const [loading, setLoading] = useState(false);
    const [validHours, setValidHours] = useState(24);


    const fetchCodes = async () => {
        try {
            const res = await fetch("/joincodes/getusercodes", { credentials: "include" });
            const data = await res.json();
            setCodes(data);
        } catch (err) {
            console.error("Failed to fetch join codes", err);
        }
    };

    useEffect(() => {
        fetchCodes();
    }, []);

    
    const generateCode = async () => {
        setLoading(true);

        try {
            const res = await fetch(`joincodes/createusercode/${encodeURI(validHours)}`, {
            method: "POST",
            credentials: "include",
            });

            if (res.ok) {
            await fetchCodes();
            } else {
            alert("Failed to generate code");
            }
        } catch (err) {
            console.error(err);
            alert("Error generating code");
        } finally {
            setLoading(false);
        }
    };



    const purgeCodes = async () => {
        if (!window.confirm("Are you sure you want to purge all used and expired user join codes?")) return;

        setLoading(true);
        try {
        const res = await fetch("joincodes/purgeusercodes", {
            method: "DELETE",
            credentials: "include",
        });
        if (res.ok) {
            await fetchCodes(); 
        } else {
            alert("Failed to purge codes");
        }
        } catch (err) {
        console.error(err);
        alert("Error purging codes");
        } finally {
        setLoading(false);
        }
  };


    return (
    <div>
      <h3>User Join Codes</h3>

      <div style={{ display: "flex", alignItems: "center", gap: "12px", marginBottom: "12px" }}>
        <label>
          Validity (hours):
          <select value={validHours} onChange={(e) => setValidHours(Number(e.target.value))}>
            <option value={1}>1</option>
            <option value={6}>6</option>
            <option value={12}>12</option>
            <option value={24}>24</option>
            <option value={48}>48</option>
          </select>
        </label>

        <button
          disabled={accessLevel < ACCESS.ADMIN || loading}
          onClick={generateCode}
        >
          {loading ? "Generating..." : "Generate Code"}
        </button>

        <button
          disabled={accessLevel < ACCESS.ADMIN || loading}
          onClick={purgeCodes}
        >
          {loading ? "Purging..." : "Purge Codes"}
        </button>

      </div>

      <table>
        <thead>
          <tr>
            <th>Code</th>
            <th>Expiry</th>
            <th>Used</th>
          </tr>
        </thead>
        <tbody>
          {codes.map((c) => (
            <tr key={c.code}>
              <td>{c.code}</td>
              <td>{c.expiryDate}</td>
              <td>{c.isUsed ? "Yes" : "No"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );


};




function Organisation({ accessLevel, refreshAuth }) {
  const [activePanel, setActivePanel] = useState(ORG_PANELS.USERS);

  return (
    <div className="org-layout">
      <OrgSidebar
        active={activePanel}
        setActive={setActivePanel}
        accessLevel={accessLevel}
      />

      <div className="org-content">
        {activePanel === ORG_PANELS.DEVICES && (
          <DeviceJoinCodeSection accessLevel={accessLevel}/>
        )}

        {activePanel === ORG_PANELS.USERS && (
          <UserJoinCodeSection accessLevel={accessLevel}/>
        )}

        {activePanel === ORG_PANELS.DANGER && (
          <DeleteOrgButton accessLevel={accessLevel} refreshAuth={refreshAuth}/>
        )}
      </div>
    </div>
  );
}


// ----------------------------------------------------------------------------------------------


export default function Dashboard({ authState, refreshAuth }) {

    const [activeTab, setActiveTab] = useState("overview")

  return (
    <div>
      
        <div className="header-row">
            <h1 className="app-title">CyberTrack Geofencing</h1>

            <div className="user-info">
                <div className="username">{authState.username}</div>
                <div className="access-level">Access: {ACCESS_LEVEL_NAME[authState.accessLevel]}</div>
                <div className="org-name">{authState.orgName}</div>
            </div>
        </div>


      <TopBar accessLevel={authState.accessLevel} activeTab={activeTab} setActiveTab={setActiveTab} refreshAuth={refreshAuth} />
      <div className="dashboard-panel">
        {activeTab === "overview" && <Overview />}
        {activeTab === "devices" && <Devices />}
        {activeTab === "geofences" && <Geofences />}
        {activeTab === "groups" && <DeviceGroups />}
        {activeTab === "map" && <Map />}
        {activeTab === "users" && <Users accessLevel={authState.accessLevel}/>}
        {activeTab === "policies" && <Policies />}
        {activeTab === "organisation" && <Organisation accessLevel={authState.accessLevel} refreshAuth={refreshAuth}/>}
    </div>
    </div>
  );
}
