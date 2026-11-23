using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Mindscape
{
    /// <summary>
    /// 把 second-brain 导出的 JSON 变成房间里的空间信息：正前总览、左弧待办、右弧记忆、脚前情绪雷达。
    /// 视觉遵循赛博终端 HUD：角括号、单色青、等宽标签、微型读数条。
    /// 场景启动后自动运行，无需手动挂载。
    /// </summary>
    public class MindscapeSpace : MonoBehaviour
    {
        private const float k_Radius = 1.7f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("Mindscape Space");
            DontDestroyOnLoad(go);
            go.AddComponent<MindscapeSpace>();
        }

        private void Start()
        {
            StartCoroutine(BuildSpace());
        }

        private IEnumerator BuildSpace()
        {
            yield return new WaitUntil(() => Camera.main != null);
            yield return new WaitForSeconds(0.4f);

            var cam = Camera.main.transform;
            var head = cam.position;
            var forward = cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();
            var baseYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

            TaskList tasks = null;
            MemoryList memories = null;
            MoodList mood = null;
            AreaList areas = null;

            yield return LoadJson<TaskList>("tasks.json", value => tasks = value);
            yield return LoadJson<MemoryList>("memories.json", value => memories = value);
            yield return LoadJson<MoodList>("mood.json", value => mood = value);
            yield return LoadJson<AreaList>("areas.json", value => areas = value);

            if (tasks == null && memories == null)
            {
                SpawnHudCard(head, baseYaw, 0f, 1.9f, 0.25f, CyberHud.Palette.Amber,
                    "SYSTEM", "NO DATA", "未找到数据", "运行 scripts/export-ar.mjs 后重启", 0f, 0);
                yield break;
            }

            BuildSummary(head, baseYaw, tasks, memories, mood, areas);
            DailyHud.Build(cam, tasks);
            BuildTaskCards(head, baseYaw, tasks);
            BuildMemoryCards(head, baseYaw, memories);
            MoodRadar.Build(head, baseYaw, mood != null ? mood.mood : null);
        }

        // ---------- 分区 ----------

        private void BuildSummary(Vector3 head, float baseYaw, TaskList tasks, MemoryList memories, MoodList mood, AreaList areas)
        {
            var open = 0;
            var done = 0;
            var total = 0;
            if (tasks != null)
            {
                foreach (var task in tasks.tasks)
                {
                    total += 1;
                    if (task.bucket == "done") done += 1;
                    else open += 1;
                }
            }

            var memoryCount = memories != null ? memories.memories.Length : 0;
            var areaCount = areas != null ? areas.areas.Length : 0;
            var moodLabel = mood != null && mood.mood.Length > 0 ? mood.mood[mood.mood.Length - 1].label : "暂无";

            var body = $"{open} 项待办 · {memoryCount} 条记忆 · {areaCount} 个领域 · 情绪 {moodLabel}";
            var ratio = total > 0 ? (float)done / total : 0f;

            SpawnHudCard(head, baseYaw, 0f, 2.15f, 0.42f, CyberHud.Palette.Cyan,
                "SYSTEM", DateTime.Now.ToString("yyyy-MM-dd"), "MINDSCAPE AR", body, ratio, 0, 700f);
        }

        private void BuildTaskCards(Vector3 head, float baseYaw, TaskList tasks)
        {
            if (tasks == null) return;

            var opened = 0;
            foreach (var task in tasks.tasks)
            {
                if (task.bucket != "done") opened += 1;
            }

            var picked = new TaskCard[opened];
            var index = 0;
            foreach (var task in tasks.tasks)
            {
                if (task.bucket != "done") picked[index++] = task;
            }

            var limit = Mathf.Min(4, picked.Length);
            for (var i = 0; i < limit; i += 1)
            {
                var task = picked[i];
                var angle = -58f + i * 13f;
                var accent = task.bucket == "overdue" ? CyberHud.Palette.Amber : CyberHud.Palette.Cyan;
                var body = string.IsNullOrEmpty(task.summary) ? task.area : task.summary;

                SpawnHudCard(head, baseYaw, angle, k_Radius, 0.12f, accent,
                    "TASK", $"{CyberHud.AsciiBucket(task.bucket)} · {task.due}",
                    task.title, Trim(body, 46), TaskRatio(task.bucket), i + 1);
            }
        }

        private void BuildMemoryCards(Vector3 head, float baseYaw, MemoryList memories)
        {
            if (memories == null) return;

            var limit = Mathf.Min(4, memories.memories.Length);
            for (var i = 0; i < limit; i += 1)
            {
                var memory = memories.memories[i];
                var angle = 58f - i * 13f;

                SpawnHudCard(head, baseYaw, angle, k_Radius, 0.12f, CyberHud.Palette.CyanDim,
                    CyberHud.AsciiKind(memory.kind), memory.date,
                    memory.title, Trim(memory.summary, 46), MemoryRatio(memory.kind), i + 1);
            }
        }

        // ---------- 卡片 ----------

        private void SpawnHudCard(Vector3 head, float baseYaw, float angle, float radius, float heightOffset, Color accent,
            string kind, string meta, string title, string body, float ratio, int index, float width = 620f, float height = 300f)
        {
            var canvasGo = CyberHud.Canvas("Card", width, height);
            canvasGo.transform.position = Polar(head, baseYaw, angle, radius) + Vector3.up * heightOffset;
            canvasGo.transform.rotation = Quaternion.LookRotation(canvasGo.transform.position - head, Vector3.up);

            var background = canvasGo.AddComponent<Image>();
            background.color = CyberHud.Palette.Panel;
            background.raycastTarget = false;

            var root = canvasGo.transform;
            var textWidth = width - 54f;
            var halfWidth = width * 0.5f;

            CyberHud.AddCornerBrackets(root, accent);
            CyberHud.AddHairLine(root, height * 0.5f - 44f, -halfWidth + 26f, halfWidth - 26f, CyberHud.Palette.CyanDim);
            CyberHud.AddQuad(root, new Vector2(8f, -14f), new Vector2(2f, 40f), accent, new Vector2(0f, 1f));

            CyberHud.AddText(root, $"{kind} · {meta} · #{index:00}",
                new Vector2(26f, -12f), new Vector2(textWidth, 24f), TextAnchor.UpperLeft,
                CyberHud.Mono, 16, CyberHud.Palette.CyanDim, new Vector2(0f, 1f));

            CyberHud.AddText(root, title,
                new Vector2(26f, -44f), new Vector2(textWidth, 38f), TextAnchor.UpperLeft,
                CyberHud.Cjk, 26, CyberHud.Palette.TextPrimary, new Vector2(0f, 1f));

            CyberHud.AddText(root, body,
                new Vector2(26f, -92f), new Vector2(textWidth, 56f), TextAnchor.UpperLeft,
                CyberHud.Cjk, 18, CyberHud.Palette.TextSecondary, new Vector2(0f, 1f));

            CyberHud.AddMiniBar(root, -height * 0.5f + 28f, -halfWidth + 26f, halfWidth - 26f, ratio, accent);
        }

        private static string Trim(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var flat = text.Replace("\r", " ").Replace("\n", " ").Trim();
            return flat.Length > max ? flat.Substring(0, max) + "…" : flat;
        }

        private static float TaskRatio(string bucket)
        {
            switch (bucket)
            {
                case "overdue": return 1f;
                case "today": return 0.8f;
                case "soon": return 0.55f;
                default: return 0.3f;
            }
        }

        private static float MemoryRatio(string kind)
        {
            switch (kind)
            {
                case "人": return 0.30f;
                case "事件": return 0.55f;
                case "感悟": return 0.85f;
                default: return 0.35f;
            }
        }

        private static Vector3 Polar(Vector3 head, float baseYaw, float angle, float radius)
        {
            var rotation = Quaternion.Euler(0f, baseYaw + angle, 0f);
            return head + rotation * Vector3.forward * radius;
        }

        private IEnumerator LoadJson<T>(string fileName, Action<T> onLoaded) where T : class
        {
            var path = Path.Combine(Application.streamingAssetsPath, "ar-data/" + fileName).Replace('\\', '/');
            if (!path.Contains("://"))
            {
                path = "file://" + path;
            }

            using var request = UnityWebRequest.Get(path);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Mindscape] 读取 {fileName} 失败：{request.error}");
                onLoaded(null);
                yield break;
            }

            try
            {
                onLoaded(JsonUtility.FromJson<T>(request.downloadHandler.text));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Mindscape] 解析 {fileName} 失败：{e.Message}");
                onLoaded(null);
            }
        }

        // ---------- 数据结构 ----------

        [Serializable]
        public class TaskList
        {
            public string generatedAt;
            public int total;
            public TaskCard[] tasks;
        }

        [Serializable]
        public class TaskCard
        {
            public string id;
            public string title;
            public string kind;
            public string status;
            public string priority;
            public string due;
            public string area;
            public string summary;
            public string bucket;
            public string path;
            public string[] tags;
            public string[] links;
        }

        [Serializable]
        public class MemoryList
        {
            public string generatedAt;
            public int total;
            public MemoryCard[] memories;
        }

        [Serializable]
        public class MemoryCard
        {
            public string id;
            public string title;
            public string kind;
            public string date;
            public string summary;
            public string path;
            public string[] tags;
            public string[] links;
        }

        [Serializable]
        public class MoodList
        {
            public string generatedAt;
            public int total;
            public MoodPoint[] mood;
        }

        [Serializable]
        public class MoodPoint
        {
            public string date;
            public string label;
            public string title;
            public string source;
            public float score;
            public float intensity;
            public string[] keywords;
        }

        [Serializable]
        public class AreaList
        {
            public string generatedAt;
            public Area[] areas;
        }

        [Serializable]
        public class Area
        {
            public string name;
            public int count;
            public AreaCard[] cards;
        }

        [Serializable]
        public class AreaCard
        {
            public string title;
            public string summary;
            public string path;
        }
    }
}
