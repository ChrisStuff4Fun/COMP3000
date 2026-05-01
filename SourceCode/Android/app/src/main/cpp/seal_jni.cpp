#include <jni.h>
#include "seal/seal.h"
#include <sstream>
#include <android/log.h>

using namespace seal;


// decoder for turning b64 back into binary
static std::string base64Decode(const std::string& input)
{
    static const std::string chars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    std::string out;
    std::vector<int> T(256, -1);
    for (int i = 0; i < 64; i++) T[chars[i]] = i;
    int val = 0, valb = -8;
    for (unsigned char c : input) {
        if (T[c] == -1) continue;
        val = (val << 6) + T[c];
        valb += 6;
        if (valb >= 0) {
            out.push_back(char((val >> valb) & 0xFF));
            valb -= 8;
        }
    }
    return out;
}

extern "C"
JNIEXPORT jstring JNICALL
Java_com_example_comp3000androidapp_Crypto_testInit(JNIEnv* env, jobject thiz) {
    return env->NewStringUTF("Hello from C++");
}

extern "C"
JNIEXPORT jstring JNICALL
Java_com_example_comp3000androidapp_Crypto_encryptValue(JNIEnv* env, jobject thiz, jstring base64PublicKey, double value)
{
    try
    {

        // set up same SEAL params as server
        EncryptionParameters parms(scheme_type::bfv);
        size_t poly_modulus_degree = 8192;
        parms.set_poly_modulus_degree(poly_modulus_degree);
        parms.set_plain_modulus(4398046150657ULL);
        parms.set_coeff_modulus(CoeffModulus::BFVDefault(poly_modulus_degree));


        SEALContext context(parms);
        auto id = context.key_context_data()->parms_id();
        std::string idStr;
        for (size_t i = 0; i < id.size(); i++) {
            char buf[32];
            snprintf(buf, sizeof(buf), "%016llx", (unsigned long long)id[i]);
            idStr += buf;
            if (i + 1 < id.size()) idStr += "-";
        }
        __android_log_print(ANDROID_LOG_DEBUG, "SEAL_JNI", "parms_id: %s", idStr.c_str());

        uint64_t plainMod = parms.plain_modulus().value();
        __android_log_print(ANDROID_LOG_DEBUG, "SEAL_JNI", "plain_modulus: %llu", plainMod);

        // decode base64 public key from server
        const char* b64chars = env->GetStringUTFChars(base64PublicKey, nullptr);
        std::string b64str(b64chars);
        env->ReleaseStringUTFChars(base64PublicKey, b64chars);


        std::string keyBytes = base64Decode(b64str);
        std::stringstream keyStream(std::ios::in | std::ios::out | std::ios::binary);
        keyStream.write(keyBytes.data(), keyBytes.size());
        keyStream.seekg(0);

        __android_log_print(ANDROID_LOG_DEBUG, "SEAL_JNI", "b64str length: %zu", b64str.length());
        __android_log_print(ANDROID_LOG_DEBUG, "SEAL_JNI", "keyBytes length: %zu", keyBytes.length());
        __android_log_print(ANDROID_LOG_DEBUG, "SEAL_JNI", "first 4 bytes: %02x %02x %02x %02x",
                            (unsigned char)keyBytes[0], (unsigned char)keyBytes[1],
                            (unsigned char)keyBytes[2], (unsigned char)keyBytes[3]);

        PublicKey pk;
        pk.load(context, keyStream);

        // scale double to integer (multiply by 1e6 for 6 decimal places)
        BatchEncoder encoder(context);
        int64_t scaled = static_cast<int64_t>(value * 1e6);
        std::vector<int64_t> vec(poly_modulus_degree, 0);
        vec[0] = scaled;
        Plaintext plain;
        encoder.encode(vec, plain);



        // encrypt
        Encryptor encryptor(context, pk);
        Ciphertext cipher;
        encryptor.encrypt(plain, cipher);

        // serialise and base64 encode
        std::ostringstream cipherStream(std::ios::binary);
        cipher.save(cipherStream, compr_mode_type::none);

        // base64 encode output
        const std::string& bytes = cipherStream.str();
        static const char table[] =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        std::string encoded;
        int val = 0, valb = -6;
        for (unsigned char c : bytes) {
            val = (val << 8) + c;
            valb += 8;
            while (valb >= 0) {
                encoded.push_back(table[(val >> valb) & 0x3F]);
                valb -= 6;
            }
        }
        if (valb > -6) encoded.push_back(table[((val << 8) >> (valb + 8)) & 0x3F]);
        while (encoded.size() % 4) encoded.push_back('=');

        return env->NewStringUTF(encoded.c_str());

    }
    catch (const std::exception& e)
    {
        // catch error and send back to java
        __android_log_print(ANDROID_LOG_ERROR, "SEAL_JNI", "encryptValue failed: %s", e.what());
        return env->NewStringUTF((std::string("ERROR: ") + e.what()).c_str());
    }

}


extern "C"
JNIEXPORT jstring JNICALL
Java_com_example_comp3000androidapp_Crypto_getSealDebugInfo(JNIEnv *env, jobject thiz)
{
    static std::string info;

    info =
            std::string("SEAL: ") +
            std::to_string(SEAL_VERSION_MAJOR) + "." +
            std::to_string(SEAL_VERSION_MINOR) + "." +
            std::to_string(SEAL_VERSION_PATCH) +
            " | COMPILED: " + __DATE__ + " " + __TIME__;

    return env->NewStringUTF(info.c_str());
}