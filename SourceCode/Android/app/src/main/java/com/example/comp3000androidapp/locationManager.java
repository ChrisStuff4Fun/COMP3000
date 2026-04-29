package com.example.comp3000androidapp;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.os.HandlerThread;
import android.os.Looper;
import androidx.core.app.ActivityCompat;
import com.google.android.gms.location.FusedLocationProviderClient;
import com.google.android.gms.location.LocationCallback;
import com.google.android.gms.location.LocationRequest;
import com.google.android.gms.location.LocationResult;
import com.google.android.gms.location.LocationServices;
import com.google.android.gms.location.Priority;

public class locationManager {
    private final FusedLocationProviderClient fusedClient;
    private final LocationCallback callback;
    private final Context context;

    public locationManager (Context context, LocationCallback callback) {
        this.fusedClient = LocationServices.getFusedLocationProviderClient(context);
        this.callback    = callback;
        this.context     = context;
    }

    // startLocationUpdates
    // Called by trackingService in order to begin receiving gps data
    public void startLocationUpdates() {

        // Check permissions before generating some strange errors
        boolean fine = ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED;
        boolean coarse = ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_COARSE_LOCATION) == PackageManager.PERMISSION_GRANTED;

        android.util.Log.d("LocationManager", "fine: " + fine + ", coarse: " + coarse);

        if (!fine && !coarse) {
            android.util.Log.e("LocationManager", "No location permission, returning early");
            return;
        }

        LocationRequest request = new LocationRequest.Builder(Priority.PRIORITY_HIGH_ACCURACY) // Prioritise accuracy, uses all forms of location gathering
                .setIntervalMillis(30000) // Calls an update every 30s
                .setMinUpdateIntervalMillis(15000) // Will wait max 15s for an update (to save power)
                .setMaxUpdateDelayMillis(60000) // Will wait 60s at the very most (low power mode etc)
                .build();

        //fusedClient.requestLocationUpdates(request, callback, Looper.getMainLooper());

        HandlerThread handlerThread = new HandlerThread("LocationThread");
        handlerThread.start();
        fusedClient.requestLocationUpdates(request, callback, handlerThread.getLooper());

        android.util.Log.d("TrackingService", "Location updates requested");
    }





    // removeUpdates
    // Used to kill the loop of location updates
    public void removeUpdates() {
        fusedClient.removeLocationUpdates(callback);
    }


}
