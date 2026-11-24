using System.Collections.Generic;
using ThroatBalls.Logic;
using ThunderRoad;
using UnityEngine;

namespace ThroatBalls
{
    /// <summary>
    /// Show Zones: the throat and groin zones on every living NPC as see-through spheres, and a short marker
    /// on each judged hit (green counted, red refused). Unity gizmos do not render in a built game, so these
    /// are plain sphere meshes whose collider is turned off and removed (the Heel of Achilles approach). Costs nothing while off.
    /// </summary>
    internal sealed class ZoneMarkers
    {
        private const float HitMarkerDiameter = 0.03f;
        private const float HitMarkerSeconds = 5f;

        // Shader candidates, first found wins; the chosen one is logged once.
        private static readonly string[] ShaderCandidates =
        {
            "Sprites/Default",
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
        };

        private static readonly Color ThroatColor = new Color(0.2f, 0.6f, 1f, 0.35f);
        private static readonly Color GroinColor = new Color(1f, 0.5f, 0f, 0.35f);
        private static readonly Color CountedColor = new Color(0f, 1f, 0f, 0.9f);
        private static readonly Color RefusedColor = new Color(1f, 0f, 0f, 0.9f);

        private readonly Dictionary<Creature, GameObject[]> zones = new Dictionary<Creature, GameObject[]>();
        private readonly List<KeyValuePair<GameObject, float>> hitMarkers = new List<KeyValuePair<GameObject, float>>();
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private readonly List<Creature> stale = new List<Creature>();
        private readonly LowBlowSettings settings = new LowBlowSettings();

        private Shader shader;
        private bool shaderResolved;

        public void Update()
        {
            if (ModOptions.ShowZones && ModOptions.ModEnabled)
            {
                PlaceZones();
            }
            else if (zones.Count > 0)
            {
                ClearZones();
            }
            ExpireHitMarkers();
        }

        public void AddHitMarker(Vector3 position, bool counted)
        {
            if (!ModOptions.ShowZones)
            {
                return;
            }
            var marker = CreateSphere("ThroatBalls.Hit", counted ? CountedColor : RefusedColor);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * HitMarkerDiameter;
            hitMarkers.Add(new KeyValuePair<GameObject, float>(marker, Time.time + HitMarkerSeconds));
        }

        public void Clear()
        {
            ClearZones();
            foreach (var entry in hitMarkers)
            {
                Destroy(entry.Key);
            }
            hitMarkers.Clear();
        }

        private void PlaceZones()
        {
            ModOptions.Fill(settings);
            foreach (var creature in Creature.allActive)
            {
                if (creature == null || creature.isKilled || creature.isPlayer)
                {
                    continue;
                }
                if (!zones.TryGetValue(creature, out var markers))
                {
                    markers = new[] { CreateSphere("ThroatBalls.Throat", ThroatColor), CreateSphere("ThroatBalls.Groin", GroinColor) };
                    zones[creature] = markers;
                }
                var forward = GameAdapters.ToVec3(creature.transform.forward);
                var neck = GameAdapters.NeckOf(creature);
                var hips = GameAdapters.HipsOf(creature);
                Place(markers[0], neck != null, neck != null ? LowBlowZones.ThroatCenter(GameAdapters.ToVec3(neck.transform.position), forward, settings) : Vec3.Zero, settings.ThroatZoneSize);
                Place(markers[1], hips != null, hips != null ? LowBlowZones.GroinCenter(GameAdapters.ToVec3(hips.transform.position), forward, settings) : Vec3.Zero, settings.GroinZoneSize);
            }
            stale.Clear();
            foreach (var pair in zones)
            {
                var creature = pair.Key;
                if (creature == null || creature.isKilled || !Creature.allActive.Contains(creature))
                {
                    stale.Add(creature);
                }
            }
            foreach (var creature in stale)
            {
                DestroyAll(zones[creature]);
                zones.Remove(creature);
            }
        }

        private static void Place(GameObject marker, bool show, Vec3 center, float radius)
        {
            marker.SetActive(show);
            if (show)
            {
                marker.transform.position = GameAdapters.ToVector3(center);
                marker.transform.localScale = Vector3.one * (radius * 2f);
            }
        }

        private void ClearZones()
        {
            foreach (var markers in zones.Values)
            {
                DestroyAll(markers);
            }
            zones.Clear();
        }

        private void ExpireHitMarkers()
        {
            var now = Time.time;
            for (var i = hitMarkers.Count - 1; i >= 0; i--)
            {
                if (hitMarkers[i].Value <= now)
                {
                    Destroy(hitMarkers[i].Key);
                    hitMarkers.RemoveAt(i);
                }
            }
        }

        private GameObject CreateSphere(string name, Color color)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            // A hit marker is made inside the game's contact callback, where Unity refuses DestroyImmediate and the
            // collider stayed: weapons bumped into the marker (headset log, 2026-11-23). Disabled at once, so it never
            // takes part in a physics step, and destroyed at the end of the frame.
            var collider = sphere.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            var material = MaterialFor(color);
            if (material != null)
            {
                sphere.GetComponent<Renderer>().sharedMaterial = material;
            }
            return sphere;
        }

        private Material MaterialFor(Color color)
        {
            if (materials.TryGetValue(color, out var material))
            {
                return material;
            }
            var found = ResolveShader();
            if (found == null)
            {
                return null;
            }
            material = new Material(found);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            materials[color] = material;
            return material;
        }

        private Shader ResolveShader()
        {
            if (shaderResolved)
            {
                return shader;
            }
            shaderResolved = true;
            foreach (var candidate in ShaderCandidates)
            {
                shader = Shader.Find(candidate);
                if (shader != null)
                {
                    ModLog.Info("Zone markers use shader: " + candidate);
                    return shader;
                }
            }
            ModLog.Warn("No marker shader found (" + string.Join(", ", ShaderCandidates) + "); markers keep Unity's default material.");
            return null;
        }

        private static void DestroyAll(GameObject[] objects)
        {
            foreach (var obj in objects)
            {
                Destroy(obj);
            }
        }

        private static void Destroy(GameObject obj)
        {
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }
    }
}
