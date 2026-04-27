#include "seal/seal.h"
using namespace seal;

// Globals
static SEALContext *context = nullptr;
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
void init_seal()
{
    EncryptionParameters parms(scheme_type::ckks);

    // Set precision
    // Higher the polynomial > more secure and more room for + and x ops, but slow down each op
    size_t poly_modulus_degree = 8192;
    parms.set_poly_modulus_degree(poly_modulus_degree);
    parms.set_coeff_modulus(CoeffModulus::Create(poly_modulus_degree, { 60, 40, 40, 60 }));

    context = new SEALContext(parms);

    encoder = new CKKSEncoder(*context);
    KeyGenerator keygen(*context);

    // Generate keys
    secret_key = keygen.secret_key();
    public_key = keygen.public_key();
    relin_keys = keygen.relin_keys();

    encryptor = new Encryptor(*context, public_key);
    evaluator = new Evaluator(*context);
    decryptor = new Decryptor(*context, secret_key);
}



extern "C" __declspec(dllexport)
void encrypt_location(double lat, double lon, double *out_lat, double *out_lon)
{
    double scale = pow(2.0, 40);

    Plaintext pt_lat, pt_lon;

    encoder->encode(lat, scale, pt_lat);
    encoder->encode(lon, scale, pt_lon);

    Ciphertext ct_lat, ct_lon;

    encryptor->encrypt(pt_lat, ct_lat);
    encryptor->encrypt(pt_lon, ct_lon);

    *out_lat = lat;
    *out_lon = lon;
}