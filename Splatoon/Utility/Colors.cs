namespace Splatoon.Utility;

public static class Colors
{ //abgr
    public const uint LightYellow = 0xffaaffff;
    public const uint Red = 0xff0000ff;
    public const uint DarkRed = 0xff000099;
    public const uint Orange = 0xff0099ff;
    public const uint Gray = 0xff999999;
    public const uint Transparent = 0x00000000;
    public const uint Green = 0xff00ff00;
    public const uint Yellow = 0xff00ffff;

    public static readonly Vector4 ElementLayoutIsVisible = ImGuiEx.Vector4FromRGB(0xc4ffc5);
    public static readonly Vector4 ElementIsConditional = ImGuiEx.Vector4FromRGB(0x6bfff8);
    public static readonly Vector4 ElementIsConditionalVisible = ImGuiEx.Vector4FromRGB(0xebff6b);

    public static uint MultiplyAlpha(uint color, float amount)
    {
        var alpha = color >> 24;
        alpha = (uint)(alpha * amount);
        alpha = Math.Clamp(alpha, 0x00, 0xFF);
        return color & 0x00FFFFFF | (alpha << 24);
    }

    // Linear interpolation between 1-byte components of uint32
    // Intended for interpolating colors
    public static uint Lerp(uint v1, uint v2, float amount)
    {
        if(v1 == v2) return v1;
        return Vector4.Lerp(v1.ToVector4(), v2.ToVector4(), amount).ToUint();
    }
}
