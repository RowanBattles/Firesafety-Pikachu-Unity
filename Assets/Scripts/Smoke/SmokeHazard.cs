using System.Collections.Generic;
using FireSafety.Fire;
using FireSafety.Movement;
using UnityEngine;

namespace FireSafety.Smoke
{
    public sealed class SmokeHazard : MonoBehaviour
    {
        [SerializeField, Min(20f)] private float fullyDevelopedSizeCm = 200f;
        [SerializeField, Min(0.1f)] private float spreadMetersPerSecond = 2f;
        [SerializeField, Min(1f)] private float lethalDoseSeconds = 12f;
        [SerializeField, Range(0f, 1f)] private float coughVolume = 0.7f;
        public SmokeExposure Exposure { get; private set; }
        public bool Active { get; private set; }
        private SmokeSimulation simulation;
        private HousePrototype house;
        private FireHazard fire;
        private Transform player;
        private ParticleSystem particles;
        private Material material;
        private Texture2D field;
        private Color32[] fieldPixels;
        private bool[] passage;
        private int passageVersion = -1, fieldRevision = -1, cursor;
        private readonly List<int> smokyCells = new List<int>();
        private float emissionBudget, nextCough;
        private AudioSource voice;
        private AudioClip cough;

        public void Initialize(HousePrototype layout, FireHazard source, Transform character)
        {
            house = layout; fire = source; player = character;
            simulation = new SmokeSimulation(house.GridWidth, house.GridHeight, house.GridCellSize, spreadMetersPerSecond);
            Exposure = new SmokeExposure(lethalDoseSeconds);
            field = new Texture2D(house.GridWidth, house.GridHeight, TextureFormat.RGBA32, false, true)
                { name = "Smoke density field", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            fieldPixels = new Color32[house.GridWidth * house.GridHeight];
            field.SetPixels32(fieldPixels); field.Apply(false);
            material = new Material(Resources.Load<Shader>("Effects/SmokeParticles"));
            material.SetTexture("_SmokeField", field);
            material.SetVector("_SmokeOrigin", new Vector4(house.GridOrigin.x, house.GridOrigin.z, 0f, 0f));
            material.SetVector("_SmokeExtent", new Vector4(house.GridWidth * house.GridCellSize, house.GridHeight * house.GridCellSize, 0f, 0f));

            particles = gameObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false; main.loop = true; main.maxParticles = 5000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
            main.startSpeed = 0f;
            main.startColor = new Color(0.38f, 0.4f, 0.42f, 0.55f);
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape; shape.enabled = false;
            var size = particles.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.8f)));
            var color = particles.colorOverLifetime; color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(fade);
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            particles.Play();

            cough = Resources.Load<AudioClip>("Audio/freesound_community-cough-104521");
            voice = player.gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false; voice.spatialBlend = 0f; voice.volume = coughVolume;
        }

        public void Tick(float seconds)
        {
            if (!Active && fire.Simulation.SizeCm >= fullyDevelopedSizeCm) Active = true;
            if (Active)
            {
                bool changedPassage = passageVersion != house.OcclusionVersion;
                if (changedPassage) { passage = house.CreateFirePassageMask(); passageVersion = house.OcclusionVersion; }
                simulation.Advance(seconds, passage, fire.Simulation.IsBurning);
                if (changedPassage || fieldRevision != simulation.Revision) RefreshField();
                EmitParticles(seconds);
            }
            int cell = house.WorldToCell(player.position);
            float density = Active && cell >= 0 && passage[cell] ? simulation.DensityAt(cell) : 0f;
            Exposure.Advance(seconds, density);
            if (cough != null && Exposure.ShouldCough && !voice.isPlaying && Time.time >= nextCough)
            {
                voice.pitch = Random.Range(0.94f, 1.04f);
                voice.PlayOneShot(cough);
                nextCough = Time.time + Mathf.Max(cough.length / voice.pitch + 0.2f,
                    Mathf.Lerp(3.5f, 1.2f, Exposure.Fraction));
            }
            if (!Exposure.ShouldCough) nextCough = Time.time;
        }

        public void StopCoughing() { if (voice != null) voice.Stop(); }

        private void RefreshField()
        {
            smokyCells.Clear();
            for (int cell = 0; cell < fieldPixels.Length; cell++)
            {
                float density = passage[cell] ? simulation.DensityAt(cell) : 0f;
                fieldPixels[cell] = new Color32((byte)(Mathf.Clamp01(density) * 255), 0, 0, 255);
                if (density >= 0.05f) smokyCells.Add(cell);
            }
            field.SetPixels32(fieldPixels); field.Apply(false);
            fieldRevision = simulation.Revision;
        }

        private void EmitParticles(float seconds)
        {
            if (smokyCells.Count == 0) return;
            emissionBudget += seconds * Mathf.Min(900f, smokyCells.Count * 1.5f);
            int count = Mathf.FloorToInt(emissionBudget);
            emissionBudget -= count;
            for (int i = 0; i < count; i++)
            {
                int cell = smokyCells[cursor % smokyCells.Count];
                cursor = (cursor + 1) % smokyCells.Count;
                Vector3 point = house.CellToWorld(cell);
                var puff = new ParticleSystem.EmitParams
                {
                    position = new Vector3(point.x + Random.Range(-0.18f, 0.18f), Random.Range(1.9f, 2.5f), point.z + Random.Range(-0.18f, 0.18f)),
                    velocity = Vector3.up * Random.Range(0.12f, 0.3f)
                };
                particles.Emit(puff, 1);
            }
        }

        private void OnDestroy()
        {
            StopCoughing();
            if (field != null) Destroy(field);
            if (material != null) Destroy(material);
        }
    }
}
