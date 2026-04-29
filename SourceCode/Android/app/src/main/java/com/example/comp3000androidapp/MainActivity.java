package com.example.comp3000androidapp;

import android.Manifest;
import android.app.AlertDialog;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.provider.Settings;
import android.util.Log;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.content.ContextCompat;

public class MainActivity extends AppCompatActivity {

    private ActivityResultLauncher<String[]> permissionLauncher;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        checkPerms(savedInstanceState);

    }

    // onResume
    // Used to refresh permissions state after switching back in to the app
    protected void onResume(Bundle savedInstanceState) {
        super.onResume();
        checkPerms(savedInstanceState);
    }

    // checkPerms
    // Checks if precise location and background location are enabled
    // Either asks user to enable them, or starts background app process
    private void checkPerms( Bundle savedInstanceState ) {
        boolean foreground = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED;
        boolean background = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_BACKGROUND_LOCATION) == PackageManager.PERMISSION_GRANTED;

        if (!foreground || !background) {
            showPermissionsDialog();
        } else {
            startApp(savedInstanceState);
        }
    }

    // showPermissionsDialog
    // Shows an alert with options to change settings or to exit the app
    private void showPermissionsDialog() {
        new AlertDialog.Builder(this)
                .setTitle("Location Permissions Required")
                .setMessage("This app requires location access at all times to function. Please go to Settings → Permissions → Location → Allow all the time. (Requires an app restart)")
                .setPositiveButton("Settings", (dialog, which) -> {
                    Intent intent = new Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS);
                    Uri uri = Uri.fromParts("package", getPackageName(), null);
                    intent.setData(uri);
                    startActivity(intent);

                })
                .setNegativeButton("Exit", (dialog, which) -> finish())
                .setCancelable(false)
                .show();

    }

    // Placeholder function for now
    private void startApp(Bundle savedInstanceState) {

        if (savedInstanceState == null) {
            boolean registered = getSharedPreferences("cybertrackClient", MODE_PRIVATE)
                    .getBoolean("registered", false);

            if (registered) {
                loadTracking();
            } else {
                loadRegister();
            }
        }


        // Start background service
    }



    public void loadRegister() {
        getSupportFragmentManager()
                .beginTransaction()
                .replace(R.id.fragment_container, new RegisterFragment())
                .commit();
    }

    public void loadTracking() {
        android.util.Log.d("MainActivity", "loadTracking called");

        Intent serviceIntent = new Intent(this, trackingService.class);
        android.util.Log.d("MainActivity", "Starting service...");

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            startForegroundService(serviceIntent);
        } else {
            startService(serviceIntent);
        }

        android.util.Log.d("MainActivity", "Service start called");

        getSupportFragmentManager()
                .beginTransaction()
                .replace(R.id.fragment_container, new TrackingFragment())
                .commit();
    }



}
