﻿using UnityEngine;

public static class Vibration
{

#if UNITY_ANDROID && !UNITY_EDITOR
    public static AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    public static AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    public static AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
#else
    public static AndroidJavaClass unityPlayer;
    public static AndroidJavaObject currentActivity;
    public static AndroidJavaObject vibrator;
#endif

    private static bool isAndroid;

    public static bool IsAndroid { get => IsDeviceAndroid(); }

    public static void Vibrate()
    {
        if (Settings.IsVibrationOn)
        {
            if (IsDeviceAndroid())
                vibrator.Call("vibrate");
            else
                Handheld.Vibrate();
        }
    }

    public static void Vibrate(long milliseconds)
    {
        if (Settings.IsVibrationOn)
        {
            if (IsDeviceAndroid())
                vibrator.Call("vibrate", milliseconds);
            else
                Handheld.Vibrate();
        }
    }

    public static void VibrateSoft()
    {
        if (Settings.IsVibrationOn)
        {
            long milliseconds = 20;

            if (IsDeviceAndroid())
                vibrator.Call("vibrate", milliseconds);
            else
                Handheld.Vibrate();
        }
    }

    public static void Vibrate(long[] pattern, int repeat)
    {
        if (Settings.IsVibrationOn)
        {
            if (IsDeviceAndroid())
                vibrator.Call("vibrate", pattern, repeat);
            else
                Handheld.Vibrate();
        }
    }

    public static void Cancel()
    {
        if (IsDeviceAndroid())
            vibrator.Call("cancel");
    }

    private static bool IsDeviceAndroid()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
	return true;
#else
        return false;
#endif
    }
}
