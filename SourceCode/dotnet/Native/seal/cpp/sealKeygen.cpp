#include "seal/seal.h"
#include <sstream>
#include <string>
#include "base64.h"

using namespace seal;

// import functions from wrapper
extern "C" SEALContext* getContext();


extern "C" __declspec(dllexport)
const char* generateKeys()
{

     static std::string result;

    try {

        SEALContext* ctx = getContext();
        if (!ctx)
        {
            result = "{\"public\":null,\"secret\":null,\"relin\":null,\"error\":\"context is null\"}";
            return result.c_str(); // not initialised
        }

        KeyGenerator keygen(*ctx);

        PublicKey pk;
        SecretKey sk = keygen.secret_key();
        RelinKeys rk;

        keygen.create_public_key(pk);
        keygen.create_relin_keys(rk);

        std::ostringstream pk_stream, sk_stream, rk_stream;

        pk.save(pk_stream, compr_mode_type::none);
        sk.save(sk_stream, compr_mode_type::none);
        rk.save(rk_stream, compr_mode_type::none);

        result =
            std::string("{\"public\":\"") + base64Encode(pk_stream.str()) +
            "\",\"secret\":\"" + base64Encode(sk_stream.str()) +
            "\",\"relin\":\"" + base64Encode(rk_stream.str()) +
            "\"}";

            
        return result.c_str();

    }
    catch (const std::exception& e)
    {
        result = std::string("{\"public\":null,\"error\":\"") + e.what() + "\"}";
        return result.c_str();
    }
    catch (...)
    {
        result = "{\"public\":null,\"error\":\"unknown exception in generateKeys\"}";
        return result.c_str();
    }

}