using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>UI-г кодоор бүтээх туслах функцууд (Unity дээр гараар тохируулах шаардлагагүй).</summary>
    public static class UIKit
    {
        static Font title, body;

        public static Font Title => title ? title : (title = LoadFont("RuneRivals/Fonts/TitleFont"));
        public static Font Body => body ? body : (body = LoadFont("RuneRivals/Fonts/BodyFont"));

        static Font LoadFont(string path)
        {
            var f = Resources.Load<Font>(path);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // ---- Өнгө (Palette-аас) ----
        public static readonly Color Ink = ArtCache.ToColor(Palette.Ink);
        public static readonly Color TextCol = ArtCache.ToColor(Palette.UiText);
        public static readonly Color Dim = ArtCache.ToColor(Palette.UiTextDim);
        public static readonly Color Gold = ArtCache.ToColor(Palette.Gold);
        public static readonly Color GoodCol = ArtCache.ToColor(Palette.Good);
        public static readonly Color BadCol = ArtCache.ToColor(Palette.Bad);
        public static readonly Color AllyCol = ArtCache.ToColor(Palette.Ally);
        public static readonly Color FoeCol = ArtCache.ToColor(Palette.Foe);
        public static readonly Color GemCol = ArtCache.ToColor(Palette.Gem);
        public static readonly Color Night = ArtCache.ToColor(Palette.UiNight);
        public static readonly Color Shade = new Color(0.05f, 0.03f, 0.1f, 0.78f);

        // ================= RectTransform =================

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Fill(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Эцгийнхээ хүрээнд margin-тай сунана (зүүн, доод, баруун, дээд).</summary>
        public static RectTransform Stretch(this RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>anchor: (0,1)=зүүн дээд, (0.5,0.5)=төв, (0.5,0)=доод төв. pivot = anchor.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, float ax, float ay, float x, float y, float w, float h) =>
            rt.Place(new Vector2(ax, ay), new Vector2(x, y), new Vector2(w, h));

        /// <summary>Төв pivot-той байршуулах: anchor (ax, ay) цэгээс (x, y) зайд.</summary>
        public static RectTransform At(this RectTransform rt, float ax, float ay, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Дээд талд наалдсан хэвтээ тууз.</summary>
        public static RectTransform TopStrip(this RectTransform rt, float height, float fromTop = 0f)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, height);
            rt.anchoredPosition = new Vector2(0, -fromTop);
            return rt;
        }

        /// <summary>Доод талд наалдсан хэвтээ тууз.</summary>
        public static RectTransform BottomStrip(this RectTransform rt, float height, float fromBottom = 0f)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, height);
            rt.anchoredPosition = new Vector2(0, fromBottom);
            return rt;
        }

        public static CanvasGroup Group(this Component c)
        {
            var g = c.GetComponent<CanvasGroup>();
            return g ? g : c.gameObject.AddComponent<CanvasGroup>();
        }

        // ================= Зураг =================

        public static Image Img(Transform parent, Sprite sprite, Color color, string name = "Image", bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Энгийн өнгөт тэгш өнцөгт.</summary>
        public static Image Block(Transform parent, Color color, string name = "Block", bool raycast = false) =>
            Img(parent, ArtCache.White, color, name, raycast);

        /// <summary>9-slice пиксел хүрээ. scale: 1 пиксел = PixelScale*scale нэгж.</summary>
        public static Image Frame(Transform parent, string frame, string name = "Frame", bool raycast = false, float scale = 1f)
        {
            var img = Img(parent, ArtCache.Frame(frame), Color.white, name, raycast);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.1f, scale);
            return img;
        }

        public static void SetFrame(this Image img, string frame)
        {
            img.sprite = ArtCache.Frame(frame);
            img.type = Image.Type.Sliced;
        }

        /// <summary>Пиксел дүрс: хэмжээ = пиксел * PixelScale * scale.</summary>
        public static Image Pix(Transform parent, string sprite, float scale = 1f, string name = null)
        {
            var img = Img(parent, ArtCache.Get(sprite), Color.white, name ?? sprite);
            var px = ArtCache.PixelSize(sprite);
            img.rectTransform.sizeDelta = new Vector2(px.x, px.y) * ArtCache.PixelScale * scale;
            img.preserveAspect = true;
            return img;
        }

        public static void SetPix(this Image img, string sprite, float scale = 1f)
        {
            img.sprite = ArtCache.Get(sprite);
            var px = ArtCache.PixelSize(sprite);
            img.rectTransform.sizeDelta = new Vector2(px.x, px.y) * ArtCache.PixelScale * scale;
        }

        // ================= Текст =================

        public static Text Label(Transform parent, string text, int size, Color color, bool titleFont = false,
            TextAnchor align = TextAnchor.MiddleCenter, string name = "Label", bool shadow = true)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = titleFont ? Title : Body;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = Ink;
                s.effectDistance = new Vector2(0, -Mathf.Max(3f, size * 0.07f));
            }
            return t;
        }

        /// <summary>Олон мөртэй (тасалдаг) текст.</summary>
        public static Text Para(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.UpperLeft, string name = "Para")
        {
            var t = Label(parent, text, size, color, false, align, name);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.lineSpacing = 1.05f;
            return t;
        }

        /// <summary>Тод хүрээтэй текст (тулааны тоо).</summary>
        public static Text Outlined(Transform parent, string text, int size, Color color, string name = "Num")
        {
            var t = Label(parent, text, size, color, true, TextAnchor.MiddleCenter, name, false);
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = Ink;
            o.effectDistance = new Vector2(4, -4);
            return t;
        }

        public static string Num(int n)
        {
            if (n >= 100000) return (n / 1000) + "K";
            if (n >= 10000) return (n / 1000f).ToString("0.#") + "K";
            return n.ToString("N0");
        }

        // ================= Товч =================

        /// <summary>
        /// 9-slice товч. label = null бол текстгүй товч (дүрс эсвэл өөр агуулгатай).
        /// label = "" бол хоосон текст үүсгэнэ: үнэ, ON/OFF зэргийг дараа нь label.text / SetLabel-ээр бөглөнө.
        /// </summary>
        public static UiButton Btn(Transform parent, string label, string frame, int fontSize, Action onClick, string name = "Button")
        {
            var img = Frame(parent, frame, name, true);
            var b = img.gameObject.AddComponent<UiButton>();
            b.onClick = onClick;
            b.frame = img;
            if (label != null)
            {
                var t = Label(img.transform, label, fontSize, TextCol, true, TextAnchor.MiddleCenter, "Text");
                t.rectTransform.Stretch(10, 12, 10, 4);
                b.label = t;
            }
            return b;
        }

        /// <summary>Дүрстэй жижиг товч (тохиргоо, буцах).</summary>
        public static UiButton IconBtn(Transform parent, string icon, string frame, float iconScale, Action onClick, string name = "IconButton")
        {
            var b = Btn(parent, null, frame, 0, onClick, name);
            var i = Pix(b.transform, icon, iconScale, "Icon");
            i.rectTransform.anchoredPosition = new Vector2(0, 4);
            b.icon = i;
            return b;
        }

        /// <summary>Хоосон (зураггүй) дарагдах талбай.</summary>
        public static UiButton HitArea(Transform parent, Action onClick, string name = "Hit")
        {
            var img = Img(parent, ArtCache.White, new Color(1, 1, 1, 0), name, true);
            var b = img.gameObject.AddComponent<UiButton>();
            b.onClick = onClick;
            b.pressScale = 0.97f;
            return b;
        }

        // ================= Мөр =================

        /// <summary>Дүрс + тоо (зоос, эрдэнэ, энерги).</summary>
        public static Text IconValue(Transform parent, string icon, string value, int size, Color color, float iconScale, out Image iconImg)
        {
            var row = Rect("IconValue", parent);
            iconImg = Pix(row, icon, iconScale, "Icon");
            iconImg.rectTransform.Place(0f, 0.5f, 0, 0, iconImg.rectTransform.sizeDelta.x, iconImg.rectTransform.sizeDelta.y);
            var t = Label(row, value, size, color, true, TextAnchor.MiddleLeft, "Value");
            t.rectTransform.anchorMin = new Vector2(0, 0);
            t.rectTransform.anchorMax = new Vector2(1, 1);
            t.rectTransform.offsetMin = new Vector2(iconImg.rectTransform.sizeDelta.x + 10, 0);
            t.rectTransform.offsetMax = Vector2.zero;
            return t;
        }

        /// <summary>IconValue мөрийн өргөнийг агуулгад нь тааруулна (төвд байрлуулахад).</summary>
        public static void FitIconValue(Text value)
        {
            var row = (RectTransform)value.transform.parent;
            var icon = row.Find("Icon") as RectTransform;
            float iw = icon != null ? icon.sizeDelta.x : 0f;
            row.sizeDelta = new Vector2(iw + 10f + value.preferredWidth, row.sizeDelta.y);
        }

        // ================= Гүйлгэх талбай =================

        /// <summary>Босоо гүйлгэх талбай. Буцаах content-ийн өндрийг (sizeDelta.y) өөрөө тохируулна.</summary>
        public static RectTransform Scroll(Transform parent, out ScrollRect sr, string name = "Scroll")
        {
            var view = Rect(name, parent);
            var bg = view.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0);
            bg.raycastTarget = true;
            view.gameObject.AddComponent<RectMask2D>();
            sr = view.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40f;
            sr.inertia = true;
            sr.decelerationRate = 0.12f;
            var content = Rect("Content", view);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, 1000);
            content.anchoredPosition = Vector2.zero;
            sr.viewport = view;
            sr.content = content;
            return content;
        }

        /// <summary>Хэсгийн гарчиг (зураас + текст).</summary>
        public static Text Header(Transform parent, string text, float y, float height = 80f)
        {
            var row = Rect("Header_" + text, parent).TopStrip(height, y);
            var line = Block(row, new Color(1f, 0.82f, 0.29f, 0.35f), "Line");
            line.rectTransform.anchorMin = new Vector2(0, 0.5f);
            line.rectTransform.anchorMax = new Vector2(1, 0.5f);
            line.rectTransform.sizeDelta = new Vector2(-60, 5);
            line.rectTransform.anchoredPosition = Vector2.zero;
            var bg = Block(row, Night, "Bg");
            var t = Label(row, text, 56, Gold, true);
            t.rectTransform.Fill();
            float w = t.preferredWidth + 60f;
            bg.rectTransform.At(0.5f, 0.5f, 0, 0, w, height * 0.8f);
            return t;
        }

        // ================= Давхарга =================

        public static Image Blocker(Transform parent, Color color)
        {
            var img = Block(parent, color, "Blocker", true);
            img.rectTransform.anchorMin = new Vector2(-0.5f, -0.5f);
            img.rectTransform.anchorMax = new Vector2(1.5f, 1.5f);
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
            return img;
        }

        public static UiBar Bar(Transform parent, Color fill, string name = "Bar")
        {
            var back = Frame(parent, "bar_back", name);
            var bar = back.gameObject.AddComponent<UiBar>();
            var area = Rect("Area", back.transform).Stretch(5, 5, 5, 5);
            var ghost = Frame(area, "bar_fill", "Ghost");
            ghost.color = new Color(1f, 0.95f, 0.8f, 0.9f);
            var f = Frame(area, "bar_fill", "Fill");
            f.color = fill;
            bar.fill = f.rectTransform;
            bar.ghost = ghost.rectTransform;
            bar.fillImage = f;
            bar.Set(1f, true);
            return bar;
        }

        // ================= Анимаци (Time.timeScale = 0 үед ч ажиллана) =================

        static float BackOut(float k)
        {
            const float s = 1.70158f;
            k -= 1f;
            return 1f + (s + 1f) * k * k * k + s * k * k;
        }

        public static IEnumerator Pop(Transform tr, float from = 0.6f, float duration = 0.32f)
        {
            float t = 0f;
            while (t < duration && tr != null)
            {
                t += Time.unscaledDeltaTime;
                float e = BackOut(Mathf.Clamp01(t / duration));
                tr.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, e);
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }

        public static IEnumerator Punch(Transform tr, float amount = 0.15f, float duration = 0.25f)
        {
            float t = 0f;
            while (t < duration && tr != null)
            {
                t += Time.unscaledDeltaTime;
                float k = t / duration;
                tr.localScale = Vector3.one * (1f + amount * Mathf.Sin(k * Mathf.PI) * (1f - k));
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }

        public static IEnumerator Fade(CanvasGroup g, float to, float duration)
        {
            if (g == null) yield break;
            float from = g.alpha, t = 0f;
            if (to > 0f) { g.gameObject.SetActive(true); g.blocksRaycasts = true; g.interactable = true; }
            else { g.blocksRaycasts = false; g.interactable = false; }
            while (t < duration && g != null)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            if (g == null) yield break;
            g.alpha = to;
            if (to <= 0f) g.gameObject.SetActive(false);
        }

        public static IEnumerator Move(RectTransform rt, Vector2 to, float duration, bool back = false)
        {
            Vector2 from = rt.anchoredPosition;
            float t = 0f;
            while (t < duration && rt != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float e = back ? BackOut(k) : 1f - (1f - k) * (1f - k) * (1f - k);
                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = to;
        }

        /// <summary>Тоо гүйлгэх (12 → 57).</summary>
        public static IEnumerator Count(Text text, int from, int to, float duration, string prefix = "", string suffix = "")
        {
            float t = 0f;
            while (t < duration && text != null)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f);
                text.text = prefix + Mathf.RoundToInt(Mathf.Lerp(from, to, k)).ToString("N0") + suffix;
                yield return null;
            }
            if (text != null) text.text = prefix + to.ToString("N0") + suffix;
        }

        public static IEnumerator Delay(float seconds, Action then)
        {
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            then?.Invoke();
        }
    }

    // ===================================================================
    // Бүрэлдэхүүн хэсгүүд
    // ===================================================================

    /// <summary>Товч: дарахад жижгэрч, дуу + чичиргээ. interactable=false үед бүдгэрнэ.</summary>
    public class UiButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action onClick;
        public Image frame, icon;
        public Text label;
        public float pressScale = 0.92f;
        public string sound = "click";
        public bool haptic = true;
        bool interactable = true, pressed, animating;
        Vector3 rest = Vector3.one;

        public bool Interactable
        {
            get => interactable;
            set
            {
                interactable = value;
                var c = value ? Color.white : new Color(0.55f, 0.55f, 0.62f, 1f);
                if (frame != null) frame.color = c;
                if (icon != null) icon.color = c;
                if (label != null) label.color = value ? UIKit.TextCol : new Color(0.8f, 0.8f, 0.85f, 0.85f);
            }
        }

        public void SetLabel(string s)
        {
            if (label != null) label.text = s;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!animating) rest = Vector3.one;
            pressed = true;
            animating = true;
        }

        public void OnPointerUp(PointerEventData e) => pressed = false;

        public void OnPointerExit(PointerEventData e) => pressed = false;

        public void OnPointerClick(PointerEventData e)
        {
            if (!interactable)
            {
                AudioManager.Instance?.Error();
                StartCoroutine(Shake());
                return;
            }
            if (!string.IsNullOrEmpty(sound)) AudioManager.Instance?.Play(sound, 0.8f);
            if (haptic) Haptics.Light();
            onClick?.Invoke();
        }

        IEnumerator Shake()
        {
            var start = transform.localPosition;
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.unscaledDeltaTime;
                transform.localPosition = start + Vector3.right * Mathf.Sin(t * 70f) * 10f * (1f - t / 0.2f);
                yield return null;
            }
            transform.localPosition = start;
        }

        void Update()
        {
            if (!animating) return;
            var target = pressed ? rest * pressScale : rest;
            transform.localScale = Vector3.MoveTowards(transform.localScale, target, Time.unscaledDeltaTime * 3f);
            if (!pressed && transform.localScale == rest) animating = false;
        }

        void OnDisable()
        {
            if (animating) transform.localScale = rest;
            pressed = animating = false;
        }
    }

    /// <summary>Хэмжүүр (HP, ахиц): anchor-оор дүүргэнэ, буурахад "сүүдэр" удаан хөөнө.</summary>
    public class UiBar : MonoBehaviour
    {
        public RectTransform fill, ghost;
        public Image fillImage;
        float value = 1f, ghostValue = 1f;

        public float Value => value;

        public void Set(float v, bool instant = false)
        {
            v = Mathf.Clamp01(v);
            if (v > value || instant) ghostValue = v;
            value = v;
            Apply();
        }

        public void SetColor(Color c)
        {
            if (fillImage != null) fillImage.color = c;
        }

        void Apply()
        {
            if (fill != null)
            {
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(value, 1f);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
                fill.gameObject.SetActive(value > 0.001f);
            }
            if (ghost != null)
            {
                ghost.anchorMin = Vector2.zero;
                ghost.anchorMax = new Vector2(ghostValue, 1f);
                ghost.offsetMin = ghost.offsetMax = Vector2.zero;
                ghost.gameObject.SetActive(ghostValue > value + 0.001f);
            }
        }

        void Update()
        {
            if (ghostValue > value)
            {
                ghostValue = Mathf.MoveTowards(ghostValue, value, Time.unscaledDeltaTime * 0.6f);
                Apply();
            }
        }
    }

    /// <summary>Утасны "notch" болон доод зурвасаас зайлсхийнэ.</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        Vector2Int lastSize;
        RectTransform rt;

        void Awake()
        {
            rt = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != last || lastSize.x != Screen.width || lastSize.y != Screen.height) Apply();
        }

        void Apply()
        {
            last = Screen.safeArea;
            lastSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rt.anchorMin = new Vector2(last.xMin / Screen.width, last.yMin / Screen.height);
            rt.anchorMax = new Vector2(last.xMax / Screen.width, last.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Урт утсанд өргөнөөр, өргөн (таблет) дэлгэцэнд өндрөөр тааруулна.</summary>
    public class CanvasAspect : MonoBehaviour
    {
        CanvasScaler scaler;
        float lastAspect = -1f;

        void Awake() => scaler = GetComponent<CanvasScaler>();

        void Update()
        {
            if (scaler == null || Screen.width <= 0) return;
            float aspect = Screen.height / (float)Screen.width;
            if (Mathf.Abs(aspect - lastAspect) < 0.001f) return;
            lastAspect = aspect;
            scaler.matchWidthOrHeight = aspect >= 1920f / 1080f - 0.01f ? 0f : 1f;
        }
    }

    /// <summary>Аажуу дээш доош хөвөх (idle bob).</summary>
    public class Bob : MonoBehaviour
    {
        public float amplitude = 8f, speed = 2f, phase;
        public bool pixelSnap = true;
        RectTransform rt;
        Vector2 basePos;
        bool init;

        void OnEnable()
        {
            rt = (RectTransform)transform;
            if (!init) { basePos = rt.anchoredPosition; init = true; }
        }

        public void Rebase()
        {
            if (rt == null) rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
            init = true;
        }

        void Update()
        {
            float y = Mathf.Sin(Time.unscaledTime * speed + phase) * amplitude;
            if (pixelSnap) y = Mathf.Round(y / ArtCache.PixelScale) * ArtCache.PixelScale;
            rt.anchoredPosition = basePos + new Vector2(0, y);
        }
    }

    /// <summary>Тасралтгүй эргэх (туяа, хүлээлтийн дүрс).</summary>
    public class Spin : MonoBehaviour
    {
        public float speed = 30f;
        void Update() => transform.Rotate(0, 0, speed * Time.unscaledDeltaTime);
    }

    /// <summary>Анхаарал татах лугшилт (BATTLE товч).</summary>
    public class Pulse : MonoBehaviour
    {
        public float amount = 0.04f, speed = 3f;
        void Update()
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * speed) * amount;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
