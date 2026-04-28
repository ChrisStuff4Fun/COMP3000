#include "seal/seal.h"
using namespace seal;

// Globals
static std::unique_ptr<SEALContext> context;
static CKKSEncoder *encoder = nullptr;
static Encryptor *encryptor = nullptr;
static Evaluator *evaluator = nullptr;
static Decryptor *decryptor = nullptr;

// Encrypt
static PublicKey public_key;
// Decrypt
static SecretKey secret_key;
// Multiplication
static RelinKeys relin_keys;

// Initialise function
extern "C" __declspec(dllexport)
void initSeal()
{
    EncryptionParameters parms(scheme_type::bfv);

    // Set precision
    // Higher the polynomial > more secure and more room for + and x ops, but slow down each op
    size_t poly_modulus_degree = 8192;
    parms.set_poly_modulus_degree(poly_modulus_degree);
    parms.set_plain_modulus(PlainModulus::Batching(poly_modulus_degree, 20));
    parms.set_coeff_modulus(CoeffModulus::BFVDefault(poly_modulus_degree));

    context = std::make_unique<SEALContext>(parms);

    BatchEncoder encoder(*context);
    KeyGenerator keygen(*context);

    // Generate keys
    secret_key = keygen.secret_key();
    keygen.create_public_key(public_key);
    keygen.create_relin_keys(relin_keys);

    encryptor = new Encryptor(*context, public_key);
    evaluator = new Evaluator(*context);
    decryptor = new Decryptor(*context, secret_key);
}

