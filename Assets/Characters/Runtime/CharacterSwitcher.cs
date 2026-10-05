using UnityEngine;
using Staff.MathSpace;
using UnityEngine.InputSystem;

namespace Staff.Characters
{
    [DisallowMultipleComponent, RequireComponent(typeof(AdventurePlayer))]
    public sealed class CharacterSwitcher : MonoBehaviour
    {
        [SerializeField] GameObject originalModel;
        [SerializeField] GameObject[] characters = new GameObject[0];
        [SerializeField] GameObject[] easterEggCharacters = new GameObject[0];
        public bool EasterEggMode { get; private set; }
        GameObject[] ActiveCharacters => EasterEggMode ? easterEggCharacters : characters;
        AdventurePlayer player;
        Animator originalAnimator;
        GameObject instance;
        InputAction nextCharacter, toggleEasterEgg, toggleToon, invertMonochrome, closePicker;
        CharacterPicker picker;
        public bool PickerOpen => picker && picker.IsOpen;
        CharacterToonToggle shading;
        public bool ToonEnabled { get; private set; } = true;
        public int Index { get; private set; }
        public int Count => characters.Length + 1;
        public string CurrentName => Index == 0 ? "Silver Robot" : ActiveCharacters[Index - 1].name;
        public void Configure(GameObject original, GameObject[] variants, GameObject[] easterEggs)
        { originalModel = original; characters = variants; easterEggCharacters = easterEggs; }
        void Awake()
        {
            player = GetComponent<AdventurePlayer>();
            // Also refresh a scene kept open across asset/script updates.
            var catalog = Resources.Load<CharacterRosterCatalog>("StaffCharacterRoster");
            if (catalog && catalog.characters != null && catalog.characters.Length > 0)
            { characters = catalog.characters; easterEggCharacters = catalog.easterEggCharacters; }
            if (!originalModel && player.Animator) originalModel = player.Animator.gameObject;
            if (originalModel) originalAnimator = originalModel.GetComponent<Animator>();
            shading = new CharacterToonToggle();
            if (originalModel) shading.Apply(originalModel, ToonEnabled);
        }
        void Start()
        {
            picker = gameObject.AddComponent<CharacterPicker>();
            picker.Initialize(this, player);
            for (int i=0;i<characters.Length;i++) if (characters[i].name == "Kafka_NoCoat") { Select(i+1); break; }
        }
        public string NameAt(int index) => index == 0 ? "Silver Robot" : characters[index-1].name;
        public void TogglePicker() { if (picker) picker.SetOpen(!picker.IsOpen); }
        void OnEnable()
        {
            nextCharacter = new InputAction("NextCharacter", InputActionType.Button, "<Keyboard>/c");
            nextCharacter.Enable();
            toggleEasterEgg = new InputAction("ToggleEasterEgg", InputActionType.Button, "<Keyboard>/f8");
            toggleEasterEgg.Enable();
            toggleToon = new InputAction("ToggleToon", InputActionType.Button, "<Keyboard>/h");
            toggleToon.Enable();
            invertMonochrome = new InputAction("InvertMonochrome", InputActionType.Button, "<Keyboard>/i"); invertMonochrome.Enable();
            closePicker = new InputAction("ClosePicker", InputActionType.Button, "<Keyboard>/escape"); closePicker.Enable();
        }
        void OnDisable() { if (picker) picker.SetOpen(false, false); invertMonochrome?.Dispose(); closePicker?.Dispose(); nextCharacter?.Dispose(); nextCharacter = null; toggleEasterEgg?.Dispose(); toggleEasterEgg = null; toggleToon?.Dispose(); toggleToon = null; }
        void OnDestroy() => shading?.Dispose();
        void Update() => HandleInput(player && Application.isFocused && (player.InputCaptured || PickerOpen));
        public void HandleInput(bool accept)
        {
            if (!accept) return;
            if (nextCharacter != null && nextCharacter.WasPressedThisFrame()) { TogglePicker(); return; }
            if (PickerOpen) { if (closePicker.WasPressedThisFrame()) picker.SetOpen(false); return; }
            if (toggleToon != null && toggleToon.WasPressedThisFrame()) ToggleToon();
            if (invertMonochrome != null && invertMonochrome.WasPressedThisFrame())
                Object.FindFirstObjectByType<MonochromeMode>()?.Toggle();
            if (toggleEasterEgg != null && toggleEasterEgg.WasPressedThisFrame()) ToggleEasterEgg();
        }
        public void ToggleToon()
        {
            ToonEnabled = !ToonEnabled;
            shading.Apply(Index == 0 ? originalModel : instance, ToonEnabled);
        }
        public void Cycle() => Select((Index + 1) % Count);
        public void ToggleEasterEgg()
        {
            if (easterEggCharacters.Length != characters.Length) return;
            EasterEggMode = !EasterEggMode;
            ApplySelection(Index, true);
        }
        public void Select(int index) => ApplySelection(index, false);
        void ApplySelection(int index, bool force)
        {
            if (index < 0 || index >= Count || (!force && index == Index)) return;
            if (!player) player = GetComponent<AdventurePlayer>();
            if (!originalAnimator) originalAnimator = originalModel.GetComponent<Animator>();
            GameObject next = index == 0 ? originalModel : Instantiate(ActiveCharacters[index - 1], player.Visual, false);
            var animation = next.GetComponent<Animator>();
            if (!animation || !animation.isHuman || !animation.avatar.isValid)
            {
                if (next != originalModel) Destroy(next);
                Debug.LogError("Character switch rejected: invalid Humanoid avatar.", this); return;
            }
            next.SetActive(true);
            player.ReplaceAnimator(animation);
            if (instance) { instance.SetActive(false); Destroy(instance); }
            originalModel.SetActive(index == 0);
            instance = index == 0 ? null : next;
            Index = index;
            shading.Apply(next, ToonEnabled);
        }
    }
}
