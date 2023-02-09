using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Lab5Games.AudioKit
{
    public class AudioSystem : MonoBehaviour
    {
        private static AudioSystem m_instance = null;

        internal static AudioSystem current
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = FindObjectOfType<AudioSystem>();

                    if (m_instance == null)
                    {
                        GameObject go = new GameObject("[AudioSystem]");
                        m_instance = go.AddComponent<AudioSystem>();

                        Debug.LogWarning("[AudioSystem] The system has been created automatically", go);
                    }

                    m_instance.Initialize();
                }

                return m_instance;
            }
        }


        #region Static Methods
        public static Sound Music => current.m_Music;

        public static void StopAll()
        {
            current.StopAll_Internal();
        }

        public static void PlayMusic(AudioClip clip, float volume = 1f)
        {
            current.PlayMusic_Internal(clip, volume);
        }

        public static void StopMusic()
        {
            current.StopMusic_Internal();
        }

        public static Sound PlaySound(AudioClip clip, float volume, float pitch, float pan, string channel)
        {
            return current.PlaySound_Internal(clip, volume, pitch, pan, channel);
        }

        public static Sound PlaySound(AudioClip clip, float volume, string channel)
        {
            return current.PlaySound_Internal(clip, volume, 1f, 0f, channel);
        }

        public static Sound PlaySound(AudioClip clip, string channel)
        {
            return current.PlaySound_Internal(clip, 1f, 1f, 0f, channel);
        }

        public static float GetVolume(string parameter)
        {
            return current.GetVolume_Internal(parameter);
        }

        public static void SetVolume(string parameter, float volume)
        {
            current.SetVolume_Internal(parameter, volume);
        }
        #endregion

        [SerializeField]
        private AudioMixer m_Mixer;

        [SerializeField]
        private int m_AvailableSoundCount = 10;
        
        private Sound m_Music;

        private Stack<Sound> m_AvailableSounds;
        private List<Sound> m_PlayingSounds;
        private Dictionary<string, AudioMixerGroup> m_AudioChannelDict;

        private void StopAll_Internal()
        {
            Debug.Log("[AudioSystem] Stop all sound", gameObject);

            m_Music.Stop();

            for (int i = m_PlayingSounds.Count - 1; i >= 0; i--)
                m_PlayingSounds[i].Stop();
        }

        private void PlayMusic_Internal(AudioClip clip, float volume)
        {
            m_Music.Play(clip, volume, 1f, 0f);
            m_Music.loop = true;
        }

        private void StopMusic_Internal()
        {
            m_Music.Stop();
        }

        private Sound PlaySound_Internal(AudioClip clip, float volume, float pitch, float pan, string channel)
        {
            if (clip == null)
                throw new ArgumentNullException("clip is null");

            Sound sound = GetAvailableSound();

            sound.source.outputAudioMixerGroup = GetAudioChannel(channel);
            sound.Play(clip, volume, pitch, pan);

            m_PlayingSounds.Add(sound);

            return sound;
        }

        private Sound GetAvailableSound()
        {
            if (m_AvailableSounds.Count > 0)
                return m_AvailableSounds.Pop();

            Debug.LogWarning("[AudioSystem] Not enough audio sound available", gameObject);
            return null;
        }

        private AudioMixerGroup GetAudioChannel(string channel)
        {
            if(!m_AudioChannelDict.TryGetValue(channel, out AudioMixerGroup result))
            {
                Debug.LogWarning($"[AudioSystem] Not found the audio channel: {channel}", gameObject);
            }

            return result;
        }

        private float GetVolume_Internal(string parameter)
        {
            if(m_Mixer == null)
            {
                Debug.LogWarning("[AudioSystem] AudioMixer is null", gameObject);
                return 0f;
            }

            float volume = 0f;
            if(!m_Mixer.GetFloat(parameter, out volume))
            {
                Debug.LogWarning($"[AudioMixer] Not found the parameter of volume: {parameter}", gameObject);
            }

            return Mathf.InverseLerp(-80f, 0f, volume);
        }

        private void SetVolume_Internal(string parameter, float volume)
        {
            if(m_Mixer == null)
            {
                Debug.LogWarning("[AudioSystem] AudioMixer is null", gameObject);
                return;
            }

            if(!m_Mixer.SetFloat(parameter, Mathf.Lerp(-80f, 0f, Mathf.Clamp01(volume))))
            {
                Debug.LogWarning($"[AudioMixer] Not found the parameter of volume: {parameter}", gameObject);
            }
        }

        private void Initialize()
        {
            // find audio channels
            m_AudioChannelDict = new Dictionary<string, AudioMixerGroup>();

            if(m_Mixer != null)
            {
                foreach(AudioMixerGroup group in m_Mixer.FindMatchingGroups(""))
                {
                    string key = group.name;
                    Debug.Log($"[AudioSystem] Find channel: {key}", gameObject);
                    
                    m_AudioChannelDict.Add(key, group);
                }
            }
            else
            {
                Debug.LogWarning("[AudioSystem] AudioMixer is null", gameObject);
            }


            // music
            m_Music = new Sound(gameObject.AddComponent<AudioSource>());
            m_Music.source.outputAudioMixerGroup = GetAudioChannel("Music");

            // sounds
            m_AvailableSounds = new Stack<Sound>(m_AvailableSoundCount);
            m_PlayingSounds = new List<Sound>(m_AvailableSoundCount);

            for(int i=0; i<m_AudioChannelDict.Count; i++)
            {
                Sound sound = new Sound(gameObject.AddComponent<AudioSource>());
                sound.onStop += OnStopSound;    

                m_AvailableSounds.Push(sound);
            }
        }

        private void OnStopSound(Sound sound)
        {
            if(m_PlayingSounds.Remove(sound))
            {
                m_AvailableSounds.Push(sound);
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            m_instance = null;
            Debug.LogWarning("[AudioSystem] The system has been destroyed");
        }

        private void FixedUpdate()
        {
            for(int i=m_PlayingSounds.Count-1; i>=0; i--)
            {
                if (!m_PlayingSounds[i].isPlaying)
                {
                    m_PlayingSounds[i].Stop();
                }
            }
        }
    }
}
