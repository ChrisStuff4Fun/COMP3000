#include "seal/seal.h"
using namespace seal;

static std::unique_ptr<SEALContext> context;
static std::unique_ptr<Encryptor> encryptor;
static std::unique_ptr<Evaluator> evaluator;
static std::unique_ptr<Decryptor> decryptor;
static std::unique_ptr<BatchEncoder> encoder;

static std::unique_ptr<PublicKey> public_key;
static std::unique_ptr<SecretKey> secret_key;
static std::unique_ptr<RelinKeys> relin_keys;

extern "C" __declspec(dllexport)
bool initSeal()
{
    try
    {
        EncryptionParameters parms(scheme_type::bfv);

        size_t poly_modulus_degree = 4096;

        parms.set_poly_modulus_degree(poly_modulus_degree);
        parms.set_plain_modulus(PlainModulus::Batching(poly_modulus_degree, 20));
        parms.set_coeff_modulus(CoeffModulus::BFVDefault(poly_modulus_degree));

        context = std::make_unique<SEALContext>(parms);

        KeyGenerator keygen(*context);

        secret_key = std::make_unique<SecretKey>(keygen.secret_key());
        public_key = std::make_unique<PublicKey>();
        keygen.create_public_key(*public_key);
        relin_keys = std::make_unique<RelinKeys>();
        keygen.create_relin_keys(*relin_keys);

        encryptor = std::make_unique<Encryptor>(*context, *public_key);
        evaluator  = std::make_unique<Evaluator>(*context);
        decryptor  = std::make_unique<Decryptor>(*context, *secret_key);
        encoder    = std::make_unique<BatchEncoder>(*context);

        return true;
    }
    catch (...)
    {
        return false;
    }
}

extern "C" __declspec(dllexport)
SEALContext* getContext() { return context.get(); }

extern "C" __declspec(dllexport)
PublicKey* getPublicKey() { return public_key.get(); }

extern "C" __declspec(dllexport)
SecretKey* getSecretKey() { return secret_key.get(); }

extern "C" __declspec(dllexport)
RelinKeys* getRelinKeys() { return relin_keys.get(); }