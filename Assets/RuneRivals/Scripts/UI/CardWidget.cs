using UnityEngine;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>
    /// Нэг картын дүрслэл: ховор байдлын хүрээ, энергийн үнэ, зураг, нэр, тайлбар.
    /// compact = цуглуулгын жижиг хэлбэр (тайлбаргүй).
    /// </summary>
    public sealed class CardWidget
    {
        public RectTransform root;
        public UiButton button;
        public Image frame, art, costOrb, kindBand, lockIcon, dim, glow;
        public Text nameText, descText, costText, triggerText, lockText, stamp;
        public CardDef card;
        readonly bool compact;
        readonly float w, h;

        public CardWidget(Transform parent, float w, float h, bool compact, System.Action onClick)
        {
            this.w = w;
            this.h = h;
            this.compact = compact;
            button = UIKit.Btn(parent, null, "card_common", 0, onClick, "Card");
            button.pressScale = 0.95f;
            button.sound = null;
            root = button.rectTransform();
            root.sizeDelta = new Vector2(w, h);
            frame = button.frame;

            glow = UIKit.Img(root, ArtCache.Glow, new Color(1f, 0.9f, 0.4f, 0f), "Glow");
            glow.rectTransform.Stretch(-60, -60, -60, -60);
            glow.transform.SetAsFirstSibling();

            kindBand = UIKit.Block(root, Color.white, "Kind");
            kindBand.rectTransform.TopStrip(compact ? 12 : 16, 20);
            kindBand.rectTransform.offsetMin = new Vector2(20, kindBand.rectTransform.offsetMin.y);
            kindBand.rectTransform.offsetMax = new Vector2(-20, kindBand.rectTransform.offsetMax.y);

            art = UIKit.Img(root, null, Color.white, "Art");
            art.preserveAspect = true;

            nameText = UIKit.Label(root, "", compact ? 38 : 48, UIKit.TextCol, true, TextAnchor.MiddleCenter, "Name");
            if (compact) nameText.rectTransform.BottomStrip(56, 22);
            else nameText.rectTransform.Place(0.5f, 1f, 0, -h * 0.5f + 8, w - 30, 60);

            if (!compact)
            {
                descText = UIKit.Para(root, "", 32, new Color(0.9f, 0.87f, 1f), TextAnchor.UpperCenter, "Desc");
                descText.rectTransform.Stretch(22, 54, 22, h * 0.5f + 50);
                triggerText = UIKit.Label(root, "", 27, UIKit.Gold, false, TextAnchor.MiddleCenter, "Trigger");
                triggerText.rectTransform.BottomStrip(40, 18);
            }

            // Энергийн үнэ (зүүн дээд буланд)
            costOrb = UIKit.Img(root, ArtCache.Circle, UIKit.Gold, "Cost");
            float orb = compact ? 74 : 96;
            costOrb.rectTransform.Place(0f, 1f, -18, 18, orb, orb);
            var ring = UIKit.Img(costOrb.transform, ArtCache.Ring, UIKit.Ink, "Ring");
            ring.rectTransform.Fill();
            costText = UIKit.Label(costOrb.transform, "", compact ? 50 : 64, UIKit.Ink, true, TextAnchor.MiddleCenter, "Value", false);
            costText.rectTransform.Stretch(0, 4, 0, 0);

            dim = UIKit.Block(root, new Color(0.05f, 0.03f, 0.1f, 0.65f), "Dim");
            dim.rectTransform.Stretch(8, 8, 8, 8);
            dim.gameObject.SetActive(false);

            lockIcon = UIKit.Pix(root, "ui_lock", 2f, "Lock");
            lockIcon.rectTransform.anchoredPosition = new Vector2(0, compact ? 20 : 40);
            lockText = UIKit.Label(root, "", compact ? 30 : 38, UIKit.TextCol, true, TextAnchor.MiddleCenter, "LockText");
            lockText.rectTransform.Place(0.5f, 0.5f, 0, compact ? -50 : -40, w - 20, 50);
            lockIcon.gameObject.SetActive(false);
            lockText.gameObject.SetActive(false);

            stamp = UIKit.Outlined(root, "SOLD", compact ? 56 : 80, UIKit.BadCol, "Stamp");
            stamp.rectTransform.localEulerAngles = new Vector3(0, 0, 14);
            stamp.gameObject.SetActive(false);
        }

        public void Set(CardDef c, int price)
        {
            card = c;
            if (c == null)
            {
                root.gameObject.SetActive(false);
                return;
            }
            root.gameObject.SetActive(true);
            frame.SetFrame(ArtCache.CardFrame(c.rarity));
            kindBand.color = ArtCache.ToColor(Palette.Kind(c.kind));
            nameText.text = c.name;
            if (descText != null) descText.text = c.text.Replace("\n", " ");
            if (triggerText != null)
            {
                triggerText.text = string.IsNullOrEmpty(c.trigger) ? KindName(c.kind) : c.trigger;
                triggerText.color = string.IsNullOrEmpty(c.trigger) ? UIKit.Dim : UIKit.Gold;
            }

            // Зураг: бүхэл тоогоор томруулна (пиксел тод харагдана)
            var sprite = ArtCache.Get(c.art);
            art.sprite = sprite;
            var px = ArtCache.PixelSize(c.art);
            float boxW = w - 70, boxH = compact ? h * 0.48f : h * 0.36f;
            float k = Mathf.Max(2f, Mathf.Floor(Mathf.Min(boxW / px.x, boxH / px.y)));
            k = Mathf.Min(k, compact ? 9f : 12f);
            art.rectTransform.Place(0.5f, 1f, 0, compact ? -48 : -44, px.x * k, px.y * k);
            if (!compact)
            {
                float artBottom = 44 + px.y * k;
                nameText.rectTransform.Place(0.5f, 1f, 0, -artBottom - 4, w - 30, 56);
                descText.rectTransform.Stretch(22, 54, 22, artBottom + 60);
            }
            SetPrice(price);
            SetSold(false);
            SetLocked(false, null);
        }

        public void SetPrice(int price)
        {
            if (price <= 0 && card != null && card.cost > 0)
            {
                costOrb.color = UIKit.GoodCol;
                costText.text = "0";
            }
            else
            {
                costOrb.color = UIKit.Gold;
                costText.text = price.ToString();
            }
        }

        public void SetAffordable(bool ok)
        {
            costText.color = ok ? UIKit.Ink : new Color(0.75f, 0.1f, 0.15f);
            art.color = ok ? Color.white : new Color(0.7f, 0.7f, 0.75f);
        }

        public void SetSold(bool sold)
        {
            dim.gameObject.SetActive(sold);
            stamp.gameObject.SetActive(sold);
            costOrb.gameObject.SetActive(!sold);
        }

        public void SetLocked(bool locked, string text)
        {
            lockIcon.gameObject.SetActive(locked);
            lockText.gameObject.SetActive(locked);
            lockText.text = text ?? "";
            if (locked)
            {
                dim.gameObject.SetActive(true);
                stamp.gameObject.SetActive(false);
            }
            else if (!stamp.gameObject.activeSelf) dim.gameObject.SetActive(false);
        }

        public void SetGlow(float a) => glow.color = new Color(1f, 0.85f, 0.35f, Mathf.Clamp01(a));

        public static string KindName(CardKind k)
        {
            switch (k)
            {
                case CardKind.Troop: return "TROOP";
                case CardKind.Boost: return "BOOST";
                case CardKind.Hero: return "HERO";
                case CardKind.Sabotage: return "SABOTAGE";
                case CardKind.Economy: return "ECONOMY";
                default: return "TACTIC";
            }
        }
    }
}
