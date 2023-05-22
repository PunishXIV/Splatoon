namespace Splatoon;

unsafe static class Camera
{
    static nint cameraAddressPtr;
    static float* Xptr;
    static float* Yptr;
    static float* ZoomPtr;

    public static void Init()
    {
        try
        {
            cameraAddressPtr = *(nint*)Svc.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9 74 11 48 8B 01");
            if (cameraAddressPtr == nint.Zero) throw new Exception("Camera address was zero");
            PluginLog.Information($"Camera address ptr: {cameraAddressPtr:X16}");
            Xptr = (float*)(cameraAddressPtr + 0x130);
            Yptr = (float*)(cameraAddressPtr + 0x134);
            ZoomPtr = (float*)(cameraAddressPtr + 0x114);
            PluginLog.Information("Camera initialized successfully");
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    internal static float GetAngleX()
    {
        if(Xptr == null)
        {
            return 0;
        }
        return *Xptr;
    }

    internal static float GetAngleY()
    {
        if (Yptr == null)
        {
            return 0;
        }
        return *Yptr;
    }

    internal static float GetZoom()
    {
        if (ZoomPtr == null)
        {
            return 20;
        }
        return *ZoomPtr;
    }
}
