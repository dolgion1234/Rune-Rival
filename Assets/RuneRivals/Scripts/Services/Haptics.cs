using UnityEngine;

namespace RuneRivals
{
    /// <summary>Утасны чичиргээ (Android). Бусад платформ дээр юу ч хийхгүй.</summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static int sdk = -1;

        static void Init()
        {
            if (vibrator != null) return;
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            catch { vibrator = null; }
        }
#endif

        public static void Light() => Vibrate(12, 60);
        public static void Medium() => Vibrate(25, 140);
        public static void Heavy() => Vibrate(60, 255);

        static void Vibrate(long ms, int amplitude)
        {
            if (!Save.VibrationOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Init();
            if (vibrator == null) return;
            try
            {
                if (sdk >= 26)
                {
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var oneShot = effect.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                        vibrator.Call("vibrate", oneShot);
                }
                else vibrator.Call("vibrate", ms);
            }
            catch { }
#endif
        }

#if UNITY_ANDROID
        // Энэ мөр Unity-д VIBRATE зөвшөөрлийг AndroidManifest-д нэмэхийг хэлдэг. Хэзээ ч дуудагдахгүй.
        static void KeepVibratePermission() => Handheld.Vibrate();
#endif
    }
}
