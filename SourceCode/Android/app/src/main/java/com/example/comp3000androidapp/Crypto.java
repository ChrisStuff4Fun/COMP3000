package com.example.comp3000androidapp;

public class Crypto {

    static {
        System.loadLibrary("seal_jni");
    }

    public native String testInit();

    public String testCall() {
        return testInit();
    }
}