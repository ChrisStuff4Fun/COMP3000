import React, { useState } from "react";

export default function OrgPage({ refreshAuth }) {
  const [orgName, setOrgName] = useState("");
  const [joinCode, setJoinCode] = useState("");

  const createOrg = async () => {
    if (!orgName.trim()) return alert("Enter an organisation name");

    const res = await fetch(`/orgs/create/${orgName}`, {
      method: "POST",
      credentials: "include"
    });

    if (res.ok) refreshAuth();
    else alert("Failed to create organisation");
  };

  const joinOrg = async () => {
    if (!joinCode.trim()) return alert("Enter a join code");

    const res = await fetch(`/user/register/${joinCode}`, {
      method: "POST",
      credentials: "include"
    });

    if (res.ok) refreshAuth();
    else alert("Invalid join code");
  };

  return (
    <div className="org-page">
      <div className="org-panel left">
        <h2>Create Organisation</h2>
        <input
          type="text"
          placeholder="Organisation name"
          value={orgName}
          onChange={(e) => setOrgName(e.target.value)}
        />
        <button onClick={createOrg}>Create</button>
      </div>

      <div className="org-panel right">
        <h2>Join Organisation</h2>
        <input
          type="text"
          placeholder="Join code"
          value={joinCode}
          onChange={(e) => setJoinCode(e.target.value)}
        />
        <button onClick={joinOrg}>Join</button>
      </div>
    </div>
  );
}
