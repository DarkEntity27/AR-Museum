using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARMuseum.Audio
{
    public enum NarrationState
    {
        Initializing,
        Ready,
        Playing,
        Paused,
        Finished,
        Unavailable
    }

    /// <summary>
    /// Narrates exhibit text with the device's native Android text-to-speech engine.
    ///
    /// Android's TextToSpeech has no true pause/resume, so this service speaks the narration one sentence
    /// at a time. Pause stops the engine and remembers the sentence; Resume speaks that sentence again from
    /// its start. Finishing a sentence is detected by polling TextToSpeech.isSpeaking(), so no extra Java
    /// listener classes are needed.
    ///
    /// One engine is created once (asynchronously) and reused for every tap. Only one session runs at a
    /// time. Outside Android (e.g. the Unity Editor) the service reports itself as unavailable and never
    /// claims to play audio.
    /// </summary>
    [DisallowMultipleComponent]
    public class NarrationService : MonoBehaviour
    {
        [Tooltip("Speech rate multiplier (1 = engine default).")]
        [SerializeField] private float speechRate = 0.95f;
        [Tooltip("Pitch multiplier (1 = engine default).")]
        [SerializeField] private float pitch = 1f;
        [Tooltip("How often (seconds) to check whether the current sentence has finished.")]
        [SerializeField] private float pollInterval = 0.1f;

        private const int QueueFlush = 0;          // TextToSpeech.QUEUE_FLUSH
        private const int TtsSuccess = 0;          // TextToSpeech.SUCCESS
        private const int NoInitStatus = int.MinValue;
        private const float StartGrace = 0.3f;     // isSpeaking() can lag right after speak()

        private readonly List<string> m_Sentences = new List<string>();
        private UnityEngine.Object m_Key;
        private int m_Index;
        private NarrationState m_State = NarrationState.Initializing;
        private string m_UnavailableReason;
        private float m_PollTimer;
        private float m_SpeakStartedAt;
        private volatile int m_PendingInitStatus = NoInitStatus;
        private bool m_UsingClip;
        private bool m_EngineReady;
        private bool m_EngineFailed;

        [Tooltip("Plays recorded narration clips. Created automatically if left empty.")]
        [SerializeField] private AudioSource narrationSource;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject m_Tts;
        private InitListenerProxy m_InitProxy;
#endif

        public NarrationState State => m_State;

        /// <summary>The object (e.g. ExhibitData) whose narration is currently loaded, or null.</summary>
        public UnityEngine.Object CurrentKey => m_Key;

        public string StatusMessage
        {
            get
            {
                switch (m_State)
                {
                    case NarrationState.Initializing: return "Preparing voice...";
                    case NarrationState.Ready: return "Ready";
                    case NarrationState.Playing: return "Playing";
                    case NarrationState.Paused: return "Paused";
                    case NarrationState.Finished: return "Finished";
                    default: return m_UnavailableReason ?? "Narration unavailable";
                }
            }
        }

private void Awake()
        {
            if (narrationSource == null)
                narrationSource = gameObject.AddComponent<AudioSource>();
            narrationSource.playOnAwake = false;
            narrationSource.loop = false;
            narrationSource.spatialBlend = 0f;   // narration is heard the same from any position

#if UNITY_ANDROID && !UNITY_EDITOR
            StartEngine();
#else
            SetUnavailable("Narration plays on Android devices.");
#endif
        }

private void Update()
        {
            if (m_PendingInitStatus != NoInitStatus)
            {
                int status = m_PendingInitStatus;
                m_PendingInitStatus = NoInitStatus;
                OnEngineInitialised(status);
            }

            if (m_State != NarrationState.Playing)
                return;

            m_PollTimer += Time.unscaledDeltaTime;
            if (m_PollTimer < pollInterval)
                return;
            m_PollTimer = 0f;

            if (m_UsingClip)
            {
                if (!narrationSource.isPlaying)
                    FinishSession();
                return;
            }

            if (Time.unscaledTime - m_SpeakStartedAt < StartGrace)
                return;

            if (!IsEngineSpeaking())
                AdvanceToNextSentence();
        }

        private void OnApplicationPause(bool paused)
        {
            // Backgrounding can silence the engine; keep the state honest by pausing.
            if (paused && m_State == NarrationState.Playing)
                Pause();
        }

        private void OnDestroy()
        {
            ShutdownEngine();
        }

        private void OnApplicationQuit()
        {
            ShutdownEngine();
        }

        /// <summary>
        /// Starts narrating the given text from the beginning. Ignored if a session is already playing,
        /// or if the voice is still starting or unavailable. Resumes if the same key is paused.
        /// </summary>
/// <summary>
        /// Starts narration for an exhibit. A recorded clip is used if provided, otherwise text-to-speech.
        /// Ignored while a session is already playing. Resumes if the same exhibit is paused.
        /// </summary>
        public void Play(string text, AudioClip clip, UnityEngine.Object key)
        {
            if (m_State == NarrationState.Playing)
                return;

            if (m_State == NarrationState.Paused)
            {
                if (m_Key == key)
                {
                    Resume();
                    return;
                }

                // A different exhibit was paused: end that session before starting this one.
                Stop();
            }

            if (clip != null)
            {
                PlayClip(clip, key);
                return;
            }

            if (m_State != NarrationState.Ready && m_State != NarrationState.Finished)
                return;

            BuildSentences(text);
            if (m_Sentences.Count == 0)
                return;

            m_Key = key;
            m_Index = 0;
            m_UsingClip = false;
            m_State = NarrationState.Playing;
            SpeakSentence(m_Index);
        }

        /// <summary>Stops speech and remembers the current sentence. Resume continues from it.</summary>
/// <summary>Pauses a recorded clip exactly, or stops text-to-speech and remembers the current sentence.</summary>
        public void Pause()
        {
            if (m_State != NarrationState.Playing)
                return;

            if (m_UsingClip)
                narrationSource.Pause();
            else
                StopEngine();

            m_State = NarrationState.Paused;
        }

        /// <summary>Speaks the paused sentence again from its start.</summary>
/// <summary>Continues a paused clip, or speaks the paused sentence again from its start.</summary>
        public void Resume()
        {
            if (m_State != NarrationState.Paused)
                return;

            m_State = NarrationState.Playing;
            if (m_UsingClip)
            {
                narrationSource.UnPause();
                m_PollTimer = 0f;
            }
            else
            {
                SpeakSentence(m_Index);
            }
        }

        /// <summary>Stops speech and clears the session.</summary>
/// <summary>Stops the current session and clears it.</summary>
        public void Stop()
        {
            if (m_State != NarrationState.Playing && m_State != NarrationState.Paused)
                return;

            if (m_UsingClip)
            {
                narrationSource.Stop();
                narrationSource.clip = null;
            }
            else
            {
                StopEngine();
            }

            m_UsingClip = false;
            m_Index = 0;
            m_Key = null;
            m_State = GetIdleState();
        }

        private void AdvanceToNextSentence()
        {
            m_Index++;
            if (m_Index >= m_Sentences.Count)
            {
                m_Index = 0;
                m_Key = null;
                m_State = NarrationState.Finished;
                return;
            }

            SpeakSentence(m_Index);
        }

        /// <summary>Splits narration into sentences so pause/resume has a useful granularity.</summary>
        private void BuildSentences(string text)
        {
            m_Sentences.Clear();
            if (string.IsNullOrWhiteSpace(text))
                return;

            string[] lines = text.Split('\n');
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0)
                    continue;

                int start = 0;
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    bool endsSentence = c == '.' || c == '!' || c == '?';
                    bool atEnd = i == line.Length - 1;
                    bool followedBySpace = !atEnd && char.IsWhiteSpace(line[i + 1]);
                    if (endsSentence && (atEnd || followedBySpace))
                    {
                        string sentence = line.Substring(start, i - start + 1).Trim();
                        if (sentence.Length > 0)
                            m_Sentences.Add(sentence);
                        start = i + 1;
                    }
                }

                if (start < line.Length)
                {
                    string tail = line.Substring(start).Trim();
                    if (tail.Length > 0)
                        m_Sentences.Add(tail);
                }
            }
        }

