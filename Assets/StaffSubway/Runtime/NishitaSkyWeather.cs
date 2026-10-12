using System;
using UnityEngine;

namespace Staff.Subway
{
    // NASA Deep Star Maps 2020: shared imported HDR celestial map.
    // Cloud rendering is supplied by UnityVolumetricCloudsURP. No per-star objects or runtime texture generation.
    public sealed class NishitaSkyWeather : IDisposable
    {
        [Serializable]
        public sealed class Settings
        {
            public bool clouds = true;
            [HideInInspector] public int seed = 2718;
            [Range(0, 1)] [HideInInspector] public float cloudCoverage = .48f;
            [Range(.1f, 3)] [HideInInspector] public float cloudDensity = 1.1f;
            [Min(100)] [HideInInspector] public float cloudBaseMeters = 1400;
            [Tooltip("Offsets the distant thin cloud layer above its base height.")]
            [Min(100)] [HideInInspector] public float cloudThicknessMeters = 1800;
            [Min(1000)] [HideInInspector] public float cloudScaleMeters = 6500;
            [Range(0, 20)] [HideInInspector] public float windMetersPerSecond = 1.2f;
            [Range(0, 360)] [HideInInspector] public float windHeading = 75;
            public bool stars = true;
            [Tooltip("Optional celestial RA/Dec map. Defaults to NASA Deep Star Maps 2020 (4K).")]
            public Texture2D starMap;
            [Range(-8,8)] public float starExposure = 0;

            [Range(0, 3)] public float starBrightness = 1;
            [Tooltip("One star revolution in game seconds; 86164 approximates real sidereal time.")]
            [Min(60)] public float starRevolutionSeconds = 14400;
            [Range(0, 360)] public float starPhaseDegrees = 0;
            public bool animate = true;
        }

        Texture2D stars;
        double elapsed;
        public double ElapsedSeconds => elapsed;

        public void Advance(float deltaSeconds, Settings settings)
        {
            if (settings.animate) elapsed += Math.Max(0, deltaSeconds);
        }

        // Explicit time also supports deterministic screenshots and frame-by-frame verification.
        public void Apply(Material material, Settings s, float latitude, float northRotation, float sunElevation,
                          double seconds = -1)
        {
            material.SetFloat("_WeatherStars", s.stars ? 1 : 0);
            if (!s.stars) return;
            if (!stars) stars = Resources.Load<Texture2D>("NASA/starmap_2020_4k");
            var map = s.starMap ? s.starMap : stars;
            if (!map) { material.SetFloat("_WeatherStars",0); return; }
            double clock = seconds < 0 ? elapsed : seconds;
            material.SetTexture("_StarMap", map);
            material.SetFloat("_StarExposure", Mathf.Pow(2, s.starExposure));
            material.SetFloat("_StarBrightness", Mathf.Max(0, s.starBrightness));
            material.SetFloat("_NightVisibility", 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-12, -3, sunElevation)));
            material.SetFloat("_WeatherDaylight", Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-8, 12, sunElevation)));
            Vector3 pole = PoleDirection(latitude, northRotation);
            Vector3 east = Quaternion.Euler(0, northRotation, 0) * Vector3.right;
            Vector3 meridian = Vector3.Cross(pole, east).normalized;
            // Inverse sidereal rotation of the lookup = apparent westward motion of the stars.
            float angle = (float)(clock / Math.Max(60, s.starRevolutionSeconds) % 1) * 360 + s.starPhaseDegrees;
            Quaternion rotation = Quaternion.AngleAxis(angle, pole);
            material.SetVector("_StarEast", rotation * east);
            material.SetVector("_StarMeridian", rotation * meridian);
            material.SetVector("_StarPole", pole);
        }

        public static Vector3 PoleDirection(float latitude, float northRotation)
        {
            float p = Mathf.Clamp(latitude, -90, 90) * Mathf.Deg2Rad;
            return Quaternion.Euler(0, northRotation, 0) * new Vector3(0, Mathf.Sin(p), Mathf.Cos(p));
        }

        // Resources are shared assets: never destroy the imported NASA map.
        public void Dispose() { stars=null; }
    }
}
