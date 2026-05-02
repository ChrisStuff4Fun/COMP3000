package com.example.comp3000androidapp;

import android.Manifest;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.location.Location;
import android.os.Build;
import android.os.IBinder;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.core.app.ActivityCompat;
import androidx.core.app.NotificationCompat;

import com.example.comp3000androidapp.Crypto;
import com.example.comp3000androidapp.R;
import com.example.comp3000androidapp.apiManager;
import com.example.comp3000androidapp.locationManager;
import com.google.android.gms.location.LocationCallback;
import com.google.android.gms.location.LocationResult;
import com.google.android.gms.location.Priority;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;

public class trackingService extends Service {

    private com.google.android.gms.location.FusedLocationProviderClient fusedClient;
    private static final String CHANNEL_ID = "tracking_channel";
    private LocationCallback callback;
    private locationManager locationManager;
    private String cachedBfvKey = null;
    private final Crypto crypto = new Crypto();
    private final apiManager api = new apiManager();

    public void onCreate() {
        super.onCreate();
        android.util.Log.d("TrackingService", "Service started");

        fusedClient = com.google.android.gms.location.LocationServices.getFusedLocationProviderClient(this);

        createNotificationChannel();
        Notification notification = new NotificationCompat.Builder(this, CHANNEL_ID)
                .setContentTitle("CyberTrack")
                .setContentText("Location tracking active")
                .setSmallIcon(R.drawable.ic_launcher_foreground)
                .build();
        startForeground(1, notification);

        callback = new LocationCallback() {
            @Override
            public void onLocationResult(@NonNull LocationResult result) {
                android.util.Log.d("TrackingService", "Location result received");
                Location location = result.getLastLocation();
                if (location == null) return;

                SharedPreferences prefs = getApplicationContext()
                        .getSharedPreferences("cybertrackClient", Context.MODE_PRIVATE);
                String deviceId = prefs.getString("device_id", null);

                new Thread(() -> {
                    String[] encrypted = crypto.encryptLocation(cachedBfvKey,
                            location.getLatitude(), location.getLongitude());
                    api.sendLocation(deviceId, encrypted[0], encrypted[1]);
                }).start();
            }
        };

        // load BFV key from assets, then start location updates
        cachedBfvKey = loadBfvKeyFromAssets();
        if (cachedBfvKey == null) {
            android.util.Log.e("TrackingService", "Failed to load BFV key from assets");
            return;
        }
        android.util.Log.d("TrackingService", "BFV key loaded, length: " + cachedBfvKey.length());

        locationManager = new locationManager(this, callback);
        locationManager.startLocationUpdates();

        // fallback timer for emulator
        java.util.Timer timer = new java.util.Timer();
        timer.schedule(new java.util.TimerTask() {
            @Override
            public void run() {
                if (ActivityCompat.checkSelfPermission(trackingService.this,
                        Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED) return;

                fusedClient.getCurrentLocation(Priority.PRIORITY_HIGH_ACCURACY, null).addOnSuccessListener(location -> {
                    if (location == null) {
                        android.util.Log.w("TrackingService", "Last location null");
                        return;
                    }

                    String deviceId = getApplicationContext()
                            .getSharedPreferences("cybertrackClient", Context.MODE_PRIVATE)
                            .getString("device_id", null);

                    var lat = location.getLatitude();
                    var lon = location.getLongitude();

                    Log.d("Location", "lat: " + lat);
                    Log.d("Location", "lon: " + lon);

                    new Thread(() -> {
                        String[] encrypted = crypto.encryptLocation(cachedBfvKey,
                                lat, lon);
                        api.sendLocation(deviceId, encrypted[0], encrypted[1]);
                    }).start();
                });
            }
        }, 0, 30000);
    }

    private String loadBfvKeyFromAssets() {
        try {
            InputStream is = getAssets().open("bfv-public.txt");
            BufferedReader reader = new BufferedReader(new InputStreamReader(is));
            StringBuilder sb = new StringBuilder();
            String line;
            while ((line = reader.readLine()) != null) {
                sb.append(line);
            }
            reader.close();
            return sb.toString();
        } catch (Exception e) {
            android.util.Log.e("TrackingService", "Failed to load BFV key from assets: " + e.getMessage());
            return null;
        }
    }

    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(
                    CHANNEL_ID,
                    "Location Tracking",
                    NotificationManager.IMPORTANCE_LOW
            );
            NotificationManager manager = getSystemService(NotificationManager.class);
            manager.createNotificationChannel(channel);
        }
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        return START_STICKY;
    }

    @Nullable
    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }
}