private void SetUnavailable(string reason)
        {
            m_UnavailableReason = reason;
            m_EngineFailed = true;
            // Never interrupt a session that is already running (e.g. a recorded clip).
            if (m_State != NarrationState.Playing && m_State != NarrationState.Paused)
                m_State = NarrationState.Unavailable;
        }

        // ---------------------------------------------------------------------------------------------
        // Android TextToSpeech
        // ---------------------------------------------------------------------------------------------

        private void StartEngine()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    m_InitProxy = new InitListenerProxy(status => m_PendingInitStatus = status);
                    m_Tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, m_InitProxy);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Narration: could not create text-to-speech engine: " + e.Message, this);
                SetUnavailable("Voice not available on this device.");
            }
#endif
        }

private void OnEngineInitialised(int status)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (status != TtsSuccess || m_Tts == null)
            {
                SetUnavailable("Text-to-speech engine unavailable.");
                ShutdownEngine();
                return;
            }

            // Prefer English (India); fall back to US English if the voice pack lacks it.
            int langResult = SetEngineLanguage("en", "IN");
            if (langResult < 0)
                langResult = SetEngineLanguage("en", "US");

            if (langResult < 0)
            {
                SetUnavailable("English voice data missing. Install it in Android text-to-speech settings.");
                return;
            }

            m_Tts.Call<int>("setSpeechRate", speechRate);
            m_Tts.Call<int>("setPitch", pitch);
            m_EngineReady = true;
            if (m_State == NarrationState.Initializing)
                m_State = NarrationState.Ready;
