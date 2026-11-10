using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase 18.8: builds the gunshot sound by layering clips already in the project (all Kenney, CC0), so the
/// result stays CC0 and its recipe is kept here rather than in a lost audio-editor session.
/// Each layer is a source clip with a start offset, a gain and a fade-out; the mix is trimmed, peak-normalised
/// and written as a mono 16-bit WAV into Assets/Audio/Gunshot/.
/// </summary>
public static class GunshotOptionBuilder
{
    const string OutFolder = "Assets/Audio/Gunshot";
    const int Rate = 44100;

    struct Layer
    {
        public string path;
        public float gain;
        public float startSeconds;
        /// <summary>The layer is faded to silence by this time (measured from its own start).</summary>
        public float fadeOutEnd;

        public Layer(string path, float gain, float startSeconds, float fadeOutEnd)
        {
            this.path = path; this.gain = gain; this.startSeconds = startSeconds; this.fadeOutEnd = fadeOutEnd;
        }
    }

    [MenuItem("Vantage/Audio/Rebuild Gunshot Sound")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder("Assets/Audio", "Gunshot");

        // Three were built for the pick (see CHANGELOG 18.8); this is the one chosen. The big laser with a
        // plate thump under it and a little of the old shot on top - recognisably the same gun, only heavier.
        Write("gunshot_punchy_blaster", 0.30f,
            new Layer("Assets/Audio/SciFiSounds/laserLarge_000.ogg", 0.8f, 0f, 0.30f),
            new Layer("Assets/Audio/ImpactSounds/impactPlate_heavy_001.ogg", 0.9f, 0f, 0.22f),
            new Layer("Assets/Audio/SciFiSounds/laserSmall_002.ogg", 0.5f, 0f, 0.18f));

        AssetDatabase.Refresh();
    }

    static void Write(string name, float lengthSeconds, params Layer[] layers)
    {
        int n = Mathf.RoundToInt(lengthSeconds * Rate);
        var mix = new float[n];

        for (int l = 0; l < layers.Length; l++)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(layers[l].path);
            if (clip == null) { Debug.LogError("GunshotOptionBuilder: missing " + layers[l].path); return; }
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);

            int start = Mathf.RoundToInt(layers[l].startSeconds * Rate);
            int fadeEnd = Mathf.RoundToInt(layers[l].fadeOutEnd * Rate);
            // The last 40% of each layer's window is the fade, so nothing ends on a click.
            int fadeStart = Mathf.RoundToInt(fadeEnd * 0.6f);
            for (int i = 0; i < fadeEnd && i < clip.samples && start + i < n; i++)
            {
                float v = 0f;
                for (int c = 0; c < clip.channels; c++) v += data[i * clip.channels + c];
                v /= clip.channels;
                float fade = i < fadeStart ? 1f : 1f - (i - fadeStart) / (float)(fadeEnd - fadeStart);
                mix[start + i] += v * layers[l].gain * fade;
            }
        }

        float peak = 0f;
        for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(mix[i]));
        float norm = peak > 0f ? 0.9f / peak : 1f;

        string file = OutFolder + "/" + name + ".wav";
        using (var w = new BinaryWriter(File.Create(file)))
        {
            int bytes = n * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
            w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            for (int i = 0; i < n; i++) w.Write((short)Mathf.RoundToInt(Mathf.Clamp(mix[i] * norm, -1f, 1f) * 32767f));
        }
        Debug.Log("GunshotOptionBuilder: wrote " + file);
    }
}
