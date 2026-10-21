using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    [RequireComponent(typeof(AudioSource))]
    public class AgentSpeaker : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float smoothFollowSpeed = 6f;

        private struct SpeechQueueItem
        {
            public AudioClip clip;
            public string text;
        }

        private readonly Queue<SpeechQueueItem> speechQueue = new Queue<SpeechQueueItem>();
        private Coroutine playbackCoroutine;
        private Transform activeAnchorTransform;

        public bool IsSpeaking => (audioSource != null && audioSource.isPlaying) || speechQueue.Count > 0;

        public event Action<string> OnSentenceStarted;
        public event Action OnSpeakingComplete;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D Audio
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 0.5f;
            audioSource.maxDistance = 15f;
            audioSource.playOnAwake = false;
        }

        public void SetSpatialAnchor(Transform anchor)
        {
            activeAnchorTransform = anchor;
        }

        public void EnqueueSpeech(AudioClip clip, string text)
        {
            if (clip == null) return;
            speechQueue.Enqueue(new SpeechQueueItem { clip = clip, text = text });

            if (playbackCoroutine == null)
            {
                playbackCoroutine = StartCoroutine(ProcessPlaybackQueue());
            }
        }

        public void StopSpeaking()
        {
            speechQueue.Clear();
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                playbackCoroutine = null;
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            OnSpeakingComplete?.Invoke();
        }

        private IEnumerator ProcessPlaybackQueue()
        {
            // Duck any legacy equipment audio sources
            DuckOtherAudioSources();

            while (speechQueue.Count > 0)
            {
                var item = speechQueue.Dequeue();
                if (item.clip == null) continue;

                audioSource.clip = item.clip;
                audioSource.Play();
                OnSentenceStarted?.Invoke(item.text);

                while (audioSource.isPlaying)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(0.08f); // Natural breathing pause between sentences
            }

            playbackCoroutine = null;
            OnSpeakingComplete?.Invoke();
        }

        public static void DuckOtherAudioSources()
        {
            // Find any playing AudioSource on equipment and stop it
            AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (var src in allSources)
            {
                if (src == null || !src.isPlaying) continue;
                // If it belongs to an interactable equipment, stop it
                if (src.GetComponentInParent<InteractableLabel>() != null || src.GetComponentInParent<PCInteractionTester>() != null)
                {
                    src.Stop();
                }
            }
        }

        private void LateUpdate()
        {
            if (activeAnchorTransform != null)
            {
                transform.position = Vector3.Lerp(transform.position, activeAnchorTransform.position, Time.deltaTime * smoothFollowSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, activeAnchorTransform.rotation, Time.deltaTime * smoothFollowSpeed);
            }
            else if (Camera.main != null)
            {
                // Default floating anchor: slightly to the left and forward of user's head
                Transform cam = Camera.main.transform;
                Vector3 defaultPos = cam.position + cam.forward * 0.8f - cam.right * 0.35f + cam.up * 0.1f;
                transform.position = Vector3.Lerp(transform.position, defaultPos, Time.deltaTime * smoothFollowSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, cam.rotation, Time.deltaTime * smoothFollowSpeed);
            }
        }
    }
}
