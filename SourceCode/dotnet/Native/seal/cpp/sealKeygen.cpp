#include "seal/seal.h"
#include <sstream>
#include <string>

using namespace seal;

// import functions from wrapper
extern "C" SEALContext* getContext();



 // helper function to convert from binary to b64
static std::string base64Encode(const std::string& input)
{
    static const char table[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    std::string out;
    int val = 0, valb = -6;
    for (unsigned char c : input)
    {
        val = (val << 8) + c;
        valb += 8;
        while (valb >= 0)
        {
            out.push_back(table[(val >> valb) & 0x3F]);
            valb -= 6;
        }
    }
    if (valb > -6) out.push_back(table[((val << 8) >> (valb + 8)) & 0x3F]);
    while (out.size() % 4) out.push_back('=');
    return out;
}


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