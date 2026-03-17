import React, { useEffect, useRef, useState } from "react";
import { FeatureGroup, MapContainer, TileLayer, Marker, Popup, Circle, GeoJSON} from "react-leaflet";
import { EditControl } from "react-leaflet-draw";
import "leaflet/dist/leaflet.css";
import 'leaflet-draw/dist/leaflet.draw.css';
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
        <button onClick={() => setActiveTab("map")}>Map</button>
        <button onClick={() => setActiveTab("devices")}>Devices</button>
        <button onClick={() => setActiveTab("geofences")}>Geofences</button>
        <button onClick={() => setActiveTab("groups")}>Device Groups</button>
        <button onClick={() => setActiveTab("users")}>Users</button>
        <button onClick={() => setActiveTab("policies")}>Policies</button>

        <button disabled={accessLevel < ACCESS.ESCALATED} onClick={() => setActiveTab("organisation")}>Organisation</button>

        <LogoutButton refreshAuth={refreshAuth} />
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

// Circle approximation - get grid
function getBoundingBox(coords) {
  let minLat = Infinity, maxLat = -Infinity;
  let minLon = Infinity, maxLon = -Infinity;

  coords.forEach(([lon, lat]) => {
    if (lat < minLat) minLat = lat;
    if (lat > maxLat) maxLat = lat;
    if (lon < minLon) minLon = lon;
    if (lon > maxLon) maxLon = lon;
  });

  return { minLat, maxLat, minLon, maxLon };
}

// Circle approximation - check if given point is within given polygon
function pointInPolygon(point, polygon) {
  const [x, y] = point;
  let inside = false;

  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const [xi, yi] = polygon[i];
    const [xj, yj] = polygon[j];

  const intersect =
    ((yi > y) !== (yj > y)) &&
    x < ((xj - xi) * (y - yi)) / (yj - yi) + xi;

    if (intersect) inside = !inside;
  }

  return inside;
}

// Check if two line segments [p1,p2] and [q1,q2] intersect
function segmentsIntersect(p1, p2, q1, q2) {
  function ccw(a, b, c) {
    return (c[1]-a[1])*(b[0]-a[0]) > (b[1]-a[1])*(c[0]-a[0]);
  }
  return ccw(p1,q1,q2) !== ccw(p2,q1,q2) && ccw(p1,p2,q1) !== ccw(p1,p2,q2);
}


// Check if any polygon edge intersects a square
function polygonIntersectsSquare(polygon, squareCorners) {
  // polygon: array of [lon, lat]
  // squareCorners: array of [lon, lat] (4 corners in order)
  
  const squareEdges = [
    [squareCorners[0], squareCorners[1]],
    [squareCorners[1], squareCorners[3]],
    [squareCorners[3], squareCorners[2]],
    [squareCorners[2], squareCorners[0]]
  ];

  for (let i=0, j=polygon.length-1; i<polygon.length; j=i++) {
    const polyEdge = [polygon[j], polygon[i]];
    for (const sqEdge of squareEdges) {
      if (segmentsIntersect(polyEdge[0], polyEdge[1], sqEdge[0], sqEdge[1])) {
        return true;
      }
    }
  }
  return false;
}