#else
            SetUnavailable("Narration plays on Android devices.");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private int SetEngineLanguage(string language, string country)
        {
            using (var locale = new AndroidJavaObject("java.util.Locale", language, country))
            {
                return m_Tts.Call<int>("setLanguage", locale);
            }
        }
#endif

        private void SpeakSentence(int index)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (m_Tts == null)
                return;

            m_Tts.Call<int>("speak", m_Sentences[index], QueueFlush, null, "narration_" + index);
            m_SpeakStartedAt = Time.unscaledTime;
            m_PollTimer = 0f;
#endif
        }

        private void StopEngine()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (m_Tts != null)
                m_Tts.Call<int>("stop");
#endif
        }

        private bool IsEngineSpeaking()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return m_Tts != null && m_Tts.Call<bool>("isSpeaking");
#else
            return false;
#endif
        }

        private void ShutdownEngine()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (m_Tts != null)
            {
                try
                {
                    m_Tts.Call<int>("stop");
                    m_Tts.Call("shutdown");
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Narration: error while shutting down engine: " + e.Message, this);
                }

                m_Tts.Dispose();
                m_Tts = null;
            }

            if (m_InitProxy != null)
            {
                // AndroidJavaProxy has no Dispose(); dropping the reference lets it be collected.
                m_InitProxy = null;
            }
#endif
            m_Key = null;
            m_Index = 0;
            if (m_State != NarrationState.Unavailable)
                m_State = NarrationState.Unavailable;
            if (string.IsNullOrEmpty(m_UnavailableReason))
                m_UnavailableReason = "Narration stopped.";
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>Receives TextToSpeech.OnInitListener.onInit(int) from Java.</summary>
        private class InitListenerProxy : AndroidJavaProxy
        {
            private readonly Action<int> m_Callback;

            public InitListenerProxy(Action<int> callback)
                : base("android.speech.tts.TextToSpeech$OnInitListener")
            {
                m_Callback = callback;
            }

            public void onInit(int status)
            {
                m_Callback?.Invoke(status);
            }
        }
#endif
    

/// <summary>The state to return to when no session is running.</summary>
        private NarrationState GetIdleState()
        {
            if (m_EngineFailed)
                return NarrationState.Unavailable;
            return m_EngineReady ? NarrationState.Ready : NarrationState.Initializing;
        }

        private void PlayClip(AudioClip clip, UnityEngine.Object key)
        {
            m_Key = key;
            m_UsingClip = true;
            m_PollTimer = 0f;
            narrationSource.clip = clip;
            narrationSource.Play();
            m_State = NarrationState.Playing;
        }

        private void FinishSession()
        {
            m_UsingClip = false;
            m_Index = 0;
            m_Key = null;
            m_State = NarrationState.Finished;
        }

        /// <summary>True when Listen should be offered for an exhibit with the given narration sources.</summary>
        public bool CanPlay(bool hasClip, bool hasText)
        {
            if (m_State == NarrationState.Playing || m_State == NarrationState.Paused)
                return false;
            if (hasClip)
                return true;
            if (!hasText)
                return false;
            return m_State == NarrationState.Ready || m_State == NarrationState.Finished;
        }

        /// <summary>Status line for an exhibit, taking its narration sources into account.</summary>
        public string GetStatus(bool hasClip, bool hasText)
        {
            if (m_State == NarrationState.Playing || m_State == NarrationState.Paused || m_State == NarrationState.Finished)
                return StatusMessage;
            if (hasClip)
                return "Ready";   // recorded narration does not need text-to-speech
            if (!hasText)
                return "No narration for this exhibit.";
            return StatusMessage;
        }
}
}
