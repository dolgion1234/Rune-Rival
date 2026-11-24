using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace RuneRivals.EditorTools
{
    /// <summary>
    /// 1) "In App Purchasing" багцыг автоматаар суулгаж, RR_IAP тэмдэгтийг нэмнэ.
    /// 2) Цэс: Rune Rivals → Setup Android (Player Settings-ийг нэг дарж тохируулна).
    /// 3) Туршилтын хэрэгсэл: хадгалалт цэвэрлэх, алт/эрдэнэ/цом нэмэх.
    /// </summary>
    [InitializeOnLoad]
    static class RuneRivalsEditor
    {
        const string IapPackage = "com.unity.purchasing";
        const string Define = "RR_IAP";
        const string RequestedKey = "RuneRivals.IapRequested";
        const string IconPath = "Assets/RuneRivals/Art/icon.png";
        static AddRequest addRequest;

        static readonly NamedBuildTarget[] Targets =
        {
            NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone
        };

        static RuneRivalsEditor()
        {
            EditorApplication.delayCall += CheckIap;
        }

        // ---------------- In App Purchasing ----------------

        static bool IapCompiled()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetType("UnityEngine.Purchasing.UnityIAPServices", false) != null);
        }

        static bool IapInManifest()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
            return File.Exists(path) && File.ReadAllText(path).Contains("\"" + IapPackage + "\"");
        }

        static void CheckIap()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += CheckIap;
                return;
            }

            if (IapCompiled())
            {
                SetDefine(true);
                return;
            }

            // Багц байхгүй бол тэмдэгтийг авч хаяна (эс тэгвээс compile алдаа гарна)
            SetDefine(false);

            if (!IapInManifest() && !SessionState.GetBool(RequestedKey, false))
            {
                SessionState.SetBool(RequestedKey, true);
                Debug.Log("[RuneRivals] Installing In App Purchasing package...");
                addRequest = Client.Add(IapPackage);
                EditorApplication.update += WaitForAdd;
            }
        }

        static void WaitForAdd()
        {
            if (addRequest == null || !addRequest.IsCompleted) return;
            EditorApplication.update -= WaitForAdd;
            if (addRequest.Status == StatusCode.Success)
                Debug.Log("[RuneRivals] In App Purchasing installed: " + addRequest.Result.version);
            else
                Debug.LogWarning("[RuneRivals] Could not install In App Purchasing: " + addRequest.Error?.message +
                                 "\nInstall it manually: Window > Package Manager > Unity Registry > In App Purchasing.");
            addRequest = null;
        }

        static void SetDefine(bool on)
        {
            foreach (var target in Targets)
            {
                string current;
                try { current = PlayerSettings.GetScriptingDefineSymbols(target); }
                catch { continue; }

                var list = current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                bool has = list.Contains(Define);
                if (on == has) continue;
                if (on) list.Add(Define); else list.Remove(Define);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
            }
        }

        // ---------------- Android ----------------

        [MenuItem("Rune Rivals/Setup Android (1 click)", priority = 0)]
        public static void SetupAndroid()
        {
            PlayerSettings.companyName = GameConfig.CompanyName;
            PlayerSettings.productName = GameConfig.ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, GameConfig.PackageId);
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1" || PlayerSettings.bundleVersion == "0.1.0")
                PlayerSettings.bundleVersion = "1.0.0";
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;

            // Зөвхөн босоо
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Google Play-ийн шаардлага: IL2CPP + ARM64, AAB
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            EditorUserBuildSettings.buildAppBundle = true;

            // Нүүр зураг (icon)
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            else
                Debug.LogWarning("[RuneRivals] Icon not found at " + IconPath);

            AssetDatabase.SaveAssets();
            Debug.Log("[RuneRivals] Android settings applied. Package: " + GameConfig.PackageId +
                      "\nNext: File > Build Profiles > Android > Switch Platform, then Player Settings > Publishing Settings > Keystore Manager.");
            EditorUtility.DisplayDialog("Rune Rivals",
                "Android тохиргоо хийгдлээ.\n\nPackage: " + GameConfig.PackageId +
                "\nVersion: " + PlayerSettings.bundleVersion + " (" + PlayerSettings.Android.bundleVersionCode + ")" +
                "\n\nДараагийн алхам: Keystore үүсгэх (ЗААВАР-аас харна уу).", "OK");
        }

        [MenuItem("Rune Rivals/Increase Version Code", priority = 1)]
        public static void BumpVersion()
        {
            PlayerSettings.Android.bundleVersionCode++;
            var parts = PlayerSettings.bundleVersion.Split('.');
            if (parts.Length == 3 && int.TryParse(parts[2], out int patch))
                PlayerSettings.bundleVersion = parts[0] + "." + parts[1] + "." + (patch + 1);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RuneRivals] Version {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode})");
        }

        // ---------------- Туршилт ----------------

        [MenuItem("Rune Rivals/Test/Reset Save Data", priority = 20)]
        public static void ResetSave()
        {
            Save.ResetAll();
            Debug.Log("[RuneRivals] Save data cleared. Restart Play mode to begin as a new player.");
        }

        [MenuItem("Rune Rivals/Test/Give 5,000 Gold + 500 Gems", priority = 21)]
        public static void GiveMoney()
        {
            Save.P.gold += 5000;
            Save.P.gems += 500;
            Save.Commit();
            Debug.Log("[RuneRivals] +5,000 gold, +500 gems.");
        }

        [MenuItem("Rune Rivals/Test/Add 300 Trophies", priority = 22)]
        public static void AddTrophies()
        {
            var p = Save.P;
            p.trophies += 300;
            p.bestTrophies = Math.Max(p.bestTrophies, p.trophies);
            p.highestTier = Math.Max(p.highestTier, League.TierOf(p.trophies));
            Save.Commit();
            Debug.Log("[RuneRivals] Trophies: " + p.trophies + " (" + League.Names[League.TierOf(p.trophies)] + ")");
        }

        [MenuItem("Rune Rivals/Test/Skip Tutorial", priority = 23)]
        public static void SkipTutorial()
        {
            Save.P.tutorialStep = 99;
            Save.Commit();
            Debug.Log("[RuneRivals] Tutorial skipped.");
        }
    }
}
