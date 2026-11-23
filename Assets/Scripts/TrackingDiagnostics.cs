using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Viture.XR;

namespace Mindscape
{
    /// <summary>
    /// 跟踪诊断浮层：实时显示设备型号、6DoF 能力、追踪状态以及相机位置变化，
    /// 用于区分「设备只给 3DoF」和「位姿流断了」两种故障。
    /// </summary>
    public class TrackingDiagnostics : MonoBehaviour
    {
        private Transform _head;
        private Text _text;
        private Vector3 _lastPosition;
        private Vector3 _oneSecondAgo;
        private float _refreshTimer;
        private float _sampleTimer;
        private float _speed;
        private float _totalDistance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("Tracking Diagnostics");
            DontDestroyOnLoad(go);
            go.AddComponent<TrackingDiagnostics>();
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => Camera.main != null);
            _head = Camera.main.transform;
            _lastPosition = _head.position;
            _oneSecondAgo = _head.position;

            BuildOverlay();
            try
            {
                VitureXR.HeadTracking.trackingStatusChanged += status =>
                    Debug.Log($"[MINDSCAPE-TRACK] 追踪状态变化 -> {status}");
                Debug.Log($"[MINDSCAPE-TRACK] 启动 型号={VitureXR.Glasses.GetGlassesModel()} 能力={VitureXR.HeadTracking.GetHeadTrackingCapability()} 状态={VitureXR.HeadTracking.trackingStatus}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MINDSCAPE-TRACK] 读取 Viture 状态失败：" + e.Message);
            }
        }

        private void BuildOverlay()
        {
            var go = new GameObject("Tracking Overlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(_head, false);
            go.transform.localPosition = new Vector3(0f, -0.26f, 0.75f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * 0.00055f;

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(1100f, 190f);

            var background = go.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.Load<Font>("Fonts/SimHei");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            text.fontSize = 26;
            text.color = new Color(0.85f, 0.92f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var rect = textGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1060f, 160f);
            rect.anchoredPosition = Vector2.zero;
            _text = text;
        }

        private void Update()
        {
            if (_head == null || _text == null) return;

            var position = _head.position;
            _totalDistance += Vector3.Distance(position, _lastPosition);
            _lastPosition = position;

            _sampleTimer += Time.unscaledDeltaTime;
            if (_sampleTimer >= 1f)
            {
                _speed = Vector3.Distance(position, _oneSecondAgo);
                _oneSecondAgo = position;
                _sampleTimer = 0f;
            }

            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer < 0.25f) return;
            _refreshTimer = 0f;

            string model = "未知";
            string capability = "未知";
            string status = "未知";
            try
            {
                model = VitureXR.Glasses.GetGlassesModel().ToString();
                capability = VitureXR.HeadTracking.GetHeadTrackingCapability().ToString();
                status = VitureXR.HeadTracking.trackingStatus.ToString();
            }
            catch (Exception)
            {
                model = "读数失败";
            }

            var verdict = _speed > 0.02f
                ? "<color=#7CF2A6>正在接收 6DoF 位置</color>"
                : "<color=#FF8A80>位姿静止（3DoF 或追踪中断）</color>";

            _text.text =
                $"<size=24>设备 {model} · 能力 {capability} · 状态 {status}</size>\n" +
                $"<size=22>位置 {position:F2} · 1s 位移 {_speed:F3}m · 累计 {_totalDistance:F2}m</size>\n" +
                $"<size=22>{verdict}</size>";
        }
    }
}
