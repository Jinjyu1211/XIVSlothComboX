using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace XIVSlothComboX.Extensions;

public static class SeStringUtils
{
    public static IntPtr emptyPtr;

    public static void Initialize()
    {
        emptyPtr = SeStringToPtr(Text(""));
    }

    public static void Dispose()
    {
    }

    public static SeString SeStringFromPtr(IntPtr seStringPtr)
    {
        byte b;
        var offset = 0;

        unsafe
        {
            while ((b = *(byte*)(seStringPtr + offset)) != 0)
            {
                offset++;
            }
        }

        var bytes = new byte[offset];
        Marshal.Copy(seStringPtr, bytes, 0, offset);

        return SeString.Parse(bytes);
    }

    public static IntPtr SeStringToPtr(SeString seString)
    {
        var bytes = seString.Encode();
        var pointer = Marshal.AllocHGlobal(bytes.Length + 1);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        Marshal.WriteByte(pointer, bytes.Length, 0);

        return pointer;
    }

    public static void FreePtr(IntPtr seStringPtr)
    {
        if (seStringPtr != emptyPtr)
        {
            Marshal.FreeHGlobal(seStringPtr);
        }
    }

    public static Byte[] NameText(string rawText)
    {
        var b = new byte[32];
        var Length = Math.Min(Encoding.UTF8.GetBytes(rawText.ToCharArray()).Length, b.Length);
        Array.Copy(Encoding.UTF8.GetBytes(rawText.ToCharArray()), b, Length);
        return b;
    }



    public static SeString Text(string rawText)
    {
        var seString = new SeString(new List<Payload>());
        seString.Append(new TextPayload(rawText));

        return seString;
    }

    public static SeString Text(string text, ushort color)
    {
        var seString = new SeString(new List<Payload>());
        seString.Append(new UIForegroundPayload(color));
        seString.Append(new TextPayload(text));
        seString.Append(UIForegroundPayload.UIForegroundOff);

        return seString;
    }

    public static SeString Icon(BitmapFontIcon icon, string? prefix)
    {
        var seString = new SeString(new List<Payload>());

        if (prefix != null)
        {
            seString.Append(new TextPayload(prefix));
        }

        seString.Append(new IconPayload(icon));

        return seString;
    }
}
