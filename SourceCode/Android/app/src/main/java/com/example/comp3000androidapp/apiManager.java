package com.example.comp3000androidapp;

import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Log;
import org.json.JSONObject;
import java.io.OutputStream;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.security.InvalidAlgorithmParameterException;
import java.security.KeyPairGenerator;
import java.security.KeyStore;
import java.security.KeyStoreException;
import java.security.NoSuchAlgorithmException;
import java.security.NoSuchProviderException;
import java.security.cert.CertificateException;
import java.security.spec.ECGenParameterSpec;
import java.security.KeyStore;
import java.security.interfaces.ECPublicKey;
import java.security.spec.ECPoint;
import com.example.comp3000androidapp.Crypto;
public class apiManager {

    private final String baseURL = "https://cybertrack.azurewebsites.net";



    public static class DHPublicKeyResponse {
        public String x;
        public String y;
    }

    public interface DHKeyCallback {
        void onSuccess(DHPublicKeyResponse key);
        void onError(Exception e);
    }

    public void fetchServerDHKey(DHKeyCallback callback) {

        new Thread(() -> {
            try {
                URL url = new URL(baseURL + "/keys/public");
                HttpURLConnection conn = (HttpURLConnection) url.openConnection();

                conn.setRequestMethod("GET");
                conn.setConnectTimeout(5000);
                conn.setReadTimeout(5000);

                int status = conn.getResponseCode();

                if (status != HttpURLConnection.HTTP_OK) {
                    throw new RuntimeException("HTTP error: " + status);
                }

                BufferedReader reader = new BufferedReader(
                        new InputStreamReader(conn.getInputStream())
                );

                StringBuilder response = new StringBuilder();
                String line;

                while ((line = reader.readLine()) != null) {
                    response.append(line);
                }

                reader.close();
                conn.disconnect();

                JSONObject json = new JSONObject(response.toString());

                DHPublicKeyResponse key = new DHPublicKeyResponse();
                key.x = json.getString("x");
                key.y = json.getString("y");

                callback.onSuccess(key);

            } catch (Exception e) {
                callback.onError(e);
            }
        }).start();
    }




    public interface bfvKeyCallback {
        void onSuccess(String key);
        void onError(Exception e);
    }

    public void fetchServerBfvKey(bfvKeyCallback callback) {

        new Thread(() -> {
            try {
                URL url = new URL(baseURL + "/keys/bfv");
                HttpURLConnection conn = (HttpURLConnection) url.openConnection();

                conn.setRequestMethod("GET");
                conn.setConnectTimeout(5000);
                conn.setReadTimeout(5000);

                int status = conn.getResponseCode();

                if (status != HttpURLConnection.HTTP_OK) {
                    throw new RuntimeException("HTTP error: " + status);
                }

                BufferedReader reader = new BufferedReader(
                        new InputStreamReader(conn.getInputStream())
                );

                StringBuilder response = new StringBuilder();
                String line;

                while ((line = reader.readLine()) != null) {
                    response.append(line);
                }

                reader.close();
                conn.disconnect();

                JSONObject json = new JSONObject(response.toString());


                String key = json.getString("publicBFV");


                callback.onSuccess(key);

                Log.d("BFV", "fetchServerBfvKey: fetched " + key);

                Crypto c = new Crypto();
                Log.d("test", "seal" + c.getSealDebugInfo());

            } catch (Exception e) {
                callback.onError(e);
            }
        }).start();
    }



    public interface RegisterCallback {
        void onSuccess(String deviceId);
        void onFailure(int statusCode, String errorMessage);
        void onError(Exception e);
    }

