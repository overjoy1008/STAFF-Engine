using System;
using UnityEngine;

namespace Staff.Subway
{
    // Procedural assets are generated once per seed and shared by all camera sky materials.
    // No UnityEngine.Random state, wall-clock time, scene geometry, or downloaded artwork.
    public sealed class NishitaSkyWeather : IDisposable
    {
        [Serializable]
        public sealed class Settings
        {
            public bool clouds = true;
            public int seed = 2718;
            [Range(0, 1)] public float cloudCoverage = .48f;
            [Range(.1f, 3)] public float cloudDensity = 1.1f;
            [Min(100)] public float cloudBaseMeters = 1400;
            [Min(100)] public float cloudThicknessMeters = 1800;
            [Min(1000)] public float cloudScaleMeters = 6500;
            [Range(0, 20)] public float windMetersPerSecond = 1.2f;
            [Range(0, 360)] public float windHeading = 75;
            [Range(16, 64)] public int cloudSteps = 64;
            public bool stars = true;
            [Range(0, 3)] public float starBrightness = 1;
            [Tooltip("One star revolution in game seconds; 86164 approximates real sidereal time.")]
            [Min(60)] public float starRevolutionSeconds = 14400;
            [Range(0, 360)] public float starPhaseDegrees = 0;
            public bool animate = true;
        }

