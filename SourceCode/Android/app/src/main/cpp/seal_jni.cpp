#include <jni.h>

extern "C"
JNIEXPORT jstring JNICALL
Java_com_example_comp3000androidapp_Crypto_testInit(JNIEnv* env, jobject thiz) {
    return env->NewStringUTF("Hello from C++");
}