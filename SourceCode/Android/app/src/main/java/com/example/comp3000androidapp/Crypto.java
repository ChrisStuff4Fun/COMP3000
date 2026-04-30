package com.example.comp3000androidapp;

public class Crypto {

    static {
        System.loadLibrary("seal_jni");
    }

    // test that CPP has loaded (not really needed anymore)
    public native String testInit();
    public String testCall() {
        return testInit();
    }


    public native String encryptValue(String base64PublicKey, double value);

    public String[] encryptLocation(String base64PublicKey, double lat, double lon) {
        return new String[]{
                encryptValue(base64PublicKey, lat),
                encryptValue(base64PublicKey, lon)
        };
    }

    public native String getSealDebugInfo();


}