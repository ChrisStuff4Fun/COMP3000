package com.example.comp3000androidapp;

import android.content.Context;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;

public class RegisterFragment extends Fragment {

    private EditText codeInput;
    private Button registerButton;

    public RegisterFragment() {
        super(R.layout.register_fragment);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        codeInput = view.findViewById(R.id.codeInput);
        registerButton = view.findViewById(R.id.registerButton);

        registerButton.setOnClickListener(v -> {
            String code = codeInput.getText().toString().trim();

            if (code.isEmpty()) {
                Toast.makeText(getContext(), "Enter a code", Toast.LENGTH_SHORT).show();
                return;
            }

            registerDevice(code);
        });
    }

    private void registerDevice(String code) {

        registerButton.setEnabled(false);
        encryptionManager crypto = encryptionManager.getInstance();

        String deviceName = android.os.Build.MODEL;

        apiManager api = new apiManager();

        api.registerDeviceKey(
                code,
                deviceName,
                crypto.getX(),
                crypto.getY(),
                new apiManager.RegisterCallback() {

                    @Override
                    public void onSuccess(String deviceId) {

                        new Handler(Looper.getMainLooper()).post(() -> {
                            Toast.makeText(getContext(), "Registered!", Toast.LENGTH_SHORT).show();
                            ((MainActivity) requireActivity()).loadTracking();
                        });

                        // store returned deviceId locally
                        SharedPreferences prefs = requireActivity().getSharedPreferences("cybertrackClient", Context.MODE_PRIVATE);

                        prefs.edit()
                                .putBoolean("registered", true)
                                .putString("device_id", deviceId)
                                .apply();
                    }

                    @Override
                    public void onFailure(int statusCode, String errorMessage) {

                        new Handler(Looper.getMainLooper()).post(() -> {
                            Toast.makeText(getContext(), "Error " + statusCode + ": " + errorMessage, Toast.LENGTH_LONG).show();
                            registerButton.setEnabled(true);
                        });
                    }

                    @Override
                    public void onError(Exception e) {

                        new Handler(Looper.getMainLooper()).post(() -> {
                            Toast.makeText(getContext(), "Registration failed", Toast.LENGTH_SHORT).show();
                            registerButton.setEnabled(true);
                        });

                        e.printStackTrace();
                    }
                }
        );

    }
}