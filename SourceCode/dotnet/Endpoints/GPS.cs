using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public static class GPSEndpoints
{
    public static void MapGPSEndpoints(this IEndpointRouteBuilder app)
    {
        var fences = app.MapGroup("/gps");

        // Map endpoints
        fences.MapPost("/update/{deviceId}", updateGPS);
        fences.MapGet("/devices", getTrackableDevices);
        fences.MapGet("/debug", testFunc);
    
    }


    // Methods for endpoints


    private static async Task<IResult> testFunc(AppDbContext db)
    {
        
        if (!SealNative.initSeal())
            return Results.Problem("SEAL init failed", statusCode: 500);
        // encrypt 12345678 (represents 12.345678 * 1e6)
        var ptr = SealNative.debugEncryptAndDecrypt(12345678);
        long result = ptr;
        return Results.Ok(new { encrypted_then_decrypted = result, expected = 12345678 });
    }


    private static async Task<IResult> updateGPS(int deviceId, GPSUpdate update, AppDbContext db)
    {
        try
        {
            // get device
            Device? device = await db.Devices.FindAsync(deviceId);
            if (device == null) return Results.NotFound("Device not found");

            // initialise SEAL
            if (!SealNative.initSeal())
                return Results.Problem("SEAL init failed", statusCode: 500);

            // get device groups for this device
            List<DeviceDeviceGroupLink> groupLinks = await db.Devices_DeviceGroup_Link
                .Where(l => l.DeviceID == deviceId)
                .ToListAsync();

            // get all policies applicable to these groups
            var groupIds = groupLinks.Select(l => l.DeviceGroupID).ToList();
            List<Policy> policies = await db.Policies
                .Where(p => groupIds.Contains(p.DeviceGroupID))
                .ToListAsync();

            // process each policy
            bool anyTrackingAllowed = false;

            foreach (Policy policy in policies)
            {
                // get geofence for this policy
                Geofence? geofence = await db.Geofences.FindAsync(policy.GeofenceID);
                if (geofence == null) continue;

                // get or create policy status for this device/policy
                DevicePolicyStatus? status = await db.DevicePolicyStatus
                    .FirstOrDefaultAsync(s => s.DeviceID == deviceId && s.PolicyID == policy.PolicyID);

                if (status == null)
                {
                    status = new DevicePolicyStatus
                    {
                        DeviceID = deviceId,
                        PolicyID = policy.PolicyID,
                        OrgID = device.OrgID,
                        IsInsideFence = false,
                        AlertOnEnterTriggered = false,
                        AlertOnLeaveTriggered = false,
                        LastUpdated = DateTime.UtcNow
                    };
                    db.DevicePolicyStatus.Add(status);
                }

                // compute if device is inside geofence using FHE
                bool isInside = await isInsideFenceAsync(update.Lat, update.Lon, geofence);

                bool wasInside = status.IsInsideFence;
                status.IsInsideFence = isInside;
                status.LastUpdated = DateTime.UtcNow;

                // handle alert rules
                if (isInside && !wasInside)
                {
                    // device just entered
                    if (policy.AlertOnEnterRule)
                        status.AlertOnEnterTriggered = true;

                    status.AlertOnLeaveTriggered = false; // reset leave alert
                }
                else if (!isInside && wasInside)
                {
                    // device just left
                    if (policy.AlertOnLeaveRule)
                        status.AlertOnLeaveTriggered = true;

                    status.AlertOnEnterTriggered = false; // reset enter alert
                }

                // check tracking rules
                bool shouldTrack = ( (isInside && policy.TrackInsideFenceRule) || (!isInside && policy.TrackOutsideFenceRule) );

                if (shouldTrack) anyTrackingAllowed = true;
            }

            // update device location only if tracking is allowed by at least one policy
            // if no policies apply, always track
            if (anyTrackingAllowed || !policies.Any())
            {
                device.LastLoggedLat = update.Lat;
                device.LastLoggedLong = update.Lon;
            }

            await db.SaveChangesAsync();
            return Results.Ok();
        }
        catch (Exception e)
        {
            return Results.Problem(detail: e.ToString(), statusCode: 500);
        }
    }

// isInsideFenceAsync - responsible for breaking down the geoJSON into usable numbers 
    private static async Task<bool> isInsideFenceAsync(string encLat, string encLon, Geofence geofence)
    {
        try
        {
            var geo = JsonSerializer.Deserialize<JsonElement>(geofence.GeoJSON);

            // handle FeatureCollection (approximated polygon)
            if (geo.GetProperty("type").GetString() == "FeatureCollection")
            {
                var features = geo.GetProperty("features").EnumerateArray();
                foreach (var feature in features)
                {

                    // check if current item is the original polygon, if it is, skip
                    string? geoType = feature.GetProperty("geometry").GetProperty("type").GetString();
                    if (geoType != "Point") continue;

                    // extract coordinates and radius as doubles
                    var coords = feature.GetProperty("geometry").GetProperty("coordinates");
                    double centreLon = coords[0].GetDouble();
                    double centreLat = coords[1].GetDouble();
                    double radius = feature.GetProperty("properties").GetProperty("radius").GetDouble();

                    // compute 
                    if (isInsideCircleFHE(encLat, encLon, centreLat, centreLon, radius))
                        return true;
                }
                return false;
            }

            // handle single circle (Point with radius)
            if (geo.GetProperty("type").GetString() == "Feature")
            {
                var coords = geo.GetProperty("geometry").GetProperty("coordinates");
                double centreLon = coords[0].GetDouble();
                double centreLat = coords[1].GetDouble();
                double radius = geo.GetProperty("properties").GetProperty("radius").GetDouble();
                return isInsideCircleFHE(encLat, encLon, centreLat, centreLon, radius);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }


// isInsideCircleFHE - uses SEAL to compute if a given BFV encrypted coordinate lies inside non-encrypted circle params
    private static bool isInsideCircleFHE( string encLat, string encLon, double centreLat, double centreLon, double radiusMeters)
    {
        // compute (encLat - centreLat)^2 and (encLon - centreLon)^2 homomorphically
        IntPtr latDiffPtr = SealNative.computeSquaredDiff(encLat, centreLat);
        string latDiffB64 = Marshal.PtrToStringAnsi(latDiffPtr)!;

        IntPtr lonDiffPtr = SealNative.computeSquaredDiff(encLon, centreLon);
        string lonDiffB64 = Marshal.PtrToStringAnsi(lonDiffPtr)!;

        // add and decrypt, result is squared distance in scaled integer space
        long squaredDistScaled = SealNative.addAndDecrypt(latDiffB64, lonDiffB64);

        if (squaredDistScaled < 0) return false; // SEAL error, would rather have false negatives than false positives

        // convert radius to scaled integer space
        // lat/lon scaled by 1e6, 1 degree lat ≈ 111320m
        // so radius in degrees = radiusMeters / 111320
        // scaled: radiusScaled = (radiusMeters / 111320) * 1e6
        double radiusScaled = (radiusMeters / 111320.0) * 1e6;
        double radiusSquaredScaled = radiusScaled * radiusScaled;

        return squaredDistScaled <= radiusSquaredScaled;
    }



    // get function for maps
    private static async Task<IResult> getTrackableDevices(AppDbContext db,IHttpContextAccessor httpAccessor, IDataProtector dataProtector)
    {
        try
        {
            // auth check
            CurrentUser currentUser = new CurrentUser(db, httpAccessor, dataProtector);
            if (!currentUser.validateToken()) return Results.Unauthorized();
            await currentUser.getUserFromDBAsync();
            if (!currentUser.isRegistered() || !currentUser.hasAccessLevel(2)) return Results.Problem("Forbidden", statusCode: 403);

            // get all devices for this org
            List<Device> devices = await db.Devices.Where(d => d.OrgID == currentUser.OrgID).ToListAsync();

            if (!devices.Any()) return Results.Ok(new List<object>());

            // initialise SEAL for decryption
            if (!SealNative.initSeal()) return Results.Problem("SEAL init failed", statusCode: 500);

            var result = new List<object>();

            foreach (Device device in devices)
            {
                // skip devices with no location yet
                if (string.IsNullOrEmpty(device.LastLoggedLat) || string.IsNullOrEmpty(device.LastLoggedLong)) continue;

                // get all policy statuses for this device
                List<DevicePolicyStatus> statuses = await db.DevicePolicyStatus.Where(s => s.DeviceID == device.DeviceID).ToListAsync();

                // if no policies apply, always show the device
                if (!statuses.Any())
                {
                    var decryptedNoPolicy = decryptLocation(device.LastLoggedLat, device.LastLoggedLong);
                    if (decryptedNoPolicy == null) continue;
                    result.Add(new
                    {
                        deviceId = device.DeviceID,
                        deviceName = device.DeviceName,
                        lat = decryptedNoPolicy.Value.lat,
                        lon = decryptedNoPolicy.Value.lon
                    });
                    continue;
                }

                // check if any policy allows tracking at current position
                bool shouldShow = false;
                foreach (DevicePolicyStatus status in statuses)
                {
                    Policy? policy = await db.Policies.FindAsync(status.PolicyID);
                    if (policy == null) continue;

                    bool trackable = (status.IsInsideFence && policy.TrackInsideFenceRule) || (!status.IsInsideFence && policy.TrackOutsideFenceRule);

                    if (trackable)
                    {
                        shouldShow = true;
                        break;
                    }
                }

                if (!shouldShow) continue;

                // decrypt location for map display
                var decrypted = decryptLocation(device.LastLoggedLat, device.LastLoggedLong);
                if (decrypted == null) continue;

                result.Add(new
                {
                    deviceId = device.DeviceID,
                    deviceName = device.DeviceName,
                    lat = decrypted.Value.lat,
                    lon = decrypted.Value.lon
                });
            }

            return Results.Ok(result);
        }
        catch (Exception e)
        {
            return Results.Problem(detail: e.ToString(), statusCode: 500);
        }
    }


    // SEAL helper function
    private static (double lat, double lon)? decryptLocation(string encLat, string encLon)
    {
        try
        {
            long scaledLat = SealNative.decryptValue(encLat);
            long scaledLon = SealNative.decryptValue(encLon);

            if (scaledLat == long.MinValue || scaledLon == long.MinValue) return null;

            double lat = scaledLat / 1e6;
            double lon = scaledLon / 1e6;

            return (lat, lon);
        }
        catch
        {
            return null;
        }
    }


}



