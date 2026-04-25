#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Proto.Sample.BlueArch.EditorTools
{
    public static class BlueArchSceneLayout
    {
        [MenuItem("Proto/BlueArch/Import RifleGirl URP Converter")]
        public static void ImportRifleUrpConverter()
        {
            string path = "Assets/UsableRes/CombatGirlsCharacterPack/Rifle_URP_Converter.unitypackage";
            if (!System.IO.File.Exists(path))
            {
                Debug.LogError($"Package not found at {path}");
                return;
            }
            AssetDatabase.ImportPackage(path, false);
            Debug.Log($"Imported package: {path}");
        }

        [MenuItem("Proto/BlueArch/Force Mesh2D-Lit-Default on Characters")]
        public static void ForceMesh2DLitOnCharacters()
        {
            const string matGuid = "9452ae1262a74094f8a68013fbcd1834";
            string matPath = AssetDatabase.GUIDToAssetPath(matGuid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Debug.LogError($"Mesh2D-Lit-Default material not found at GUID {matGuid} (path '{matPath}')");
                return;
            }

            int total = 0;

            // 1) Player in scene
            var player = GameObject.Find("Player");
            if (player != null)
            {
                total += ReplaceAllRendererMaterials(player, mat);
                EditorUtility.SetDirty(player);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
            }
            else Debug.LogWarning("Player not found in active scene.");

            // 2) BlueEnemy prefab — open stage, replace, save
            const string enemyPrefabPath = "Assets/Proto/Sample/BlueArch/Prefabs/BlueEnemy.prefab";
            var prefabRoot = PrefabUtility.LoadPrefabContents(enemyPrefabPath);
            if (prefabRoot != null)
            {
                int n = ReplaceAllRendererMaterials(prefabRoot, mat);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, enemyPrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                total += n;
                Debug.Log($"BlueEnemy prefab: replaced {n} material slots");
            }
            else Debug.LogWarning($"BlueEnemy prefab not found at {enemyPrefabPath}");

            AssetDatabase.SaveAssets();
            Debug.Log($"Total material slots replaced: {total}");
        }

        private static int ReplaceAllRendererMaterials(GameObject root, Material mat)
        {
            int count = 0;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var sharedMats = r.sharedMaterials;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    sharedMats[i] = mat;
                    count++;
                }
                r.sharedMaterials = sharedMats;
                EditorUtility.SetDirty(r);
            }
            return count;
        }

        [MenuItem("Proto/BlueArch/Strip Missing Scripts")]
        public static void StripMissingScripts()
        {
            int totalRemoved = 0;
            int totalGosVisited = 0;

            // 1) 씬 전체 walk
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                totalRemoved += StripRecursive(root, ref totalGosVisited);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            // 2) 처리 대상 프리팹들
            string[] prefabsToClean = new string[]
            {
                "Assets/Proto/Sample/BlueArch/Prefabs/BlueEnemy.prefab",
                "Assets/Proto/Sample/BlueArch/Prefabs/BlueProjectile.prefab",
                "Assets/UsableRes/CombatGirlsCharacterPack/RifleGirl/Prefab/Rifle_Full_Body.prefab",
                "Assets/UsableRes/CombatGirlsCharacterPack/Humanoid_Bot/Prefab/Humanoid_F.prefab",
                "Assets/UsableRes/CombatGirlsCharacterPack/Humanoid_Bot/Prefab/Humanoid_F_Rifle.prefab",
            };
            foreach (string p in prefabsToClean)
            {
                if (!System.IO.File.Exists(p)) continue;
                var prefabRoot = PrefabUtility.LoadPrefabContents(p);
                if (prefabRoot == null) continue;

                int n = StripRecursive(prefabRoot, ref totalGosVisited);
                if (n > 0) PrefabUtility.SaveAsPrefabAsset(prefabRoot, p);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                Debug.Log($"{System.IO.Path.GetFileName(p)}: removed {n} missing scripts");
                totalRemoved += n;
            }

            Debug.Log($"Total missing scripts removed: {totalRemoved} across {totalGosVisited} GameObjects.");
        }

        private static int StripRecursive(GameObject root, ref int visited)
        {
            int removed = 0;
            var stack = new System.Collections.Generic.Stack<Transform>();
            stack.Push(root.transform);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                visited++;
                int n = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (n > 0)
                {
                    removed += n;
                    EditorUtility.SetDirty(t.gameObject);
                }
                for (int i = 0; i < t.childCount; i++) stack.Push(t.GetChild(i));
            }
            return removed;
        }

        [MenuItem("Proto/BlueArch/Strip Magica Cloth Children")]
        public static void StripMagicaChildren()
        {
            var player = GameObject.Find("Player");
            if (player == null) { Debug.LogError("Player not found"); return; }

            var toDelete = new System.Collections.Generic.List<GameObject>();
            foreach (var t in player.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t == player.transform) continue;
                if (t.name.StartsWith("Magica ", System.StringComparison.Ordinal))
                {
                    toDelete.Add(t.gameObject);
                }
            }

            int deleted = 0;
            foreach (var go in toDelete)
            {
                if (go == null) continue;
                Object.DestroyImmediate(go);
                deleted++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log($"Stripped {deleted} Magica child GameObjects from Player.");
        }

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
