using UnityEngine;
using UnityEngine.UI;
namespace Staff.Interaction
{
    // Layout reference: Honkai: Star Rail's Golden Hour interaction prompt.
    // Original UI artwork is not bundled; key ring and chair/door icons are drawn here.
    public sealed class InteractionPrompt : MonoBehaviour
    {
        Canvas canvas;
        CanvasGroup group;
        Text label, feedback;
        GameObject chairIcon, doorIcon;
        Font font;
        string action;
        float feedbackUntil;
        readonly Color gold = new(.84f, .76f, .51f, 1);
        void Awake()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Apple SD Gothic Neo", "Malgun Gothic", "Noto Sans CJK KR", "Arial" }, 24);
            var root = new GameObject("UI - World interaction", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            group = root.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false; group.alpha = 0;
            var row = Rect("Interaction row", root.transform, new Vector2(380, 64), Vector2.zero);
            row.anchorMin = row.anchorMax = new Vector2(.71f, .30f); row.pivot = new Vector2(0, .5f);
            Panel("Top border", row, new Vector2(310, 2), new Vector2(220, 29), gold);
            Panel("Bottom border", row, new Vector2(310, 2), new Vector2(220, -29), gold);
            Panel("Left border", row, new Vector2(2, 60), new Vector2(66, 0), gold);
            Panel("Right border", row, new Vector2(2, 60), new Vector2(374, 0), gold);
            Panel("Dark action bar", row, new Vector2(306, 56), new Vector2(220, 0), new Color(.025f,.027f,.035f,.94f));
            var ring = Rect("Key ring", row, new Vector2(42,42), new Vector2(25,0));
            for (int i=0;i<40;i++)
            {
                float angle=i*Mathf.PI*2/40;
                var line=Panel("Ring segment",ring,new Vector2(3.7f,2),new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*19,gold);
                line.localRotation=Quaternion.Euler(0,0,i*9+90);
            }
            Text("F key", ring, "F", 26, gold, new Vector2(42,42), Vector2.zero, TextAnchor.MiddleCenter);
            Panel("Selected dot",row,new Vector2(6,6),new Vector2(55,0),gold).localRotation=Quaternion.Euler(0,0,45);
            Panel("Icon divider",row,new Vector2(1,32),new Vector2(116,0),new Color(1,1,1,.3f));
            chairIcon=Rect("Chair icon",row,new Vector2(36,36),new Vector2(91,0)).gameObject;
            Panel("Seat",chairIcon.transform,new Vector2(25,5),new Vector2(0,-1),Color.white);
            Panel("Back",chairIcon.transform,new Vector2(5,20),new Vector2(-10,9),Color.white);
            Panel("Leg",chairIcon.transform,new Vector2(4,13),new Vector2(-9,-8),Color.white);
            Panel("Leg",chairIcon.transform,new Vector2(4,13),new Vector2(9,-8),Color.white);
            doorIcon=Rect("Door icon",row,new Vector2(36,36),new Vector2(91,0)).gameObject;
            Panel("Door frame",doorIcon.transform,new Vector2(23,31),Vector2.zero,Color.white);
            Panel("Door inset",doorIcon.transform,new Vector2(17,25),Vector2.zero,new Color(.08f,.08f,.09f,1));
            Panel("Door handle",doorIcon.transform,new Vector2(4,4),new Vector2(5,0),gold);
            label=Text("Action",row,"",24,Color.white,new Vector2(230,50),new Vector2(246,0),TextAnchor.MiddleLeft);
            feedback=Text("Feedback",row,"",18,gold,new Vector2(370,40),new Vector2(195,-51),TextAnchor.MiddleLeft);
        }
        static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(parent.name == "Interaction row" ? 0 : .5f,.5f);
            rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        static RectTransform Panel(string name,Transform parent,Vector2 size,Vector2 position,Color color)
        {var r=Rect(name,parent,size,position);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return r;}
        Text Text(string name,Transform parent,string value,int size,Color color,Vector2 dimensions,Vector2 position,TextAnchor align)
        {var r=Rect(name,parent,dimensions,position);var text=r.gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=color;text.alignment=align;text.raycastTarget=false;return text;}
        public void Show(string value,bool seat)
        {action=value;if(!label)return;label.text=value;chairIcon.SetActive(seat);doorIcon.SetActive(!seat);}
        public void Flash(string value){if(feedback)feedback.text=value;feedbackUntil=Time.unscaledTime+2;}
        void Update()
        {
            if(!group)return;
            group.alpha=Mathf.MoveTowards(group.alpha,string.IsNullOrEmpty(action)?0:1,Time.unscaledDeltaTime*9);
            if(Time.unscaledTime>feedbackUntil)feedback.text="";
        }
        void OnDestroy(){if(canvas)Destroy(canvas.gameObject);if(font)Destroy(font);}
    }
}
