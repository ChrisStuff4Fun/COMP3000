import React, { useEffect, useRef, useState } from "react";
import { FeatureGroup, MapContainer, TileLayer, Marker, Popup, Circle, Polygon} from "react-leaflet";
import { EditControl } from "react-leaflet-draw";
import "leaflet/dist/leaflet.css";
import 'leaflet-draw/dist/leaflet.draw.css';
import L from "leaflet";



import markerIcon from "leaflet/dist/images/marker-icon.png";
import markerShadow from "leaflet/dist/images/marker-shadow.png";

delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
    iconUrl: markerIcon,
    shadowUrl: markerShadow,
});

const ACCESS = {
  USER: 1,
  ELEVATED: 2,
  ADMIN: 3,
  ROOT: 4
};

const ACCESS_LEVEL_NAME = {
  1: "User",
  2: "Elevated",
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

        <button disabled={accessLevel < ACCESS.ELEVATED} onClick={() => setActiveTab("organisation")}>Join Codes</button>

        <button disabled={accessLevel < ACCESS.ROOT} onClick={() => setActiveTab("root")}>Root User Menu</button>

        <LogoutButton refreshAuth={refreshAuth} />
        </div>
    );
}


// ----------------------------------------------------------------------------------------------

function Devices({ accessLevel }) {
  const [devices, setDevices] = useState([]);

  const fetchDevices = async () => {
    try {
      const res = await fetch("/device/devices", { credentials: "include" });
      console.log("devices status:", res.status);
      if (res.status === 404) { setDevices([]); return; }
      if (!res.ok) throw new Error("Failed to fetch devices");
      const data = await res.json();
      setDevices(data);
    } catch (err) {
      console.error("Failed to fetch devices", err);
    }
  };

  useEffect(() => {
    fetchDevices();
  }, []);

  const releaseDevice = async (deviceId) => {
    if (!window.confirm("Are you sure you want to release this device?")) return;
    try {
      const res = await fetch(`/device/release/${deviceId}`, {
        method: "DELETE",
        credentials: "include",
      });
      if (!res.ok) throw new Error("Failed to release device");
      await fetchDevices();
    } catch (err) {
      console.error("Failed to release device", err);
    }
  };

  const updateDeviceName = async (deviceId, newName) => {
    try {
      const res = await fetch(`/device/update/${deviceId}/${encodeURIComponent(newName)}`, {
        method: "PUT",
        credentials: "include",
      });
      if (!res.ok) throw new Error("Failed to update device name");
      await fetchDevices();
    } catch (err) {
      console.error("Failed to update device name", err);
    }
  };

  return (
    <div>
      <h2>Devices</h2>
      <table>
        <thead>
          <tr>
            <th>Device Name</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {devices.map((device) => (
            <tr key={device.deviceID}>
              <td>
                {accessLevel >= ACCESS.ADMIN ? (
                  <input
                    type="text"
                    value={device.deviceName}
                    onChange={(e) =>
                      setDevices((prev) =>
                        prev.map((d) =>
                          d.deviceID === device.deviceID
                            ? { ...d, deviceName: e.target.value }
                            : d
                        )
                      )
                    }
                    onBlur={(e) => updateDeviceName(device.deviceID, e.target.value)}
                  />
                ) : (
                  device.deviceName
                )}
              </td>
              <td>
                <button
                  className="danger-btn"
                  disabled={accessLevel < ACCESS.ADMIN}
                  onClick={() => releaseDevice(device.deviceID)}
                >
                  Release
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
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

      const approxCircles = generateApproxCircles( polygonCoords, 12); // grid resolution (make configurable later)

      const approxShape = {
        type: "FeatureCollection",
        features: [
        {
          type: "Feature",
          geometry: geo.geometry, // original polygon
          properties: { type: "polygon" }
        },
          ...approxCircles.map(c => ({
            type: "Feature",
            geometry: {
              type: "Point",
              coordinates: [c.lon, c.lat]
            },
            properties: {
              radius: c.radius
            }
        }))
        ]
      };

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

const GROUP_PANELS = {
  OVERVIEW: "overview",
  CREATE: "create",
};

function GroupSidebar({ active, setActive, accessLevel }) {
  return (
    <div className="org-sidebar">
      <button
        className={active === GROUP_PANELS.OVERVIEW ? "active" : ""}
        onClick={() => setActive(GROUP_PANELS.OVERVIEW)}
      >
        Groups
      </button>

      <button
        className={active === GROUP_PANELS.CREATE ? "active" : ""}
        onClick={() => setActive(GROUP_PANELS.CREATE)}
        disabled={accessLevel < ACCESS.ADMIN}
      >
        Create
      </button>
    </div>
  );
}


function GroupOverview({ accessLevel }) {
  const [groups, setGroups] = useState([]);
  const [allDevices, setAllDevices] = useState([]);
  const [groupDevices, setGroupDevices] = useState({});
  const [expanded, setExpanded] = useState(null);

  const fetchAll = async () => {
    const g = await fetch("/group/groups", { credentials: "include" });
    const d = await fetch("/device/devices", { credentials: "include" });

    setGroups(g.ok ? await g.json() : []);
    setAllDevices(d.ok ? await d.json() : []);
  };

  const fetchGroupDevices = async (groupId) => {
    const res = await fetch(`/group/${groupId}/devices`, {
      credentials: "include",
    });

    if (res.ok) {
      const data = await res.json();
      setGroupDevices(prev => ({ ...prev, [groupId]: data }));
    }
  };

  useEffect(() => {
    fetchAll();
  }, []);

  const toggleExpand = async (groupId) => {
    if (expanded === groupId) {
      setExpanded(null);
      return;
    }

    setExpanded(groupId);

    if (!groupDevices[groupId]) {
      await fetchGroupDevices(groupId);
    }
  };

  const deleteGroup = async (id) => {
    if (!window.confirm("Delete this group?")) return;

    await fetch(`/group/delete/${id}`, {
      method: "DELETE",
      credentials: "include",
    });

    fetchAll();
  };

  const addDevice = async (groupId, deviceId) => {
    await fetch(`/group/${groupId}/adddevice/${deviceId}`, {
      method: "PUT",
      credentials: "include",
    });

    await fetchGroupDevices(groupId);
  };

  const removeDevice = async (groupId, deviceId) => {
    await fetch(`/group/${groupId}/removedevice/${deviceId}`, {
      method: "PUT",
      credentials: "include",
    });

    await fetchGroupDevices(groupId);
  };

  console.log("accessLevel:", accessLevel);

  return (
    <div>
      <h2>Device Groups</h2>

      {groups.map(group => {
        const devicesInGroup = groupDevices[group.deviceGroupID] || [];

        const deviceIdsInGroup = new Set(
          devicesInGroup.map(d => d.deviceID)
        );

        const devicesNotInGroup = allDevices.filter(
          d => !deviceIdsInGroup.has(d.deviceID)
        );

        return (
          <div key={group.deviceGroupID} className="group-card">

            <div className="group-header">
              <strong>{group.groupName}</strong>

              <div>
                {accessLevel >= ACCESS.ELEVATED && (
                  <button onClick={() => toggleExpand(group.deviceGroupID)}>
                    {expanded === group.deviceGroupID ? "Hide" : "Manage"}
                  </button>
                )}

                {accessLevel >= ACCESS.ADMIN && (
                  <button
                    className="danger-btn"
                    onClick={() => deleteGroup(group.deviceGroupID)}
                  >
                    Delete
                  </button>
                )}
              </div>
            </div>

            {expanded === group.deviceGroupID && (
              <div className="group-devices">

                {/* IN GROUP */}
                <div>
                  <h4>In Group</h4>
                  {devicesInGroup.map(device => (
                    <div key={device.deviceID}>
                      {device.deviceName}

                      {accessLevel >= ACCESS.ADMIN && (
                        <button onClick={() =>
                          removeDevice(group.deviceGroupID, device.deviceID)
                        }>
                          Remove
                        </button>
                      )}
                    </div>
                  ))}
                </div>

                {/* NOT IN GROUP */}
                {accessLevel >= ACCESS.ADMIN && (
                  <div>
                    <h4>Add Devices</h4>
                    {devicesNotInGroup.map(device => (
                      <div key={device.deviceID}>
                        {device.deviceName}

                        <button onClick={() =>
                          addDevice(group.deviceGroupID, device.deviceID)
                        }>
                          Add
                        </button>
                      </div>
                    ))}
                  </div>
                )}

              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}


function CreateGroup({ accessLevel }) {
  const [name, setName] = useState("");

  const createGroup = async () => {
    if (!name) return alert("Enter a name");

    await fetch("/group/create", {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        groupName: name,
        gpsAccuracy: 10
      }),
    });

    setName("");
    alert("Group created");
  };

  return (
    <div>
      <h2>Create Group</h2>

      <input
        value={name}
        onChange={(e) => setName(e.target.value)}
        placeholder="Group name"
      />

      <button
        onClick={createGroup}
        disabled={accessLevel < ACCESS.ADMIN}
      >
        Create
      </button>
    </div>
  );
}


function DeviceGroups({ accessLevel }) {
  const [active, setActive] = useState(GROUP_PANELS.OVERVIEW);

  return (
    <div className="org-layout">
      <GroupSidebar
        active={active}
        setActive={setActive}
        accessLevel={accessLevel}
      />

      <div className="org-content">
        {active === GROUP_PANELS.OVERVIEW && (
          <GroupOverview accessLevel={accessLevel} />
        )}

        {active === GROUP_PANELS.CREATE && (
          <CreateGroup accessLevel={accessLevel} />
        )}
      </div>
    </div>
  );
}



// ----------------------------------------------------------------------------------------------

function Map() {

    const [viewMode, setViewMode] = useState("approx"); 
    const [devices, setDevices] = useState([]);
    const [geofences, setGeofences] = useState([]);

    useEffect(() => {
        const fetchFenceData = async () => {
        try {
            const geofencesRes = await fetch("/geofence/geofences", { credentials: "include" });
            const geofencesData = await geofencesRes.json();
            setGeofences(geofencesData);
            console.log("Geofences: ", geofencesData)

        } catch (err) {
            console.error("Failed to fence map data", err);
        }
        };

        fetchFenceData();
    }, []);


    useEffect(() => {
        const fetchDeviceData = async () => {
        try {
            const devicesRes = await fetch("/gps/devices", { credentials: "include" });
            const devicesData = await devicesRes.json();
            setDevices(devicesData);
            console.log("Devices: ", devicesData)

        } catch (err) {
            console.error("Failed to device map data", err);
        }
        };

        fetchDeviceData();
    }, []);




    return(
      <div className="map-wrapper">
        <button onClick={() => setViewMode("polygon")}>
          Polygon view
        </button>
        <button onClick={() => setViewMode("approx")}>
          Approx circles
        </button>


        <MapContainer
          center={[50.375, -4.139]} 
          zoom={13}
          style={{ height: '100vh', width: '100%' }}
        >
          <TileLayer
            attribution='&copy; OpenStreetMap contributors'
            url='https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png'
          />

          {devices.map((device) => (
            <Marker
              key={device.deviceID}
              position={[device.lat, device.lon]}
            >
              <Popup>{device.deviceName}</Popup>
            </Marker>
          ))}


          {geofences.flatMap((fence) => {
            if (!fence.geoJSON) return [];

            let geo;
            try {
              geo = JSON.parse(fence.geoJSON);
            } catch {
              return [];
            }


            // case 1: approx circles
            if (geo.type === "FeatureCollection") {
              if (viewMode !== "approx") return [];

              return geo.features.map((feature, i) => (
                <Circle
                  key={`${fence.geofenceID}-approx-${i}`}
                  center={[
                    feature.geometry.coordinates[1],
                    feature.geometry.coordinates[0],
                  ]}
                  radius={feature.properties.radius}
                  pathOptions={{
                    color: "blue",
                    fillOpacity: 0.15,
                    weight: 1,
                  }}
                >
                  {i === 0 && <Popup>{fence.geofenceName} (approx)</Popup>}
                </Circle>
              ));
            }

            // case 2: polygon
            if (geo.type === "Feature" && geo.geometry?.type === "Polygon") {
              if (viewMode !== "polygon") return [];

              const coords = geo.geometry.coordinates[0].map(([lon, lat]) => [
                lat,
                lon,
              ]);

              return [
                <Circle
                  key={fence.geofenceID + "-poly"}
                  center={coords[0]}
                  radius={0} // dummy (we use polygon instead)
                >
                  <Polygon
                    positions={coords}
                    pathOptions={{
                      color: "red",
                      fillOpacity: 0.2,
                      weight: 2,
                    }}
                  />
                  <Popup>{fence.geofenceName} (polygon)</Popup>
                </Circle>,
              ];
            }

            // case 3: single circle
            if (geo.geometry?.type === "Point" && geo.properties?.radius) {
              if (viewMode !== "approx") return [];

              return [
                <Circle
                  key={fence.geofenceID}
                  center={[
                    geo.geometry.coordinates[1],
                    geo.geometry.coordinates[0],
                  ]}
                  radius={geo.properties.radius}
                  pathOptions={{ color: "red", fillOpacity: 0.3 }}
                >
                  <Popup>{fence.geofenceName}</Popup>
                </Circle>,
              ];
            }

            return [];
          
        })}



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
                      <option value={ACCESS.ELEVATED}>Elevated</option>
                      <option value={ACCESS.USER}>User</option>
                    </>
                  )}
                  {accessLevel === ACCESS.ADMIN && (
                    <>
                      <option value={ACCESS.ELEVATED}>Elevated</option>
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


const ORG_PANELS = {
    USERS: "users",
    DEVICES: "devices",
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
      </div>
    </div>
  );
}


// ----------------------------------------------------------------------------------------------

const ROOT_PANELS = {
    DANGER: "danger"
};

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

function RootSidebar({ active, setActive, accessLevel }) {
  return (
    <div className="org-sidebar">

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


function RootMenu({ accessLevel, refreshAuth }) {
  const [activePanel, setActivePanel] = useState(ROOT_PANELS.DANGER);

  return (
    <div className="org-layout">
      <RootSidebar
        active={activePanel}
        setActive={setActivePanel}
        accessLevel={accessLevel}
      />

      <div className="org-content">
        {activePanel === ROOT_PANELS.DANGER && (
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
        {activeTab === "devices" && <Devices accessLevel={authState.accessLevel}/>}
        {activeTab === "geofences" && <Geofences accessLevel={authState.accessLevel}/>}
        {activeTab === "groups" && <DeviceGroups accessLevel={authState.accessLevel}/>}
        {activeTab === "map" && <Map accessLevel={authState.accessLevel}/>}
        {activeTab === "users" && <Users accessLevel={authState.accessLevel}/>}
        {activeTab === "policies" && <Policies accessLevel={authState.accessLevel}/>}
        {activeTab === "organisation" && <Organisation accessLevel={authState.accessLevel} refreshAuth={refreshAuth}/>}
        {activeTab === "root" && <RootMenu accessLevel={authState.accessLevel} refreshAuth={refreshAuth}/>}
      </div>
    </div>
  );
}
