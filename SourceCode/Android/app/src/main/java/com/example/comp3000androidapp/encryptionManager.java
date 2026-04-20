package com.example.comp3000androidapp;

import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;

import java.security.KeyPairGenerator;
import java.security.KeyStore;
import java.security.interfaces.ECPublicKey;
import java.security.spec.ECGenParameterSpec;
import java.security.spec.ECPoint;
import javax.crypto.Cipher;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.SecretKeySpec;
import android.util.Base64;
import java.security.SecureRandom;

public class encryptionManager {

    private static encryptionManager instance;

    public static encryptionManager getInstance() {
        if (instance == null) {
            instance = new encryptionManager();
        }
        return instance;
    }

    private boolean isInitialised = false;
    private String localX;
    private String localY;
    private boolean isReady = false;

    public String getX() { return localX; }
    public String getY() { return localY; }
    public boolean isReady() { return isReady; }


    public interface InitCallback {
        void onReady(String x, String y);
        void onError(Exception e);
    }

    public void generateLocalKeys(InitCallback callback) {

        if (isInitialised) {
            callback.onReady(localX, localY);
            return;
        }

        new Thread(() -> {
            try {

                KeyStore keyStore = KeyStore.getInstance("AndroidKeyStore");
                keyStore.load(null);

                String alias = "cybertrack_p384_key";

                // create keys if missing
                if (!keyStore.containsAlias(alias)) {

                    KeyPairGenerator kpg = KeyPairGenerator.getInstance(
                            KeyProperties.KEY_ALGORITHM_EC,
                            "AndroidKeyStore"
                    );

                    KeyGenParameterSpec spec = new KeyGenParameterSpec.Builder(
                            alias,
                            KeyProperties.PURPOSE_AGREE_KEY
                    )
                            .setAlgorithmParameterSpec(new ECGenParameterSpec("secp384r1"))
                            .setDigests(KeyProperties.DIGEST_SHA256, KeyProperties.DIGEST_SHA384)
                            .build();

                    kpg.initialize(spec);
                    kpg.generateKeyPair();
                }

                // load key from keystore
                KeyStore.Entry entry = keyStore.getEntry(alias, null);

                ECPublicKey pub =
                        (ECPublicKey) ((KeyStore.PrivateKeyEntry) entry)
                                .getCertificate()
                                .getPublicKey();

                ECPoint w = pub.getW();

                // store kwys locally
                localX = w.getAffineX().toString();
                localY = w.getAffineY().toString();

                isReady = true;

                callback.onReady(localX, localY);

            } catch (Exception e) {
                callback.onError(e);
            }
        }).start();
    }



    public String encrypt(String data) {
        try {
            byte[] key = getDerivedKey();
            // ⚠️ this should come from ECDH + HKDF later

            SecretKeySpec secretKey = new SecretKeySpec(key, "AES");
            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");

            byte[] iv = new byte[12];
            new SecureRandom().nextBytes(iv);

            GCMParameterSpec spec = new GCMParameterSpec(128, iv);
            cipher.init(Cipher.ENCRYPT_MODE, secretKey, spec);

            byte[] encrypted = cipher.doFinal(data.getBytes());

            byte[] combined = new byte[iv.length + encrypted.length];

            System.arraycopy(iv, 0, combined, 0, iv.length);
            System.arraycopy(encrypted, 0, combined, iv.length, encrypted.length);

            return Base64.encodeToString(combined, Base64.NO_WRAP);

        } catch (Exception e) {
            e.printStackTrace();
            return null;
        }
    }


    public EncryptedLocation encryptLocation(double lat, double lon) {

        EncryptedLocation result = new EncryptedLocation();

        result.lat = encrypt(String.valueOf(lat));
        result.lon = encrypt(String.valueOf(lon));

        return result;
    }

    public static class EncryptedLocation {
        public String lat;
        public String lon;
    }

}
