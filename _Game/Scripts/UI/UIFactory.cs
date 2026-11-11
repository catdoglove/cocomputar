using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public static class UIFactory
    {
        public static UITheme Theme;
        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var r = (RectTransform)go.transform; r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        public static Image Panel(Transform p, string name, float x, float y, float w, float h, Color color)
        {
            var r = Rect(p, name, x, y, w, h); var image = r.gameObject.AddComponent<Image>(); image.color = color; return image;
        }
        public static TextMeshProUGUI Text(Transform p, string value, float x, float y, float w, float h, float size = 22, Color? color = null)
        {
            var r = Rect(p, "텍스트", x, y, w, h); var text = r.gameObject.AddComponent<TextMeshProUGUI>();
            bool useNeo = (GameApp.Instance && GameApp.Instance.IsTitleScreen) || p.GetComponentInParent<NeoFontScope>();
            text.font = useNeo && Theme.titleFont ? Theme.titleFont : Theme.font; text.text = value; text.fontSize = size; text.color = color ?? Theme.navy;
            text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        public static Image Sprite(Transform p, Sprite sprite, float x, float y, float w, float h)
        {
            var image = Panel(p, "그림", x, y, w, h, Color.white); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; return image;
        }
        public static Button Button(Transform p, string label, float x, float y, float w, float h, Action action, Color? color = null)
        {
            bool windowButton=p.GetComponentInParent<FakeWindow>() && !color.HasValue;
            var image = Panel(p, label, x, y, w, h, color ?? (windowButton?WindowSkin.Pink:new Color(.78f,.78f,.80f)));
            var outline = image.gameObject.AddComponent<Outline>(); outline.effectColor = Color.white; outline.effectDistance = new Vector2(1,-1);
            if(windowButton){outline.effectColor=WindowSkin.Purple;WindowSkin.Bevel(image.rectTransform);}
            var b = image.gameObject.AddComponent<Button>(); b.targetGraphic = image;
            var text = Text(image.transform, label, 4, 1, w-8, h-2, 21); text.alignment = TextAlignmentOptions.Center;
            b.onClick.AddListener(() => { GameApp.Instance.Audio.Play("sfx_click"); action?.Invoke(); }); return b;
        }
        public static Slider Slider(Transform p, float x, float y, float value, Action<float> action,bool keyboardInput=true)
        {
            var root = Rect(p, "슬라이더", x, y, 230, 30);
            var track = Panel(root, "트랙", 0, 12, 230, 6, Theme.navy);
            var travel=Rect(root,"손잡이 이동 영역",8,0,214,30);
            var handle = Panel(travel, "손잡이", 0, 0, 16, 30, Theme.pink);
            Slider s = keyboardInput?root.gameObject.AddComponent<Slider>():root.gameObject.AddComponent<PointerOnlySlider>(); s.targetGraphic = handle; s.handleRect = handle.rectTransform;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight; s.minValue = 0; s.maxValue = 1; s.value = value;
            handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.sizeDelta=new Vector2(16,0);handle.rectTransform.anchoredPosition=Vector2.zero;
            s.onValueChanged.AddListener(v => action(v)); return s;
        }
        public static void Clear(Transform parent) { FolderView.BeforeClear(parent); for (int i = parent.childCount-1; i >= 0; i--) { var g = parent.GetChild(i).gameObject; g.SetActive(false); UnityEngine.Object.Destroy(g); } }
        public static RectTransform CheckMark(Transform parent,float x,float y)
        {
            var r=Rect(parent,"체크 표시",x,y,25,25);
            var a=Panel(r,"짧은 획",2,12,10,3,Theme.navy);a.rectTransform.localRotation=Quaternion.Euler(0,0,-45);
            var b=Panel(r,"긴 획",8,18,20,3,Theme.navy);b.rectTransform.localRotation=Quaternion.Euler(0,0,45);
            a.raycastTarget=false;b.raycastTarget=false;return r;
        }
    }
}
