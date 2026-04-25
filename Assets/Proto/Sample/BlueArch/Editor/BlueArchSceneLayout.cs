#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Proto.Sample.BlueArch.EditorTools
{
    public static class BlueArchSceneLayout
    {
        [MenuItem("Proto/BlueArch/Layout HUD Widgets")]
        public static void LayoutHudWidgets()
        {
            var canvas = GameObject.Find("HudCanvas");
            if (canvas == null) { Debug.LogError("HudCanvas not found"); return; }

            Place(canvas, "HpText", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -40), new Vector2(500, 60));
            Place(canvas, "TimerText", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(400, 100));
            Place(canvas, "ScoreText", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), new Vector2(500, 60));
            Place(canvas, "ResultText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1200, 240));

            EditorUtility.SetDirty(canvas);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("HUD widgets laid out.");
        }

        private static void Place(GameObject canvas, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var t = canvas.transform.Find(name) as RectTransform;
            if (t == null) { Debug.LogWarning($"Child '{name}' missing"); return; }
            t.anchorMin = aMin;
            t.anchorMax = aMax;
            t.pivot = pivot;
            t.anchoredPosition = pos;
            t.sizeDelta = size;
        }
    }
}
#endif
