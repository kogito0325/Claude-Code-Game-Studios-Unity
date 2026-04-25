using UnityEngine;

namespace Proto.Sample.BlueArch
{
    public static class BlueSfx
    {
        public static void Play(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
        }
    }
}
