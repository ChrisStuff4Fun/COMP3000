using System.Runtime.InteropServices;

public static class SealNative
{
    [DllImport("sealWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void initSeal();

    [DllImport("sealKeygen.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr generateKeys();
}