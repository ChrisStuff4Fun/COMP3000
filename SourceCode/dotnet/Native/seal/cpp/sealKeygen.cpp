#include "seal/seal.h"
#include <sstream>

using namespace seal;

// import functions from wrapper
extern "C" SEALContext* getContext();

extern "C" __declspec(dllexport)
const char* generateKeys()
{
    static std::string result;

    SEALContext* ctx = getContext();
    if (!ctx)
    {
        return "{}"; // not initialised
    }

    KeyGenerator keygen(*ctx);

    PublicKey pk;
    SecretKey sk = keygen.secret_key();
    RelinKeys rk;

    keygen.create_public_key(pk);
    keygen.create_relin_keys(rk);

    std::ostringstream pk_stream, sk_stream, rk_stream;

    pk.save(pk_stream);
    sk.save(sk_stream);
    rk.save(rk_stream);

    result =
        std::string("{\"public\":\"") + pk_stream.str() +
        "\",\"secret\":\"" + sk_stream.str() +
        "\",\"relin\":\"" + rk_stream.str() +
        "\"}";

    return result.c_str();
}