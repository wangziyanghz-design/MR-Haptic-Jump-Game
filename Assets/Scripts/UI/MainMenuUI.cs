using MRKnobJump.Core;
using MRKnobJump.Haptics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MRKnobJump.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Text experienceText;
        [SerializeField] private Text endlessText;
        [SerializeField] private Text knobText;
        [SerializeField] private Text keyboardText;
        [SerializeField] private GameObject knobSourceGroup;
        [SerializeField] private Text realHardwareText;
        [SerializeField] private Text simulationText;
        [SerializeField] private Text hapticText;
        [SerializeField] private Button hapticButton;
        [SerializeField] private GameObject settingsPanel;
        private Selectable[] backgroundControls;
        private bool[] previousInteractable;
        [Header("Presentation")]
        [SerializeField] private Color selectedColor = new Color(0.24f, 0.95f, 1f);
        [SerializeField] private Color unselectedColor = new Color(0.72f, 0.78f, 0.88f);
        [SerializeField] private Color disabledColor = new Color(0.4f, 0.44f, 0.52f);

        private void Awake()
        {
            if (experienceText == null || endlessText == null || knobText == null || keyboardText == null ||
                knobSourceGroup == null || realHardwareText == null || simulationText == null ||
                hapticText == null || hapticButton == null || settingsPanel == null)
            {
                Debug.LogError("MainMenuUI requires all menu Text, Button and Settings references.", this);
                enabled = false;
                return;
            }
            settingsPanel.SetActive(false);
            Refresh();
        }

        public void SelectExperience() { GameSettings.SelectGameMode(GameMode.Experience); Refresh(); }
        public void SelectEndless() { GameSettings.SelectGameMode(GameMode.Endless); Refresh(); }
        public void SelectKnob() { GameSettings.SelectInputMode(InputMode.Knob); Refresh(); }
        public void SelectKeyboard() { GameSettings.SelectInputMode(InputMode.Keyboard); Refresh(); }
        public void SelectRealHardware() { GameSettings.SelectHardwareMode(HardwareMode.RealHardware); Refresh(); }
        public void SelectSimulation() { GameSettings.SelectHardwareMode(HardwareMode.Simulation); Refresh(); }
        public void ToggleHaptic() { GameSettings.SetHapticFeedback(!GameSettings.HapticFeedbackEnabled); Refresh(); }
        public void Play()
        {
            if (GameSettings.SelectedInputMode == InputMode.Knob &&
                GameSettings.SelectedHardwareMode == HardwareMode.RealHardware &&
                GameSettings.HapticFeedbackEnabled &&
                !HapticCalibration.HasCalibration)
            {
                HapticCalibration.BeginCalibration(true);
                return;
            }

            SceneManager.LoadScene("GameScene");
        }

        public void Recalibrate() => HapticCalibration.Recalibrate();
        public void ToggleSettings()
        {
            bool open = !settingsPanel.activeSelf;
            if (open)
            {
                backgroundControls = GetComponentInParent<Canvas>().GetComponentsInChildren<Selectable>(true);
                previousInteractable = new bool[backgroundControls.Length];
                for (int i = 0; i < backgroundControls.Length; i++)
                {
                    var control = backgroundControls[i];
                    previousInteractable[i] = control.interactable;
                    if (!control.transform.IsChildOf(settingsPanel.transform)) control.interactable = false;
                }
                settingsPanel.transform.SetAsLastSibling();
            }
            else if (backgroundControls != null)
            {
                for (int i = 0; i < backgroundControls.Length; i++)
                    if (backgroundControls[i] != null) backgroundControls[i].interactable = previousInteractable[i];
            }
            settingsPanel.SetActive(open);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Refresh()
        {
            bool experience = GameSettings.SelectedGameMode == GameMode.Experience;
            bool knob = GameSettings.SelectedInputMode == InputMode.Knob;
            experienceText.text = experience ? "[ EXPERIENCE ]" : "EXPERIENCE";
            endlessText.text = experience ? "ENDLESS" : "[ ENDLESS ]";
            experienceText.color = experience ? selectedColor : unselectedColor;
            endlessText.color = experience ? unselectedColor : selectedColor;
            knobText.text = knob ? "[ KNOB ]" : "KNOB";
            keyboardText.text = knob ? "KEYBOARD" : "[ KEYBOARD ]";
            knobText.color = knob ? selectedColor : unselectedColor;
            keyboardText.color = knob ? unselectedColor : selectedColor;
            bool realHardware = GameSettings.SelectedHardwareMode == HardwareMode.RealHardware;
            knobSourceGroup.SetActive(knob);
            realHardwareText.text = realHardware ? "[ REAL HARDWARE ]" : "REAL HARDWARE";
            simulationText.text = realHardware ? "SIMULATION" : "[ SIMULATION ]";
            realHardwareText.color = realHardware ? selectedColor : unselectedColor;
            simulationText.color = realHardware ? unselectedColor : selectedColor;
            hapticText.text = GameSettings.HapticFeedbackEnabled ? "[ ON ]" : "[ OFF ]";
            hapticButton.interactable = knob;
            hapticText.color = knob ? selectedColor : disabledColor;
        }
    }
}