        Texture3D volume;
        Texture2D weather, stars;
        int builtSeed;
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
            material.SetFloat("_WeatherClouds", s.clouds ? 1 : 0);
            material.SetFloat("_WeatherStars", s.stars ? 1 : 0);
            if (!s.clouds && !s.stars) return;
            if (!volume || builtSeed != s.seed) Build(s.seed);
            double clock = seconds < 0 ? elapsed : seconds;
            float scale = Mathf.Max(1000, s.cloudScaleMeters);
            double travel = clock * Math.Max(0, s.windMetersPerSecond) / scale;
            float heading = s.windHeading * Mathf.Deg2Rad;
            // Wrapped texture offsets stay precise even after days of play.
            material.SetVector("_WeatherWind", new Vector4((float)(travel * Math.Sin(heading) % 8),
                (float)(travel * Math.Cos(heading) % 8),
                (float)(travel * .65 * Math.Sin(heading) % 8), (float)(travel * .65 * Math.Cos(heading) % 8)));
            material.SetTexture("_CloudVolume", volume);
            material.SetTexture("_CloudWeather", weather);
            material.SetTexture("_StarMap", stars);
            material.SetVector("_CloudLayer", new Vector4(Mathf.Max(100, s.cloudBaseMeters),
                Mathf.Max(100, s.cloudThicknessMeters), scale, Mathf.Clamp01(s.cloudCoverage)));
            material.SetFloat("_CloudDensity", Mathf.Max(0, s.cloudDensity));
            material.SetInt("_CloudSteps", Mathf.Clamp(s.cloudSteps, 16, 64));
            material.SetFloat("_StarBrightness", Mathf.Max(0, s.starBrightness));
            material.SetFloat("_NightVisibility", 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-12, -3, sunElevation)));
            material.SetFloat("_WeatherDaylight", Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-8, 12, sunElevation)));
            Vector3 pole = PoleDirection(latitude, northRotation);
            Vector3 east = Quaternion.Euler(0, northRotation, 0) * Vector3.right;
            Vector3 meridian = Vector3.Cross(east, pole).normalized;
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

        static uint Hash(int x, int y, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)z * 2147483647u + (uint)seed * 1274126177u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }
        static float Random01(int x, int y, int z, int seed) => (Hash(x, y, z, seed) & 0xffffff) / 16777215f;
        static int Wrap(int x, int n) => (x % n + n) % n;
        static float Noise(float x, float y, float z, int period, int seed)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y), iz = Mathf.FloorToInt(z);
            float u = x - ix, v = y - iy, w = z - iz;
            u = u * u * (3 - 2 * u); v = v * v * (3 - 2 * v); w = w * w * (3 - 2 * w);
            float value = 0;
            for (int k = 0; k < 2; k++) for (int j = 0; j < 2; j++) for (int i = 0; i < 2; i++)
                value += Random01(Wrap(ix + i, period), Wrap(iy + j, period), Wrap(iz + k, period), seed)
                    * (i == 0 ? 1 - u : u) * (j == 0 ? 1 - v : v) * (k == 0 ? 1 - w : w);
            return value;
        }
        static float Fractal(float x, float y, float z, int seed)
        {
            return Noise(x * 4, y * 4, z * 4, 4, seed) * .57f
                 + Noise(x * 8, y * 8, z * 8, 8, seed + 1) * .28f
                 + Noise(x * 16, y * 16, z * 16, 16, seed + 2) * .15f;
        }

        static float WeatherFractal(float u, float v, int seed)
        {
            return Noise(u * 24, .8f, v * 24, 24, seed) * .55f
                 + Noise(u * 48, .3f, v * 48, 48, seed + 1) * .28f
                 + Noise(u * 96, .5f, v * 96, 96, seed + 2) * .17f;
        }

        void Build(int seed)
        {
            Dispose(); builtSeed = seed;
            const int size = 128;
            var voxels = new Color[size * size * size];
            for (int z = 0; z < size; z++) for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float a = x / (float)size, b = y / (float)size, c = z / (float)size;
                float broad = Fractal(a, b, c, seed);
                float detail = Fractal(a * 4, b * 4, c * 4, seed + 19);
                voxels[x + size * (y + size * z)] = new Color(broad, detail, 0, 1);
            }
            volume = new Texture3D(size, size, size, TextureFormat.RGBA32, true, true)
            { name = "Seeded cloud volume", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.HideAndDontSave };
            volume.SetPixels(voxels); volume.Apply(true, true);
            const int mapSize = 1024;
            var pixels = new Color[mapSize * mapSize];
            for (int y = 0; y < mapSize; y++) for (int x = 0; x < mapSize; x++)
            {
                float u = x / (float)mapSize, v = y / (float)mapSize;
                float warp = Fractal(u * 2, .31f, v * 2, seed + 31) - .5f;
                float shape = WeatherFractal(u + warp * .025f, v + warp * .018f, seed + 51);
                float towers = WeatherFractal(u, v, seed + 79);
                // Separate elongated high-cloud field breaks up the main cumulus silhouette.
                float wisps = Noise(u * 32, .13f, v * 96, 32, seed + 99);
                pixels[x + y * mapSize] = new Color(shape, towers, wisps, 1);
            }
            weather = new Texture2D(mapSize, mapSize, TextureFormat.RGBA32, true, true)
            { name = "Seeded cloud coverage", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.HideAndDontSave };
            weather.SetPixels(pixels); weather.Apply(true, true);
            BuildStars(seed);
        }

        void BuildStars(int seed)
        {
            const int width = 2048, height = 1024;
            var data = new Color[width * height];
            for (int n = 0; n < 2300; n++)
            {
                float longitude = Random01(n, 1, 0, seed + 101) * 2 * Mathf.PI;
                // Uniform distribution on the sphere, not uniform latitude.
                float latitude = Mathf.Asin(Mathf.Lerp(-.998f, .998f, Random01(n, 2, 0, seed + 101)));
                float brightness = Mathf.Pow(Random01(n, 3, 0, seed + 101), 5);
                float radius = Mathf.Lerp(.32f, .8f, brightness);
                float cx = longitude / (2 * Mathf.PI) * width;
                float cy = (latitude / Mathf.PI + .5f) * height;
                float stretch = 1 / Mathf.Max(.06f, Mathf.Cos(latitude));
                int rx = Mathf.CeilToInt(radius * stretch * 3), ry = Mathf.CeilToInt(radius * 3);
                Color tint = Color.Lerp(new Color(.67f, .79f, 1), new Color(1, .86f, .69f), Random01(n, 4, 0, seed + 101));
                for (int y = -ry; y <= ry; y++) for (int x = -rx; x <= rx; x++)
                {
                    int px = Mathf.FloorToInt(cx) + x, py = Mathf.FloorToInt(cy) + y;
                    if (py < 0 || py >= height) continue;
                    float dx = (px + .5f - cx) / stretch, dy = py + .5f - cy;
                    float energy = Mathf.Exp(-(dx * dx + dy * dy) / (2 * radius * radius)) * Mathf.Lerp(.12f, 1.4f, brightness);
                    data[Wrap(px, width) + py * width] += tint * energy;
                }
            }
            stars = new Texture2D(width, height, TextureFormat.RGBAHalf, true, true)
            { name = "Seeded celestial sphere", wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Clamp,
              filterMode = FilterMode.Trilinear, anisoLevel = 4, hideFlags = HideFlags.HideAndDontSave };
            stars.SetPixels(data); stars.Apply(true, true);
        }

        public void Dispose()
        {
            Release(volume); Release(weather); Release(stars);
            volume = null; weather = null; stars = null;
        }
        static void Release(UnityEngine.Object obj)
        {
            if (!obj) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj); else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
