package com.example.comp3000androidapp;

import static android.content.Context.MODE_PRIVATE;

import android.content.SharedPreferences;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;

public class TrackingFragment extends Fragment {

    TextView orgText;
    TextView deviceText;

    public TrackingFragment() {
        super(R.layout.tracking_fragment);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {

        orgText  = getView().findViewById(R.id.pairedToOrg);
        deviceText = getView().findViewById(R.id.pairedAsName);

        apiManager api = new apiManager();

        SharedPreferences prefs = requireActivity().getSharedPreferences("cybertrackClient", MODE_PRIVATE);

        int deviceId = Integer.parseInt(prefs.getString("device_id", "-1"));

        api.getDeviceInfo(deviceId, new apiManager.DeviceInfoCallback() {

            @Override
            public void onSuccess(apiManager.DeviceInfoResponse response) {

                new Handler(Looper.getMainLooper()).post(() -> {
                    orgText.setText("Device registered to: " + response.orgName);
                    deviceText.setText("Device paired as: " + response.deviceName);
                });

            }

            @Override
            public void onError(Exception e) {
                e.printStackTrace();
            }
        });

    }




}
