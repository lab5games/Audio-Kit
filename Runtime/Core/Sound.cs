using UnityEngine;
using System.Collections;

namespace Lab5Games.AudioKit
{
    public class Sound 
    {
        AudioSource m_Source;
        public AudioSource source => m_Source;

        Coroutine m_FadeOutRoutine = null;

        public bool loop
        {
            get => m_Source.loop;
            set => m_Source.loop = value;
        }

        public bool isPlaying => m_Source.isPlaying;

        public delegate void SoundDelegate(Sound sound);
        public event SoundDelegate onStop;

        public Sound(AudioSource source)
        {
            m_Source = source;
            m_Source.playOnAwake = false;
        }

        public void Stop()
        {
            m_Source.Stop();
            m_Source.clip = null;

            if(m_FadeOutRoutine != null)
            {
                AudioSystem.current.StopCoroutine(m_FadeOutRoutine);
                m_FadeOutRoutine = null;
            }

            onStop?.Invoke(this);
        }

        public void Play(AudioClip clip, float volume, float pitch, float pan)
        {
            m_Source.clip = clip;
            m_Source.volume = volume;
            m_Source.pitch = pitch;
            m_Source.panStereo = pan;
            m_Source.loop = false;

            m_Source.Play();
        }

        public void Pause()
        {
            m_Source.Pause();
        }

        public void UnPause()
        {
            m_Source.UnPause();
        }

        public void FadeOut(float fadeOutTime)
        {
            m_FadeOutRoutine = AudioSystem.current.StartCoroutine(FadeOutTask(fadeOutTime));
        }

        IEnumerator FadeOutTask(float time)
        {
            float startVol = source.volume;

            while(source.volume > 0)
            {
                source.volume -= ((startVol / time) * Time.deltaTime);
                yield return null;
            }

            m_FadeOutRoutine = null;
            Stop();
        }
    }
}
