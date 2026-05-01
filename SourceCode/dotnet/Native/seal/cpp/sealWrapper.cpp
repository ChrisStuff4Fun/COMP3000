#include "seal/seal.h"
#include "base64.h"
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


        std::cout << parms.coeff_modulus().size() << std::endl;
        auto id = parms.parms_id();

        std::cout << std::hex;
        for (size_t i = 0; i < id.size(); i++)
        {
            std::cout << id[i];
            if (i + 1 < id.size()) std::cout << "-";
        }
        std::cout << std::dec << std::endl;

        KeyGenerator keygen(*context);

        secret_key = std::make_unique<SecretKey>(keygen.secret_key());
        public_key = std::make_unique<PublicKey>();
        keygen.create_public_key(*public_key);
        relin_keys = std::make_unique<RelinKeys>();
        keygen.create_relin_keys(*relin_keys);

        encryptor  = std::make_unique<Encryptor>(*context, *public_key);
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


 
// HOMOMORPHIC ENCRYPTION // // HOMOMORPHIC ENCRYPTION // // HOMOMORPHIC ENCRYPTION // // HOMOMORPHIC ENCRYPTION // // HOMOMORPHIC ENCRYPTION // // HOMOMORPHIC ENCRYPTION // // HOMOMORPHIC ENCRYPTION // 

// Computes encrypted result of (ciphertext - plaintext_centre)^2
extern "C" __declspec(dllexport)
const char* computeSquaredDiff(const char* base64Cipher, double plaintextCentre)
{
    static std::string result;
    try
    {
        if (!context) return "ERROR:not_initialised";

        // decode ciphertext from b64 back into binary
        std::string cipherBytes = base64Decode(base64Cipher);
        std::istringstream cipherStream(cipherBytes);
        Ciphertext cipher;
        cipher.load(*context, cipherStream);

        // encode centre as plaintext
        // scale must match what Android used 
        BatchEncoder encoder(*context);
        int64_t scaledCentre = static_cast<int64_t>(plaintextCentre * 1e10);
        std::vector<int64_t> centreVec(context->key_context_data()->parms().poly_modulus_degree(), scaledCentre);
        Plaintext centrePlain;
        encoder.encode(centreVec, centrePlain);

        // compute (cipher - centre)
        Evaluator evaluator(*context);
        Ciphertext diff;
        evaluator.sub_plain(cipher, centrePlain, diff);

        // compute (cipher - centre)^2
        Ciphertext squared;
        evaluator.multiply(diff, diff, squared);
        evaluator.relinearize_inplace(squared, *relin_keys);

        // serialise and base64 encode result
        std::ostringstream outStream;
        squared.save(outStream);

        result = base64Encode(outStream.str());
        return result.c_str();
    }
    catch (const std::exception& e)
    {
        // catch any errors :(
        result = std::string("ERROR:") + e.what();
        return result.c_str();
    }
}

// adds two ciphertexts and decrypts the first slot
extern "C" __declspec(dllexport)
long long addAndDecrypt(const char* base64Cipher1, const char* base64Cipher2)
{
    try
    {
        if (!context) return -1;

        //  decode back to binary
        std::string bytes1 = base64Decode(base64Cipher1);
        std::istringstream stream1(bytes1);
        Ciphertext c1;
        c1.load(*context, stream1);

        //  decode back to binary
        std::string bytes2 = base64Decode(base64Cipher2);
        std::istringstream stream2(bytes2);
        Ciphertext c2;
        c2.load(*context, stream2);
        
        // add input 1 and 2
        Evaluator evaluator(*context);
        Ciphertext sum;
        evaluator.add(c1, c2, sum);

        // decrypt result
        Plaintext plain;
        decryptor->decrypt(sum, plain);
        
        // encode back into b64 from binary for storage
        BatchEncoder encoder(*context);
        std::vector<int64_t> decoded;
        encoder.decode(plain, decoded);

        return decoded[0]; // squared distance in slot 0
    }
    catch (...)
    {
        return -1;
    }
}



extern "C" __declspec(dllexport)
long long decryptValue(const char* base64Cipher)
{
    try
    {
        if (!context || !decryptor) return -1;

        std::string cipherBytes = base64Decode(base64Cipher);
        std::istringstream cipherStream(cipherBytes, std::ios::binary);
        Ciphertext cipher;
        cipher.load(*context, cipherStream);

        Plaintext plain;
        decryptor->decrypt(cipher, plain);

        BatchEncoder encoder(*context);
        std::vector<int64_t> decoded;
        encoder.decode(plain, decoded);

        return decoded[0];
    }
    catch (const std::exception& e)
    {
        std::cerr << "Decrypt failed: " << e.what() << std::endl;
        return LLONG_MIN;
    }
}

extern "C" __declspec(dllexport)
const char* getParms()
{
    static std::string idStr;

    auto id = context->key_context_data()->parms_id();

    idStr.clear();

    for (size_t i = 0; i < id.size(); i++) {
        char buf[32];
        snprintf(buf, sizeof(buf), "%016llx", (unsigned long long)id[i]);
        idStr += buf;
        if (i + 1 < id.size()) idStr += "-";
    }

    return idStr.c_str();
}