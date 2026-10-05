using UnityEngine;
using UnityEngine.InputSystem;
namespace Staff.Characters
{
    [DisallowMultipleComponent, RequireComponent(typeof(AdventurePlayer)), DefaultExecutionOrder(20)]
    public sealed class AdventureDance : MonoBehaviour
    {
        AdventurePlayer player;
        DanceCatalog catalog;
        AudioSource music;
        InputAction[] shortcuts;
        bool gameplayIntent;
        public int ActiveIndex { get; private set; } = -1;
        public bool IsDancing => ActiveIndex >= 0;
        public int Count => catalog ? catalog.dances.Length : 0;
        public AudioSource Music => music;
        public string CurrentTitle => IsDancing ? catalog.dances[ActiveIndex].title : "";
        void Awake()
        {
            player=GetComponent<AdventurePlayer>();
            catalog=Resources.Load<DanceCatalog>("StaffDanceCatalog");
            music=gameObject.AddComponent<AudioSource>();
            music.playOnAwake=false; music.loop=false; music.spatialBlend=0; music.volume=.7f;
        }
        void OnEnable()
        {
            shortcuts=new InputAction[Count];
            for(int i=0;i<shortcuts.Length;i++)
            {
                shortcuts[i]=new InputAction("Dance"+(i+1),InputActionType.Button,"<Keyboard>/"+(i+1));
                shortcuts[i].AddBinding("<Keyboard>/numpad"+(i+1));
                shortcuts[i].Enable();
            }
        }
        void OnDisable()
        {
            StopDance();
            if(shortcuts!=null)foreach(var action in shortcuts)action.Dispose();
            shortcuts=null;
        }
        void Update() => HandleInput(!player.InputBlocked && player.InputCaptured && Application.isFocused);
        public void HandleInput(bool accept)
        {
            if(!accept || shortcuts==null)return;
            for(int i=0;i<shortcuts.Length;i++)
                if(shortcuts[i].WasPressedThisFrame()){StartDance(i);break;}
        }
        // Called by the same movement path used for keyboard/gamepad and verification.
        // Look, zoom and camera recenter never enter this cancellation path.
        public void NotifyGameplay(AdventurePlayer.Command command, bool grounded, float speed)
        {
            gameplayIntent=command.move.sqrMagnitude>.0001f || command.jump || command.sprint;
            if(IsDancing && (gameplayIntent || !grounded || speed>.05f))StopDance();
        }
        public bool StartDance(int index)
        {
            if(!catalog || index<0 || index>=Count || player.InputBlocked || gameplayIntent ||
                !player.Grounded || player.HorizontalSpeed>.05f || player.IsDashing ||
                !player.Animator || !player.Animator.isHuman)return false;
            var entry=catalog.dances[index];
            int state=Animator.StringToHash("Base Layer.Dance_"+entry.id);
            if(!entry.clip || !player.Animator.HasState(0,state))return false;
            // Repeating a number restarts its dance; another number switches directly.
            ActiveIndex=index;
            player.Animator.SetInteger("Dance",index+1);
            player.Animator.CrossFadeInFixedTime(state,.10f,0,0);
            music.Stop();music.clip=entry.music;
            if(music.clip){music.time=Mathf.Clamp(entry.audioStart,0,Mathf.Max(0,music.clip.length-.01f));music.Play();}
            return true;
        }
        public void StopDance()
        {
            if(!IsDancing)return;
            ActiveIndex=-1;
            if(music)music.Stop();
            if(player && player.Animator)
            {
                player.Animator.SetInteger("Dance",0);
                string state=player.Grounded?"Locomotion":player.VerticalVelocity>0?"Jump":"Fall";
                player.Animator.CrossFadeInFixedTime("Base Layer."+state,.08f,0);
            }
        }
        void LateUpdate()
        {
            if(!IsDancing || !music.clip)return;
            var animator=player.Animator;
            var state=animator.IsInTransition(0)?animator.GetNextAnimatorStateInfo(0):animator.GetCurrentAnimatorStateInfo(0);
            var entry=catalog.dances[ActiveIndex];
            if(!state.IsName("Dance_"+entry.id))return;
            float time=entry.audioStart+Mathf.Repeat(state.normalizedTime,1)*entry.clip.length;
            if(time>=music.clip.length){music.Stop();return;}
            if(!music.isPlaying || Mathf.Abs(music.time-time)>.15f)
            {music.time=time;if(!music.isPlaying)music.Play();}
        }
    }
}
