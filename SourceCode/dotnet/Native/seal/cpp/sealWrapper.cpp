#include "seal/seal.h"
using namespace seal;

// Globals
static std::unique_ptr<SEALContext> context;
static std::unique_ptr<Encryptor> encryptor;
static std::unique_ptr<Evaluator> evaluator;
static std::unique_ptr<Decryptor> decryptor;
static std::unique_ptr<BatchEncoder> encoder;

// Encrypt
static PublicKey public_key;
// Decrypt
static SecretKey secret_key;
// Multiplication
static RelinKeys relin_keys;

// Initialise function
extern "C" __declspec(dllexport)
bool initSeal()
{
    try
    {
        EncryptionParameters parms(scheme_type::bfv);

        size_t poly_modulus_degree = 8192;

        parms.set_poly_modulus_degree(poly_modulus_degree);
        parms.set_plain_modulus(PlainModulus::Batching(poly_modulus_degree, 20));
        parms.set_coeff_modulus(CoeffModulus::BFVDefault(poly_modulus_degree));

        context = std::make_unique<SEALContext>(parms);

        KeyGenerator keygen(*context);

        secret_key = keygen.secret_key();
        keygen.create_public_key(public_key);
        keygen.create_relin_keys(relin_keys);

        encryptor = std::make_unique<Encryptor>(*context, public_key);
        evaluator  = std::make_unique<Evaluator>(*context);
        decryptor  = std::make_unique<Decryptor>(*context, secret_key);
        encoder    = std::make_unique<BatchEncoder>(*context);

        return true;
    }
    catch (...)
    {
        return false;
    }
}