// Circle approximation - take polygon array and amount of grid cells to compute array of approx circles
function generateApproxCircles(polygonCoords, gridSize) {

  const { minLat, maxLat, minLon, maxLon } =
    getBoundingBox(polygonCoords);

  const latStep = (maxLat - minLat) / gridSize;
  const lonStep = (maxLon - minLon) / gridSize;

  const circles = [];

  for (let i = 0; i < gridSize; i++) {
    for (let j = 0; j < gridSize; j++) {

      const cellMinLat = minLat + i * latStep;
      const cellMaxLat = cellMinLat + latStep;

      const cellMinLon = minLon + j * lonStep;
      const cellMaxLon = cellMinLon + lonStep;

      const centreLat = (cellMinLat + cellMaxLat) / 2;
      const centreLon = (cellMinLon + cellMaxLon) / 2;

      // corners
      const corners = [
        [cellMinLon, cellMinLat],
        [cellMaxLon, cellMinLat],
        [cellMinLon, cellMaxLat],
        [cellMaxLon, cellMaxLat]
      ];

      let overlaps = false;

      // check if square centre is inside polygon
      if (pointInPolygon([centreLon, centreLat], polygonCoords)) {
        overlaps = true;
      }

      // check for square's corners inside polygon
      if (!overlaps) {
        for (const c of corners) {
          if (pointInPolygon(c, polygonCoords)) {
            overlaps = true;
            break;
          }
        }
      }

      // check if polygon enters the square at all
      if (!overlaps) {
        if (polygonIntersectsSquare(polygonCoords, corners)) {
          overlaps = true;
        }
      }

      if (overlaps) {

        // radius = half diagonal
        const latMeters = latStep * 111320;
        const lonMeters = lonStep * 111320 *
          Math.cos(centreLat * Math.PI / 180);

        const cellWidthMeters = Math.sqrt(
          latMeters ** 2 + lonMeters ** 2
        );

        const radius = cellWidthMeters / 2;

        circles.push({
          lat: centreLat,
          lon: centreLon,
          radius
        });
      }
    }
  }

  return circles;
}



