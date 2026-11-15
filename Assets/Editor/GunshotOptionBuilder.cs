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
        /// <summary>Playback speed of the source: below 1 plays it slower and so lower, which is what makes a shot sound heavy.</summary>
        public float pitch;

        public Layer(string path, float gain, float startSeconds, float fadeOutEnd, float pitch = 1f)
        {
            this.path = path; this.gain = gain; this.startSeconds = startSeconds; this.fadeOutEnd = fadeOutEnd; this.pitch = pitch;
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

        // Phase 19.5, the other two guns, from the same family of clips so they sound like one armoury.
        // SMG: short and light - the small laser over a light plate tap, cut off early so 11 a second don't smear.
        Write("gunshot_smg", 0.14f,
            new Layer("Assets/Audio/SciFiSounds/laserSmall_002.ogg", 0.9f, 0f, 0.13f),
            new Layer("Assets/Audio/ImpactSounds/impactPlate_light_001.ogg", 0.6f, 0f, 0.10f));
        // Marksman: the heaviest of the three. Weight has to be heard on laptop speakers, which play almost nothing
        // below 120 Hz, so it comes from the crunch and the slowed laser and metal hit (100 to 1000 Hz), not from
        // the sub-bass rumble, which is kept low and cut below 55 Hz: left in, the rumble sets the peak and the
        // part people can hear ends up quiet. A long tail - one shot a second leaves room for it.
        Write("gunshot_marksman", 0.9f, 55f,
            new Layer("Assets/Audio/SciFiSounds/explosionCrunch_000.ogg", 1.3f, 0f, 0.8f, 0.75f),
            new Layer("Assets/Audio/SciFiSounds/laserLarge_000.ogg", 0.9f, 0f, 0.5f, 0.7f),
            new Layer("Assets/Audio/SciFiSounds/impactMetal_002.ogg", 0.7f, 0f, 0.25f, 0.6f),
            new Layer("Assets/Audio/ImpactSounds/impactPlate_heavy_001.ogg", 0.5f, 0f, 0.3f, 0.8f),
            new Layer("Assets/Audio/SciFiSounds/lowFrequency_explosion_000.ogg", 0.3f, 0f, 0.9f));

        AssetDatabase.Refresh();
    }

    static void Write(string name, float lengthSeconds, params Layer[] layers)
    {
        Write(name, lengthSeconds, 0f, layers);
    }

    /// <summary>highPassHz above 0 removes everything below it from the mix before it is normalised.</summary>
    static void Write(string name, float lengthSeconds, float highPassHz, params Layer[] layers)
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
            float pitch = layers[l].pitch > 0f ? layers[l].pitch : 1f;
            for (int i = 0; i < fadeEnd && start + i < n; i++)
            {
                // Read the source at i * pitch, blending the two samples either side.
                float at = i * pitch;
                int s0 = (int)at;
                if (s0 + 1 >= clip.samples) break;
                float blend = at - s0;
                float v = 0f;
                for (int c = 0; c < clip.channels; c++) v += Mathf.Lerp(data[s0 * clip.channels + c], data[(s0 + 1) * clip.channels + c], blend);
                v /= clip.channels;
                float fade = i < fadeStart ? 1f : 1f - (i - fadeStart) / (float)(fadeEnd - fadeStart);
                mix[start + i] += v * layers[l].gain * fade;
            }
        }

        if (highPassHz > 0f)
        {
            // Second-order Butterworth high-pass, run twice for a steeper cut.
            float w0 = 2f * Mathf.PI * highPassHz / Rate, cos = Mathf.Cos(w0), alpha = Mathf.Sin(w0) / Mathf.Sqrt(2f);
            float a0 = 1f + alpha, b0 = (1f + cos) / 2f / a0, b1 = -(1f + cos) / a0, a1 = -2f * cos / a0, a2 = (1f - alpha) / a0;
            for (int pass = 0; pass < 2; pass++)
            {
                float x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f;
                for (int i = 0; i < n; i++)
                {
                    float x = mix[i];
                    float y = b0 * x + b1 * x1 + b0 * x2 - a1 * y1 - a2 * y2;
                    x2 = x1; x1 = x; y2 = y1; y1 = y;
                    mix[i] = y;
                }
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
