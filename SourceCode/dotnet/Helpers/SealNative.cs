using System.Runtime.InteropServices;

public static class SealNative
{
    [DllImport("seal_wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool initSeal();

    [DllImport("seal_wrapper.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr generateKeys();
}