    public void registerDeviceKey(String code, String deviceName, String x, String y, RegisterCallback callback) {

        new Thread(() -> {
            HttpURLConnection conn = null;

            try {
                URL url = new URL(baseURL + "/keys/register");
                conn = (HttpURLConnection) url.openConnection();

                conn.setRequestMethod("POST");
                conn.setRequestProperty("Content-Type", "application/json; charset=UTF-8");
                conn.setConnectTimeout(5000);
                conn.setReadTimeout(5000);
                conn.setDoOutput(true);

                // build json obj
                JSONObject json = new JSONObject();
                json.put("Code", code);
                json.put("DeviceName", deviceName);
                json.put("X", x);
                json.put("Y", y);

                OutputStream os = conn.getOutputStream();
                os.write(json.toString().getBytes("UTF-8"));
                os.close();

                int status = conn.getResponseCode();

                BufferedReader reader;

                // handle good status codes (should only ever be 200, but just in case)
                if (status >= 200 && status < 300) {
                    reader = new BufferedReader(new InputStreamReader(conn.getInputStream()));
                } else {
                    reader = new BufferedReader(new InputStreamReader(conn.getErrorStream()));
                }

                StringBuilder response = new StringBuilder();
                String line;

                while ((line = reader.readLine()) != null) {
                    response.append(line);
                }

                reader.close();
                conn.disconnect();

                if (status >= 200 && status < 300) {
                    // extract returned deviceId
                    JSONObject obj = new JSONObject(response.toString());
                    String deviceId = obj.getString("deviceId");

                    callback.onSuccess(deviceId);
                } else {
                    // pass server message back

                    String errorText = response.toString();
                    callback.onFailure(status, errorText);
                }

            } catch (Exception e) {
                if (conn != null) conn.disconnect();
                callback.onError(e);
            }
        }).start();
    }


    public void sendLocation(String deviceId, String lat, String lon) {

        new Thread(() -> {
            try {

                URL url = new URL(baseURL + "/gps/update/" + deviceId);
                HttpURLConnection conn = (HttpURLConnection) url.openConnection();

                conn.setRequestMethod("POST");
                conn.setRequestProperty("Content-Type", "application/json");
                conn.setDoOutput(true);

                JSONObject json = new JSONObject();
                json.put("lat", lat);
                json.put("lon", lon);

                Log.d("API", "lat: " + lat);
                Log.d("API", "lon: " + lon);

                OutputStream os = conn.getOutputStream();
                os.write(json.toString().getBytes());
                os.close();

                conn.getResponseCode();
                conn.disconnect();

                Log.d("API", "sendLocation: success");

            } catch (Exception e) {
                e.printStackTrace();
            }
        }).start();
    }


    public static class DeviceInfoResponse {
        public String orgName;
        public String deviceName;
    }

    public interface DeviceInfoCallback {
        void onSuccess(DeviceInfoResponse response);
        void onError(Exception e);
    }

    public void getDeviceInfo(int deviceId, DeviceInfoCallback callback) {

        new Thread(() -> {
            HttpURLConnection conn = null;

            try {
                URL url = new URL(baseURL + "/device/getinfo/" + deviceId);
                conn = (HttpURLConnection) url.openConnection();

                conn.setRequestMethod("GET");
                conn.setConnectTimeout(5000);
                conn.setReadTimeout(5000);

                int status = conn.getResponseCode();

                BufferedReader reader;

                if (status >= 200 && status < 300) {
                    reader = new BufferedReader(new InputStreamReader(conn.getInputStream()));
                } else {
                    reader = new BufferedReader(new InputStreamReader(conn.getErrorStream()));
                }

                StringBuilder response = new StringBuilder();
                String line;

                while ((line = reader.readLine()) != null) {
                    response.append(line);
                }

                reader.close();
                conn.disconnect();

                if (status >= 200 && status < 300) {

                    JSONObject json = new JSONObject(response.toString());

                    DeviceInfoResponse result = new DeviceInfoResponse();
                    result.orgName = json.getString("orgName");
                    result.deviceName = json.getString("deviceName");

                    callback.onSuccess(result);

                } else {
                    throw new RuntimeException("HTTP " + status + ": " + response);
                }

            } catch (Exception e) {
                if (conn != null) conn.disconnect();
                callback.onError(e);
            }
        }).start();
    }


}
