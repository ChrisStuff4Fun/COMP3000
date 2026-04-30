using System.Runtime.InteropServices;

public static class SealNative
{
    [DllImport("seal_wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool initSeal();

    [DllImport("seal_wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr generateKeys();

    [DllImport("seal_wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr computeSquaredDiff(string base64Cipher, double plaintextCentre);

    [DllImport("seal_wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern long addAndDecrypt(string base64Cipher1, string base64Cipher2);

    [DllImport("seal_wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern long decryptValue(string base64Cipher);


    [DllImport("seal_wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern string getParms();
}