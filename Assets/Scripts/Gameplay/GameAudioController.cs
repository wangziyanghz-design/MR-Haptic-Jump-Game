using System;
using System.Collections.Generic;
using MRKnobJump.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MRKnobJump.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameAudioController : MonoBehaviour
    {
        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.85f;

        [Header("Optional Audio Overrides")]
        [SerializeField] private AudioClip chargeClip;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip perfectClip;
        [SerializeField] private AudioClip failClip;
        [SerializeField] private AudioClip gameOverClip;
        [SerializeField] private AudioClip uiClickClip;

        [Header("Sources (created automatically when empty)")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource chargeSource;

        private static GameAudioController instance;
        private readonly List<Button> hookedButtons = new List<Button>();
        private readonly List<AudioClip> generatedClips = new List<AudioClip>();
        private CompressionController compressionController;
        private PlayerJumpController jumpController;
        private PlayerLandingDetector landingDetector;
        private LivesManager livesManager;
        private bool isPrimary;
        private bool gameOverPlayed;

        public float MasterVolume => masterVolume;
        public float SfxVolume => sfxVolume;
        public bool ChargeAudioActive => chargeSource != null && chargeSource.isPlaying;
        public int JumpPlayCount { get; private set; }
        public int SuccessPlayCount { get; private set; }
        public int PerfectPlayCount { get; private set; }
        public int FailPlayCount { get; private set; }
        public int GameOverPlayCount { get; private set; }
        public int UiClickPlayCount { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            isPrimary = true;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
            EnsureClips();
        }

        private void OnEnable()
        {
            if (!isPrimary) return;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (isPrimary) HookScene(SceneManager.GetActiveScene());
        }

        private void OnDisable()
        {
            if (!isPrimary) return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnhookScene();
            StopChargeImmediately();
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            for (int i = 0; i < generatedClips.Count; i++)
            {
                if (generatedClips[i] != null) Destroy(generatedClips[i]);
            }
            generatedClips.Clear();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => HookScene(scene);

        private void HookScene(Scene scene)
        {
            UnhookScene();
            compressionController = FindInScene<CompressionController>(scene);
            jumpController = FindInScene<PlayerJumpController>(scene);
            landingDetector = FindInScene<PlayerLandingDetector>(scene);
            livesManager = FindInScene<LivesManager>(scene);
            gameOverPlayed = livesManager != null && livesManager.IsGameOver;

            if (jumpController != null) jumpController.JumpStarted += HandleJumpStarted;
            if (landingDetector != null) landingDetector.LandingResolved += HandleLanding;

            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button.gameObject.scene != scene) continue;
                button.onClick.AddListener(PlayUiClick);
                hookedButtons.Add(button);
            }
        }

        private void UnhookScene()
        {
            if (jumpController != null) jumpController.JumpStarted -= HandleJumpStarted;
            if (landingDetector != null) landingDetector.LandingResolved -= HandleLanding;

            for (int i = 0; i < hookedButtons.Count; i++)
            {
                if (hookedButtons[i] != null) hookedButtons[i].onClick.RemoveListener(PlayUiClick);
            }
            hookedButtons.Clear();
            compressionController = null;
            jumpController = null;
            landingDetector = null;
            livesManager = null;
        }

        private void Update()
        {
            UpdateChargeAudio();

            if (livesManager != null && livesManager.IsGameOver && !gameOverPlayed)
            {
                gameOverPlayed = true;
                StopChargeImmediately();
                PlayOneShot(gameOverClip, 1f);
                GameOverPlayCount++;
            }
        }

        private void UpdateChargeAudio()
        {
            bool canCharge = compressionController != null && jumpController != null &&
                jumpController.JumpEnabled && !jumpController.IsAirborne &&
                (livesManager == null || !livesManager.IsGameOver);
            float compression = canCharge ? compressionController.Compression : 0f;
            float targetVolume = compression > 0.005f
                ? masterVolume * sfxVolume * Mathf.Lerp(0.015f, 0.07f, compression)
                : 0f;

            chargeSource.volume = Mathf.MoveTowards(
                chargeSource.volume,
                targetVolume,
                Time.unscaledDeltaTime * 0.3f);
            chargeSource.pitch = Mathf.Lerp(0.82f, 1.18f, compression);

            if (targetVolume > 0f && !chargeSource.isPlaying)
            {
                chargeSource.clip = chargeClip;
                chargeSource.Play();
            }
            else if (targetVolume <= 0f && chargeSource.isPlaying && chargeSource.volume <= 0.001f)
            {
                chargeSource.Stop();
            }
        }

        private void HandleJumpStarted()
        {
            StopChargeImmediately();
            PlayOneShot(jumpClip, 0.8f);
            JumpPlayCount++;
        }

        private void HandleLanding(LandingResult result)
        {
            switch (result)
            {
                case LandingResult.Perfect:
                    PlayOneShot(perfectClip, 1f);
                    PerfectPlayCount++;
                    break;
                case LandingResult.Success:
                    PlayOneShot(successClip, 0.72f);
                    SuccessPlayCount++;
                    break;
                default:
                    PlayOneShot(failClip, 0.78f);
                    FailPlayCount++;
                    break;
            }
        }

        private void PlayUiClick()
        {
            PlayOneShot(uiClickClip, 0.5f);
            UiClickPlayCount++;
        }

        private void PlayOneShot(AudioClip clip, float relativeVolume)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, masterVolume * sfxVolume * relativeVolume);
        }

        private void StopChargeImmediately()
        {
            if (chargeSource == null) return;
            chargeSource.Stop();
            chargeSource.volume = 0f;
        }

        private void EnsureSources()
        {
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (chargeSource == null) chargeSource = gameObject.AddComponent<AudioSource>();

            ConfigureSource(sfxSource, false);
            ConfigureSource(chargeSource, true);
        }

        private static void ConfigureSource(AudioSource source, bool loop)
        {
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = loop ? 0f : 1f;
        }

        private void EnsureClips()
        {
            if (chargeClip == null) chargeClip = Track(CreateSustainedTone("Charge", 110f, 0.4f, 0.18f));
            if (jumpClip == null) jumpClip = Track(CreateSweep("Jump", 0.16f, 240f, 560f, 0.34f));
            if (successClip == null) successClip = Track(CreateSequence("Success", 0.28f, 0.28f, 440f, 587f));
            if (perfectClip == null) perfectClip = Track(CreateSequence("Perfect", 0.48f, 0.38f, 523f, 659f, 784f));
            if (failClip == null) failClip = Track(CreateSequence("Fail", 0.34f, 0.24f, 330f, 247f));
            if (gameOverClip == null) gameOverClip = Track(CreateSequence("GameOver", 0.65f, 0.3f, 392f, 330f, 262f));
            if (uiClickClip == null) uiClickClip = Track(CreateSweep("UiClick", 0.07f, 720f, 600f, 0.22f));
            chargeSource.clip = chargeClip;
        }

        private AudioClip Track(AudioClip clip)
        {
            generatedClips.Add(clip);
            return clip;
        }

        private static AudioClip CreateSustainedTone(string name, float frequency, float duration, float amplitude)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / sampleRate;
                samples[i] = amplitude * (Mathf.Sin(2f * Mathf.PI * frequency * time) +
                    0.18f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * time));
            }
            return CreateClip(name, samples, sampleRate);
        }

        private static AudioClip CreateSweep(string name, float duration, float startFrequency, float endFrequency, float amplitude)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = (float)i / Mathf.Max(1, sampleCount - 1);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += 2f * Mathf.PI * frequency / sampleRate;
                samples[i] = Mathf.Sin(phase) * amplitude * Envelope(progress);
            }
            return CreateClip(name, samples, sampleRate);
        }

        private static AudioClip CreateSequence(string name, float duration, float amplitude, params float[] frequencies)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = (float)i / Mathf.Max(1, sampleCount - 1);
                int note = Mathf.Min(frequencies.Length - 1, Mathf.FloorToInt(progress * frequencies.Length));
                phase += 2f * Mathf.PI * frequencies[note] / sampleRate;
                samples[i] = (Mathf.Sin(phase) + 0.16f * Mathf.Sin(phase * 2f)) * amplitude * Envelope(progress);
            }
            return CreateClip(name, samples, sampleRate);
        }

        private static float Envelope(float progress)
        {
            float attack = Mathf.Clamp01(progress / 0.08f);
            float release = Mathf.Clamp01((1f - progress) / 0.18f);
            return Mathf.Min(attack, release);
        }

        private static AudioClip CreateClip(string name, float[] samples, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            T[] components = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.scene == scene) return components[i];
            }
            return null;
        }

        private void OnValidate()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
        }
    }
}
