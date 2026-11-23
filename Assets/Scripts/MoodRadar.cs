using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindscape
{
    /// <summary>
    /// 地面 3D 线框情绪雷达：网格 + 发光折线 + 垂线 + 扫描带 + 最新点光束。
    /// 位置以启动时的头部位置为基准，落在脚前地面上。
    /// </summary>
    public static class MoodRadar
    {
        private const float k_Drop = 1.30f;
        private const float k_HalfWidth = 0.70f;
        private const float k_HalfDepth = 0.50f;
        private const float k_LineZ = -0.10f;

        private static readonly Dictionary<Color, Material> s_Materials = new Dictionary<Color, Material>();

        public static void Build(Vector3 head, float baseYaw, MindscapeSpace.MoodPoint[] points)
        {
            if (points == null || points.Length < 2)
            {
                Debug.Log("[Mindscape] 情绪数据不足，跳过地面雷达");
                return;
            }

            var rotation = Quaternion.Euler(0f, baseYaw, 0f);
            var origin = head + rotation * Vector3.forward * 1.55f + Vector3.up * -k_Drop;

            var root = new GameObject("Mood Radar");
            root.transform.SetPositionAndRotation(origin, rotation);

            BuildGrid(root.transform);
            BuildPolyline(root.transform, points);
            BuildNodes(root.transform, points);
            var latest = BuildBeam(root.transform, points);
            var sweep = BuildSweep(root.transform);

            var animator = root.AddComponent<RadarAnimator>();
            animator.Configure(sweep, latest, GetMaterial(CyberHud.Palette.Cyan));

            BuildLabel(head, origin, rotation, points);
        }

        private static void BuildGrid(Transform root)
        {
            for (var i = 0; i <= 8; i += 1)
            {
                var x = -k_HalfWidth + i * (k_HalfWidth * 2f / 8f);
                var isCenter = i == 4;
                AddLine(root, $"Grid X {i}",
                    new[] { new Vector3(x, 0.001f, -k_HalfDepth), new Vector3(x, 0.001f, k_HalfDepth) },
                    isCenter ? 0.006f : 0.004f,
                    isCenter ? CyberHud.Palette.CyanDim : CyberHud.Palette.CyanFaint);
            }

            for (var j = 0; j <= 6; j += 1)
            {
                var z = -k_HalfDepth + j * (k_HalfDepth * 2f / 6f);
                var isCenter = j == 3;
                AddLine(root, $"Grid Z {j}",
                    new[] { new Vector3(-k_HalfWidth, 0.001f, z), new Vector3(k_HalfWidth, 0.001f, z) },
                    isCenter ? 0.006f : 0.004f,
                    isCenter ? CyberHud.Palette.CyanDim : CyberHud.Palette.CyanFaint);
            }
        }

        private static Vector3[] BuildPolyline(Transform root, MindscapeSpace.MoodPoint[] points)
        {
            var positions = new Vector3[points.Length];
            for (var i = 0; i < points.Length; i += 1)
            {
                var t = points.Length == 1 ? 0.5f : (float)i / (points.Length - 1);
                var x = Mathf.Lerp(-0.65f, 0.65f, t);
                var y = 0.03f + (Mathf.Clamp(points[i].score, -1f, 1f) + 1f) * 0.5f * 0.30f;
                positions[i] = new Vector3(x, y, k_LineZ);
            }

            AddLine(root, "Mood Line", positions, 0.008f, CyberHud.Palette.Cyan);
            return positions;
        }

        private static void BuildNodes(Transform root, MindscapeSpace.MoodPoint[] points)
        {
            for (var i = 0; i < points.Length; i += 1)
            {
                var t = points.Length == 1 ? 0.5f : (float)i / (points.Length - 1);
                var x = Mathf.Lerp(-0.65f, 0.65f, t);
                var y = 0.03f + (Mathf.Clamp(points[i].score, -1f, 1f) + 1f) * 0.5f * 0.30f;

                AddLine(root, $"Drop {i}",
                    new[] { new Vector3(x, 0.001f, k_LineZ), new Vector3(x, y, k_LineZ) },
                    0.002f, CyberHud.Palette.CyanFaint);

                var color = points[i].score > 0.15f
                    ? CyberHud.Palette.Cyan
                    : points[i].score < -0.15f
                        ? CyberHud.Palette.Magenta
                        : CyberHud.Palette.Neutral;

                AddLine(root, $"Node {i}",
                    new[] { new Vector3(x, y, k_LineZ), new Vector3(x, y + 0.02f, k_LineZ) },
                    0.012f, color);
            }
        }

        private static LineRenderer BuildBeam(Transform root, MindscapeSpace.MoodPoint[] points)
        {
            var lastIndex = points.Length - 1;
            var t = points.Length == 1 ? 0.5f : (float)lastIndex / (points.Length - 1);
            var x = Mathf.Lerp(-0.65f, 0.65f, t);
            var y = 0.03f + (Mathf.Clamp(points[lastIndex].score, -1f, 1f) + 1f) * 0.5f * 0.30f;

            return AddLine(root, "Latest Beam",
                new[] { new Vector3(x, y, k_LineZ), new Vector3(x, y + 0.30f, k_LineZ) },
                0.006f, CyberHud.Palette.Cyan, CreateMaterial(CyberHud.Palette.Cyan));
        }

        private static LineRenderer BuildSweep(Transform root)
        {
            var line = AddLine(root, "Sweep",
                new[] { new Vector3(-k_HalfWidth, 0.004f, -k_HalfDepth), new Vector3(k_HalfWidth, 0.004f, -k_HalfDepth) },
                0.006f, CyberHud.Palette.CyanDim);
            line.positionCount = 2;
            return line;
        }

        private static void BuildLabel(Vector3 head, Vector3 origin, Quaternion rotation, MindscapeSpace.MoodPoint[] points)
        {
            var label = CyberHud.Canvas("MOOD Label", 320f, 64f, 11);
            var worldPosition = origin + rotation * new Vector3(-0.55f, 0.06f, -0.62f);
            label.transform.position = worldPosition;
            label.transform.rotation = Quaternion.LookRotation(worldPosition - head, Vector3.up);

            var background = label.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0f, 0f, 0f, 0.28f);
            background.raycastTarget = false;

            CyberHud.AddCornerBrackets(label.transform, CyberHud.Palette.CyanDim, 12f, 1f, 4f);
            CyberHud.AddText(label.transform,
                $"MOOD · LAST {points.Length}D · {points[points.Length - 1].label}",
                Vector2.zero, new Vector2(288f, 44f), TextAnchor.MiddleLeft,
                CyberHud.Mono, 16, CyberHud.Palette.CyanDim);
        }

        private static LineRenderer AddLine(Transform parent, string name, Vector3[] positions, float width, Color color, Material material = null)
        {
            var go = new GameObject(name, typeof(LineRenderer));
            go.transform.SetParent(parent, false);

            var line = go.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = positions.Length;
            line.SetPositions(positions);
            line.startWidth = width;
            line.endWidth = width;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.loop = false;
            line.sharedMaterial = material != null ? material : GetMaterial(color);
            return line;
        }

        private static Material GetMaterial(Color color)
        {
            if (s_Materials.TryGetValue(color, out var cached) && cached != null)
            {
                return cached;
            }

            var material = CreateMaterial(color);
            s_Materials[color] = material;
            return material;
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            material.color = color;
            return material;
        }

        /// <summary>扫描带往返与最新点呼吸。</summary>
        public class RadarAnimator : MonoBehaviour
        {
            private LineRenderer _sweep;
            private LineRenderer _beam;
            private Material _beamMaterial;
            private float _time;

            public void Configure(LineRenderer sweep, LineRenderer beam, Material beamMaterial)
            {
                _sweep = sweep;
                _beam = beam;
                _beamMaterial = beamMaterial;
            }

            private void Update()
            {
                _time += Time.deltaTime;

                if (_sweep != null)
                {
                    var z = Mathf.Lerp(-k_HalfDepth, k_HalfDepth, Mathf.PingPong(_time / 3f, 1f));
                    _sweep.SetPosition(0, new Vector3(-k_HalfWidth, 0.004f, z));
                    _sweep.SetPosition(1, new Vector3(k_HalfWidth, 0.004f, z));
                }

                if (_beamMaterial != null)
                {
                    var color = CyberHud.Palette.Cyan;
                    color.a = 0.35f + 0.35f * Mathf.Sin(_time * Mathf.PI * 2.4f);
                    if (_beamMaterial.HasProperty("_BaseColor"))
                    {
                        _beamMaterial.SetColor("_BaseColor", color);
                    }
                    _beamMaterial.color = color;
                }
            }
        }
    }
}
