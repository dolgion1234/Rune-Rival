using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace RuneRivals
{
    /// <summary>
    /// Тоглоомын эхлэл. Play дарахад автоматаар үүснэ (scene дээр юу ч тохируулах шаардлагагүй).
    /// Бүх UI, тулаан, дуу кодоор бүтээгдэнэ.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        UIManager ui;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (Instance == null) new GameObject("RuneRivals").AddComponent<GameManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Time.timeScale = 1f;

            SetupCamera();
            EnsureEventSystem();

            gameObject.AddComponent<AudioManager>();
            gameObject.AddComponent<IAPManager>();
            gameObject.AddComponent<AdsManager>();
            ui = gameObject.AddComponent<UIManager>();
            ui.Build();
        }

        void Start()
        {
            ui.ShowHome();
            AudioManager.Instance.PlayMusic(false);
        }

        void Update()
        {
            bool back = false;
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null) back = kb.escapeKey.wasPressedThisFrame;
#else
            back = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (back) ui.HandleBack();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            Save.Flush();
            if (ui != null) ui.OnAppPaused();
        }

        void OnApplicationQuit() => Save.Flush();

        // ---------------- Камер (UI зөвхөн, 3D/2D объект байхгүй) ----------------

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIKit.Night;
            cam.cullingMask = 0;
            cam.orthographic = true;

            // URP-ийн post-processing-ийг унтраана (UI overlay дээр хэрэггүй). Reflection: URP-гүй төсөлд ч compile болно.
            foreach (var c in cam.GetComponents<Component>())
            {
                if (c == null || c.GetType().Name != "UniversalAdditionalCameraData") continue;
                var prop = c.GetType().GetProperty("renderPostProcessing");
                if (prop != null && prop.CanWrite) prop.SetValue(c, false, null);
            }
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(go);
        }
    }
}