function GeofenceSection({accessLevel}) {

  const [geofences, setGeofences] = useState([]);

  const fetchGeofences = async() => {
    try { 
      const res = await fetch("/geofence/geofences", {credentials: "include"});
      if (!res.ok) throw new Error("Failed to fetch geofences");

      const data = await res.json();
      setGeofences(data);

      console.log(data);
    } catch {
      console.error("Failed to fetch geofences");
    }
  }

    useEffect(() => {
      fetchGeofences();
    }, []);


  const canModify = (accessLevel >= ACCESS.ADMIN);

  const deleteGeofence = async (id) => {
    try {
      const res = await fetch(`/geofence/delete/${id}`, {
        method: "DELETE",
        credentials: "include",
      });
      if (!res.ok) throw new Error("Failed to delete geofence");
      await fetchGeofences();
    } catch (err) {
      console.error("Failed to delete geofence", err);
    }
  };

  const updateGeofenceName = async (id, newName) => {
    try{
      const res = await fetch(`/geofence/update/${id}/${newName}`, {method:"PUT", credentials:"include"});
      if (!res.ok) throw new Error("Failed to update geofence");

      await fetchGeofences();
    } catch {
      console.error("Failed to update geofence");
    }
    
  };
  
    return (
    <div>
      <h2>Geofences</h2>
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {geofences.map((fence) => (
            <tr key={fence.geofenceID}>
              <td>
                {canModify ? (
                  <input
                    type="text"
                    value={fence.geofenceName}
                    onChange={(e) => setGeofences((prev) => prev.map((oldFence) => oldFence.geofenceID === fence.geofenceID ? { ...oldFence, geofenceName: e.target.value } : oldFence) )}
                    onBlur={(e) => updateGeofenceName(fence.geofenceID, e.target.value)}
                  />
                ) : (
                  fence.geofenceName
                )}
              </td>
              <td>
                <button onClick={() => deleteGeofence(fence.geofenceID)} className="danger-btn" disabled={!canModify}> Delete </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}





function CreateFenceSection ({accessLevel}) {
  const [name, setName] = useState("");
  // shape is GeoJSON obj
  const [shape, setShape] = useState(null);
  const fgRef = useRef(null);


  const handleSave = async () => {
    if (!name || !shape) {
      alert("No name or shape.");
      return;
    }


    try {
      const res = await fetch(`/geofence/create/${encodeURIComponent(name)}`, {
        method: "POST",
        credentials: "include", 
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(shape), 
      });

      if (!res.ok) {
        const errText = await res.text();
        throw new Error(errText || "Failed to create geofence");
      }

      alert("Geofence created successfully!");


      setName("");
      setShape(null);

    } catch (err) {
      console.error("Error creating geofence:", err);
    }

  }


  const handleCreate = (e) => {
    const layer = e.layer;

    const featureGroup = fgRef.current;
    if (featureGroup) {
      featureGroup.eachLayer((layer) => featureGroup.removeLayer(layer));
    }

    layer.addTo(featureGroup);


    if (layer instanceof L.Circle) {
      const circleGeo = layer.toGeoJSON();
      circleGeo.properties = {
        ...circleGeo.properties,
        radius: layer.getRadius()}
      
      setShape(circleGeo);
    }
    else {
      const geo = layer.toGeoJSON();

      // extract actual coords from GeoJSON
      const polygonCoords = geo.geometry.coordinates[0];

      const approxCircles = generateApproxCircles( polygonCoords, 5); // grid resolution (make configurable later)

      const approxShape = {type: "approxMultiCircle", circles: approxCircles};

      setShape(approxShape);

      // render approx circles 
      const featureGroup = fgRef.current;

      approxCircles.forEach(c => {
        L.circle([c.lat, c.lon], {
          radius: c.radius,
          color: "blue",
          fillOpacity: 0.2
        }).addTo(featureGroup);
      });
    }

    
  }



   return (
    <div>
      <label>
        Geofence Name:
        <input
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </label>

      <MapContainer
        center={[50.375, -4.139]}
        zoom={13}
        style={{ height: '60vh', width: '100%' }}
      >
        <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />

        <FeatureGroup ref={fgRef}>
          <EditControl
            position="topright"
            onCreated={handleCreate}
            onDeleted={() => setShape(null)}
            draw={{
              rectangle: false,
              marker: false,
              polyline: false,
              circlemarker: false,
              polygon: true,
              circle: true,
            }}
            edit={{
            featureGroup: fgRef.current,
            }}
          />
        </FeatureGroup>
      </MapContainer>

      <button
        style={{ marginTop: "12px" }}
        onClick={handleSave}
      >
        Create Geofence
      </button>
    </div>
  );
}





const GEOFENCE_PANELS = {
    OVERVIEW: "overview",
    CREATE: "create",
};


function GeofenceSidebar({ active, setActive, accessLevel }) {
  return (
    <div className="org-sidebar">
      <button
        className={active === "overview" ? "active" : ""}
        onClick={() => setActive("overview")}
      >
        Geofences
      </button>

      <button
        className={active === "create" ? "active" : ""}
        onClick={() => setActive("create")}
        disabled={accessLevel<ACCESS.ADMIN}
      >
        Create
      </button>
    </div>
  );
}


function Geofences({ accessLevel }) {
  const [activePanel, setActivePanel] = useState(GEOFENCE_PANELS.OVERVIEW);

  return (
    <div className="org-layout">
      <GeofenceSidebar
        active={activePanel}
        setActive={setActivePanel}
        accessLevel={accessLevel}
      />

      <div className="org-content">
        {activePanel === GEOFENCE_PANELS.OVERVIEW && (
          <GeofenceSection accessLevel={accessLevel}/>
        )}

        {activePanel === GEOFENCE_PANELS.CREATE && (
          <CreateFenceSection accessLevel={accessLevel}/>
        )}

      </div>
    </div>
  );
}

// ----------------------------------------------------------------------------------------------

function DeviceGroups() {
    return(
        <p> DeviceGroups </p>
    )
}

// ----------------------------------------------------------------------------------------------

function Map() {

    const [devices, setDevices] = useState([]);
    const [geofences, setGeofences] = useState([]);

    useEffect(() => {
        const fetchFenceData = async () => {
        try {
            const geofencesRes = await fetch("/geofence/geofences", { credentials: "include" });
            const geofencesData = await geofencesRes.json();
            setGeofences(geofencesData);

        } catch (err) {
            console.error("Failed to fence map data", err);
        }
        };

        fetchFenceData();
    }, []);


    useEffect(() => {
        const fetchDeviceData = async () => {
        try {
            const devicesRes = await fetch("/device/devices", { credentials: "include" });
            const devicesData = await devicesRes.json();
            setDevices(devicesData);

        } catch (err) {
            console.error("Failed to device map data", err);
        }
        };

        fetchDeviceData();
    }, []);




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

          {geofences.map((fence) => {
          const geo = JSON.parse(fence.geoJSON);

          // Circles
          if (geo.geometry.type === "Point" && geo.properties?.radius) {
            return (
              <Circle
                key={fence.geofenceID}
                center={[geo.geometry.coordinates[1], geo.geometry.coordinates[0]]}
                radius={geo.properties.radius}
                pathOptions={{ color: "red", fillOpacity: 0.3 }}
              >
                <Popup>{fence.geofenceName}</Popup>
              </Circle>
            );
          }

            // Polygons 
            return (
              <GeoJSON
                key={fence.geofenceID}
                data={geo}
                style={{ color: "blue", weight: 2, fillOpacity: 0.2 }}
                onEachFeature={(feature, layer) => layer.bindPopup(fence.geofenceName)}
              />
            );
          })}

          {/* Render devices as markers */}
          {devices.map((device) => (
            <Marker
              key={device.deviceID}
              position={[device.lastLoggedLat, device.lastLoggedLong]}
            >
              <Popup>{device.deviceName}</Popup>
            </Marker>
          ))}



        </MapContainer>
      </div>
    )
}

// ----------------------------------------------------------------------------------------------

function Users({accessLevel}) {
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

  const releaseUser = async (userId) => {
    await fetch(`/user/release/${userId}`, {
      method: "POST"
    });
    await fetchUsers();
  };

  const updateAccessLevel = async (userId, newAL) => {
    try {
      const res = await fetch(`/user/update/${userId}/${newAL}`, {method: "POST", credentials:"include"});
      if (!res.ok) {
        const error = await res.text();
        throw new Error("Failed to update user", error);
      }

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

          return (
            <tr key={user.userID}>
              <td>{user.name}</td>

              <td>
                <select disabled={(accessLevel<3) || (targetLevel === 4)}
                  value={targetLevel}
                  onChange={(e) =>
                    updateAccessLevel(user.userID, Number(e.target.value))
                  }
                >
                  {targetLevel === ACCESS.ROOT && (
                    <>
                      <option value={ACCESS.ROOT}>Root</option>
                    </>
                  )}
                  {/* only roles you can assign */}
                  {accessLevel === ACCESS.ROOT && (
                    <>
                      <option value={ACCESS.ADMIN}>Admin</option>
                      <option value={ACCESS.ESCALATED}>Escalated</option>
                      <option value={ACCESS.USER}>User</option>
                    </>
                  )}
                  {accessLevel === ACCESS.ADMIN && (
                    <>
                      <option value={ACCESS.ESCALATED}>Escalated</option>
                      <option value={ACCESS.USER}>User</option>
                    </>
                  )}
                </select>
              </td>

              <td>
                <button onClick={() => releaseUser(user.userID)} className="danger-btn" disabled={(accessLevel <= user.accessLevel) || (accessLevel < ACCESS.ADMIN)}>Release</button>
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

    const [activeTab, setActiveTab] = useState("map")

  return (
    <div className="full-screen-wrapper">
      
        <div className="header-row">
            <img className="app-logo" alt="CyberTrack Logo" src="/logo.png"/>
            <h1 className="app-title">CyberTrack Geofencing</h1>

            <div className="user-info">
                <div className="username">{authState.username}</div>
                <div className="access-level">Access: {ACCESS_LEVEL_NAME[authState.accessLevel]}</div>
                <div className="org-name">{authState.orgName}</div>
            </div>
        </div>


      <TopBar accessLevel={authState.accessLevel} activeTab={activeTab} setActiveTab={setActiveTab} refreshAuth={refreshAuth} />
      <div className="dashboard-panel">
        {activeTab === "devices" && <Devices />}
        {activeTab === "geofences" && <Geofences accessLevel={authState.accessLevel}/>}
        {activeTab === "groups" && <DeviceGroups />}
        {activeTab === "map" && <Map />}
        {activeTab === "users" && <Users accessLevel={authState.accessLevel}/>}
        {activeTab === "policies" && <Policies />}
        {activeTab === "organisation" && <Organisation accessLevel={authState.accessLevel} refreshAuth={refreshAuth}/>}
      </div>
    </div>
  );
}
