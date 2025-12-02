package com.example.comp3000androidapp;

import android.Manifest;
import android.app.AlertDialog;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.provider.Settings;

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


        checkPerms();

    }

    // onResume
    // Used to refresh permissions state after switching back in to the app
    protected void onResume() {
        super.onResume();
        checkPerms();
    }

    // checkPerms
    // Checks if precise location and background location are enabled
    // Either asks user to enable them, or starts background app process
    private void checkPerms() {
        boolean foreground = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED;
        boolean background = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_BACKGROUND_LOCATION) == PackageManager.PERMISSION_GRANTED;

        if (!foreground || !background) {
            showPermissionsDialog();
        } else {
            startApp();
        }
    }

    // showPermissionsDialog
    // Shows an alert with options to change settings or to exit the app
    private void showPermissionsDialog() {
        new AlertDialog.Builder(this)
                .setTitle("Location Permissions Required")
                .setMessage("This app requires location access at all times to function. Please go to Settings → Permissions → Location → Allow all the time.")
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
    private void startApp() {
        // Start background service
    }
}
