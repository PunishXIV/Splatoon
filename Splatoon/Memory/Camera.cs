namespace Splatoon;

internal static unsafe class Camera
{
    private static nint cameraAddressPtr;
    private static float* Xptr;
    private static float* Yptr;
    private static float* ZoomPtr;

    public static void Init()
    {
        try
        {
            cameraAddressPtr = *(nint*)Svc.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9 74 11 48 8B 01");
            if(cameraAddressPtr == nint.Zero) throw new Exception("Camera address was zero");
            PluginLog.Information($"Camera address ptr: {cameraAddressPtr:X16}");
            Xptr = (float*)(cameraAddressPtr + 0x140);
            Yptr = (float*)(cameraAddressPtr + 0x144);
            ZoomPtr = (float*)(cameraAddressPtr + 0x124);
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
        if(Yptr == null)
        {
            return 0;
        }
        return *Yptr;
    }

    internal static float GetZoom()
    {
        if(ZoomPtr == null)
        {
            return 20;
        }
        return *ZoomPtr;
    }
}
