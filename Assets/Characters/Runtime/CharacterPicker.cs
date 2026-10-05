using UnityEngine;

namespace Staff.Characters
{
    // Static portraits keep the menu lightweight: only the selected model is instantiated.
    public sealed class CharacterPicker : MonoBehaviour
    {
        CharacterSwitcher roster;
        AdventurePlayer player;
        Texture2D[] portraits;
        Vector2 scroll;
        GUIStyle title, subtitle, label, card;
        public bool IsOpen { get; private set; }
        public void Initialize(CharacterSwitcher switcher, AdventurePlayer controller)
        {
            roster = switcher; player = controller;
            portraits = new Texture2D[roster.Count];
            for (int i=0;i<portraits.Length;i++) portraits[i]=Resources.Load<Texture2D>("CharacterPortraits/"+roster.NameAt(i));
        }
        public void SetOpen(bool open, bool capture=true)
        {
            IsOpen=open;
            if (!player) return;
            player.InputBlocked=open;
            if (open) player.ReleaseInput();
            else if (capture) player.CaptureInput();
        }
        public void Choose(int index)
        {
            if (!roster || index<0 || index>=roster.Count) return;
            roster.Select(index); SetOpen(false);
        }
        void OnDestroy() { if (player) player.InputBlocked=false; }
        static string DisplayName(string name) => name.Replace("Kafka_NoCoat","Kafka · No coat").Replace("Ellen_OnCampus","Ellen · On Campus").Replace("TheHerta","The Herta").Replace("JuFufu","Ju Fufu");
        void OnGUI()
        {
            if (!IsOpen || !roster) return;
            if (title==null)
            {
                title=new GUIStyle(GUI.skin.label){fontSize=25,fontStyle=FontStyle.Bold};
                subtitle=new GUIStyle(GUI.skin.label){fontSize=13};
                label=new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleCenter,wordWrap=true};
                card=new GUIStyle(GUI.skin.button){padding=new RectOffset(5,5,5,5)};
            }
            var oldColor=GUI.color;
            GUI.color=new Color(0,0,0,.85f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=oldColor;
            float width=Mathf.Min(1000,Screen.width-32),height=Mathf.Max(160,Screen.height-48);
            var panel=new Rect((Screen.width-width)/2,24,width,height);
            GUI.Box(panel,GUIContent.none);
            GUI.Label(new Rect(panel.x+22,panel.y+14,width-120,34),"Choose your character",title);
            GUI.Label(new Rect(panel.x+22,panel.y+51,width-44,25),"Click a portrait to play · C / Esc to close",subtitle);
            if(GUI.Button(new Rect(panel.xMax-76,panel.y+18,54,30),"Close")){SetOpen(false);return;}
            int columns=Mathf.Clamp(Mathf.FloorToInt((width-44)/175),1,5);
            float gap=12,cell=(width-44-(columns-1)*gap-18)/columns,cardHeight=cell*1.35f+42;
            var viewport=new Rect(panel.x+22,panel.y+87,width-44,height-105);
            int rows=Mathf.CeilToInt(roster.Count/(float)columns);
            scroll=GUI.BeginScrollView(viewport,scroll,new Rect(0,0,viewport.width-18,rows*(cardHeight+gap)));
            int selected=-1;
            for(int i=0;i<roster.Count;i++)
            {
                var r=new Rect((i%columns)*(cell+gap),(i/columns)*(cardHeight+gap),cell,cardHeight);
                GUI.backgroundColor=i==roster.Index?new Color(.35f,.8f,1):Color.white;
                if(GUI.Button(r,GUIContent.none,card)) selected=i;
                GUI.backgroundColor=Color.white;
                if(portraits[i])GUI.DrawTexture(new Rect(r.x+6,r.y+6,r.width-12,r.height-45),portraits[i],ScaleMode.ScaleToFit);
                GUI.Label(new Rect(r.x+4,r.yMax-39,r.width-8,35),DisplayName(roster.NameAt(i)),label);
            }
            GUI.EndScrollView();GUI.color=oldColor;
            if(selected>=0)Choose(selected);
        }
    }
}
