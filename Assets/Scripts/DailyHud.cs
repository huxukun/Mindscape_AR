using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mindscape
{
    /// <summary>
    /// 日常悬浮面板：完成率、逾期/今日/临近计数、最近两条待办。
    /// 贴在视野右下角，随头部轻微跟随，主动让出正前方视野。
    /// </summary>
    public static class DailyHud
    {
        public static void Build(Transform cameraTransform, MindscapeSpace.TaskList tasks)
        {
            if (cameraTransform == null || tasks == null || tasks.tasks == null || tasks.tasks.Length == 0)
            {
                return;
            }

            var total = tasks.tasks.Length;
            var done = 0;
            var overdue = 0;
            var today = 0;
            var soon = 0;
            var open = new List<MindscapeSpace.TaskCard>();

            foreach (var task in tasks.tasks)
            {
                switch (task.bucket)
                {
                    case "done":
                        done += 1;
                        break;
                    case "overdue":
                        overdue += 1;
                        open.Add(task);
                        break;
                    case "today":
                        today += 1;
                        open.Add(task);
                        break;
                    case "soon":
                        soon += 1;
                        open.Add(task);
                        break;
                    default:
                        open.Add(task);
                        break;
                }
            }

            var percent = total > 0 ? Mathf.RoundToInt(done * 100f / total) : 0;

            var canvasGo = CyberHud.Canvas("Daily HUD", 460f, 260f, 12);
            canvasGo.transform.SetParent(cameraTransform, false);
            canvasGo.transform.localPosition = new Vector3(0.30f, -0.18f, 0.75f);
            canvasGo.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.0007f;

            var background = canvasGo.AddComponent<Image>();
            background.color = CyberHud.Palette.Panel;
            background.raycastTarget = false;

            var root = canvasGo.transform;
            const float halfWidth = 230f;
            const float inner = 20f;

            CyberHud.AddCornerBrackets(root, CyberHud.Palette.Cyan);
            CyberHud.AddHairLine(root, 130f - 92f, -halfWidth + inner, halfWidth - inner, CyberHud.Palette.CyanDim);

            CyberHud.AddText(root, $"DAILY · {System.DateTime.Now:yyyy-MM-dd}",
                new Vector2(inner, -12f), new Vector2(420f, 20f), TextAnchor.UpperLeft,
                CyberHud.Mono, 14, CyberHud.Palette.CyanDim, new Vector2(0f, 1f));

            CyberHud.AddText(root, $"{percent}%",
                new Vector2(inner, -32f), new Vector2(200f, 48f), TextAnchor.UpperLeft,
                CyberHud.Mono, 34, CyberHud.Palette.Cyan, new Vector2(0f, 1f));

            CyberHud.AddText(root, $"{done}/{total} DONE",
                new Vector2(-inner, -50f), new Vector2(160f, 22f), TextAnchor.UpperRight,
                CyberHud.Mono, 15, CyberHud.Palette.CyanDim, new Vector2(1f, 1f));

            CyberHud.AddMiniBar(root, 130f - 100f, -halfWidth + inner, halfWidth - inner, total > 0 ? (float)done / total : 0f,
                CyberHud.Palette.Cyan);

            CyberHud.AddText(root, $"逾期 {overdue} · 今日 {today} · 临近 {soon}",
                new Vector2(inner, -108f), new Vector2(420f, 22f), TextAnchor.UpperLeft,
                CyberHud.Cjk, 15, CyberHud.Palette.TextSecondary, new Vector2(0f, 1f));

            for (var i = 0; i < 2 && i < open.Count; i += 1)
            {
                var task = open[i];
                var color = task.bucket == "overdue"
                    ? CyberHud.Palette.Amber
                    : task.bucket == "today"
                        ? CyberHud.Palette.Cyan
                        : CyberHud.Palette.TextSecondary;
                var mark = task.bucket == "overdue" ? "!" : "›";
                var line = $"{mark} {Trim(task.title, 18)}";

                CyberHud.AddText(root, line,
                    new Vector2(inner, -134f - i * 26f), new Vector2(420f, 22f), TextAnchor.UpperLeft,
                    CyberHud.Cjk, 15, color, new Vector2(0f, 1f));
            }
        }

        private static string Trim(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length > max ? text.Substring(0, max) + "…" : text;
        }
    }
}
