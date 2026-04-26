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

        [MenuItem("Proto/BlueArch/Build TinyHero Enemy Variants")]
        public static void BuildTinyHeroEnemyVariants()
        {
            const string sourceFolder = "Assets/UsableRes/RPGTinyHeroWorldBundlePBR/RPGTinyHeroWavePBR/Prefab/ModularCharacters";
            const string targetFolder = "Assets/Proto/Sample/BlueArch/Prefabs/Enemies";

            if (!System.IO.Directory.Exists(targetFolder))
            {
                System.IO.Directory.CreateDirectory(targetFolder);
                AssetDatabase.Refresh();
            }

            string[] sources = new string[]
            {
                $"{sourceFolder}/MC01.prefab",
                $"{sourceFolder}/MC02.prefab",
                $"{sourceFolder}/MC03.prefab",
                $"{sourceFolder}/MC04.prefab",
                $"{sourceFolder}/MC05.prefab",
            };

            int createdCount = 0;
            foreach (string srcPath in sources)
            {
                string baseName = System.IO.Path.GetFileNameWithoutExtension(srcPath);
                string dstPath = $"{targetFolder}/BlueEnemy_{baseName}.prefab";

                var src = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
                if (src == null) { Debug.LogWarning($"Source missing: {srcPath}"); continue; }

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
                inst.name = $"BlueEnemy_{baseName}";

                // MovementAgent requires CapsuleCollider
                var capsule = inst.GetComponent<CapsuleCollider>();
                if (capsule == null) capsule = inst.AddComponent<CapsuleCollider>();
                capsule.radius = 0.4f;
                capsule.height = 1.8f;
                capsule.center = new Vector3(0f, 0.9f, 0f);
                capsule.direction = 1;

                if (inst.GetComponent<Proto.Movement.MovementAgent>() == null)
                {
                    var agent = inst.AddComponent<Proto.Movement.MovementAgent>();
                    var profile = AssetDatabase.LoadAssetAtPath<Proto.Movement.MovementProfile>(
                        "Assets/Proto/Data/Config/Movement_Default.asset");
                    if (profile != null)
                    {
                        var so = new SerializedObject(agent);
                        so.FindProperty("_profile").objectReferenceValue = profile;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                if (inst.GetComponent<BlueEnemy>() == null) inst.AddComponent<BlueEnemy>();
                if (inst.GetComponent<BlueAnimEventSink>() == null) inst.AddComponent<BlueAnimEventSink>();

                if (inst.GetComponent<BlueAnimDriver>() == null)
                {
                    var driver = inst.AddComponent<BlueAnimDriver>();
                    var so = new SerializedObject(driver);
                    so.FindProperty("_idleState").stringValue = "Idle_Normal_NoWeapon";
                    so.FindProperty("_walkState").stringValue = "MoveFWD_Normal_InPlace_NoWeapon";
                    so.FindProperty("_attackState").stringValue = "Attack01_NoWeapon";
                    so.FindProperty("_dieState").stringValue = "Die01_NoWeapon";
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // Replace controller with NoWeaponStance which has the named states above.
                var anim = inst.GetComponent<Animator>();
                if (anim != null)
                {
                    var noWeaponCtrl = AssetDatabase.LoadAssetAtPath<UnityEngine.RuntimeAnimatorController>(
                        "Assets/UsableRes/RPGTinyHeroWorldBundlePBR/RPGTinyHeroWavePBR/Animator/NoWeaponStance.controller");
                    if (noWeaponCtrl != null) anim.runtimeAnimatorController = noWeaponCtrl;
                }

                PrefabUtility.SaveAsPrefabAsset(inst, dstPath);
                Object.DestroyImmediate(inst);
                createdCount++;
                Debug.Log($"Built variant: {dstPath}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Created {createdCount} BlueEnemy_MC* variants in {targetFolder}");
        }

        [MenuItem("Proto/BlueArch/Fix BlueEnemy Mesh Reference")]
        public static void FixBlueEnemyMesh()
        {
            const string enemyPrefabPath = "Assets/Proto/Sample/BlueArch/Prefabs/BlueEnemy.prefab";
            const string fbxPath = "Assets/UsableRes/CombatGirlsCharacterPack/Humanoid_Bot/Models/Humanoid_F.fbx";

            Mesh targetMesh = null;
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (a is Mesh m && m.name == "Humanoid_Female")
                {
                    targetMesh = m;
                    break;
                }
            }
            if (targetMesh == null)
            {
                Debug.LogError($"Humanoid_Female mesh not found in {fbxPath}");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(enemyPrefabPath);
            if (prefabRoot == null) { Debug.LogError($"Failed to load {enemyPrefabPath}"); return; }

            int fixedCount = 0;
            foreach (var smr in prefabRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null)
                {
                    smr.sharedMesh = targetMesh;
                    EditorUtility.SetDirty(smr);
                    fixedCount++;
                    Debug.Log($"Assigned mesh '{targetMesh.name}' to '{smr.name}'");
                }
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, enemyPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log($"Fixed {fixedCount} SkinnedMeshRenderer mesh refs in BlueEnemy prefab.");
        }

        [MenuItem("Proto/BlueArch/Diagnose Map Ground")]
        public static void DiagnoseMapGround()
        {
            string[] testPoints = new[] { "Player(0,0,0)", "Spawn ring NW(-10,0,10)", "Spawn ring SE(10,0,-10)", "Spawn ring N(0,0,15)", "Spawn ring S(0,0,-15)" };
            Vector3[] points = new[] { Vector3.zero, new Vector3(-10, 0, 10), new Vector3(10, 0, -10), new Vector3(0, 0, 15), new Vector3(0, 0, -15) };
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 origin = points[i] + Vector3.up * 100f;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f))
                {
                    Debug.Log($"[Map] {testPoints[i]}: ground at y={hit.point.y:F2}, hit '{hit.collider.name}' (layer={LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                }
                else
                {
                    Debug.LogWarning($"[Map] {testPoints[i]}: NO GROUND BELOW (raycast missed within 200u)");
                }
            }

            var player = GameObject.Find("Player");
            if (player != null)
            {
                Debug.Log($"[Player] position={player.transform.position}");
                var capsule = player.GetComponent<CapsuleCollider>();
                if (capsule != null) Debug.Log($"[Player] CapsuleCollider radius={capsule.radius}, height={capsule.height}, center={capsule.center}");
            }
        }

        [MenuItem("Proto/BlueArch/Diagnose Player Animator")]
        public static void DiagnosePlayerAnimator()
        {
            var player = GameObject.Find("Player");
            if (player == null) { Debug.LogError("Player not found"); return; }
            var anim = player.GetComponent<Animator>();
            if (anim == null) { Debug.LogError("Animator missing on Player"); return; }
            Debug.Log($"Controller: {(anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "<NULL>")}");
            Debug.Log($"Layer count: {anim.layerCount}");

            string[] candidates = new[]
            {
                "IDLE", "IDLE 0", "JOG", "WALK", "RUN", "SHOOT", "AUTO SHOOT",
                "DIE F", "----normal", "Idle_Normal_NoWeapon"
            };
            foreach (string n in candidates)
            {
                int hash = Animator.StringToHash(n);
                Debug.Log($"HasState[layer 0] '{n}' -> {anim.HasState(0, hash)}");
            }

            // Also list ALL clip names referenced by the controller via AnimatorController asset access
            var ac = anim.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (ac != null)
            {
                foreach (var layer in ac.layers)
                {
                    Debug.Log($"-- Layer '{layer.name}' --");
                    DumpStateMachine(layer.stateMachine, "");
                }
            }
            else
            {
                Debug.Log("Controller is not an editable AnimatorController (might be Override).");
            }
        }

        private static void DumpStateMachine(UnityEditor.Animations.AnimatorStateMachine sm, string indent)
        {
            if (sm == null) return;
            foreach (var s in sm.states)
            {
                string clipName = s.state.motion != null ? s.state.motion.name : "<no motion>";
                Debug.Log($"{indent}State '{s.state.name}' (clip: {clipName})");
            }
            foreach (var sub in sm.stateMachines)
            {
                Debug.Log($"{indent}-> Sub '{sub.stateMachine.name}'");
                DumpStateMachine(sub.stateMachine, indent + "  ");
            }
        }

        [MenuItem("Proto/BlueArch/Diagnose BlueEnemy Mesh")]
        public static void DiagnoseBlueEnemyMesh()
        {
            const string enemyPrefabPath = "Assets/Proto/Sample/BlueArch/Prefabs/BlueEnemy.prefab";
            const string fbxPath = "Assets/UsableRes/CombatGirlsCharacterPack/Humanoid_Bot/Models/Humanoid_F.fbx";

            // 1) Inspect BlueEnemy prefab's SkinnedMeshRenderer
            var prefabRoot = PrefabUtility.LoadPrefabContents(enemyPrefabPath);
            if (prefabRoot != null)
            {
                var smrs = prefabRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs)
                {
                    Debug.Log($"[BlueEnemy.prefab] '{smr.name}' sharedMesh = {(smr.sharedMesh != null ? smr.sharedMesh.name : "<NULL>")}, rootBone = {(smr.rootBone != null ? smr.rootBone.name : "<NULL>")}, bones = {(smr.bones != null ? smr.bones.Length : 0)}");
                }
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            // 2) List all sub-assets in the FBX
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            Debug.Log($"[Humanoid_F.fbx] {allAssets.Length} sub-assets:");
            foreach (var a in allAssets)
            {
                if (a == null) continue;
                Debug.Log($"  - {a.GetType().Name}: '{a.name}'");
            }
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

        [MenuItem("Proto/BlueArch/Setup Camera Aim Offset")]
        public static void SetupCameraAimOffset()
        {
            var player = GameObject.Find("Player");
            if (player == null) { Debug.LogError("Player not found in active scene"); return; }

            var rig = Object.FindFirstObjectByType<Proto.Camera.CameraRig>();
            if (rig == null) { Debug.LogError("CameraRig not found in active scene"); return; }

            var aimOffset = player.GetComponent<BlueCameraAimOffset>();
            if (aimOffset == null) aimOffset = player.AddComponent<BlueCameraAimOffset>();

            // BluePlayerController 의 _aimCamera 와 동일한 카메라를 사용 (마우스 ray 일관성)
            UnityEngine.Camera aimCam = null;
            var pc = player.GetComponent<BluePlayerController>();
            if (pc != null)
            {
                var so = new SerializedObject(pc);
                var camProp = so.FindProperty("_aimCamera");
                if (camProp != null) aimCam = camProp.objectReferenceValue as UnityEngine.Camera;
            }
            if (aimCam == null) aimCam = UnityEngine.Camera.main;

            var aoSo = new SerializedObject(aimOffset);
            aoSo.FindProperty("_rig").objectReferenceValue = rig;
            aoSo.FindProperty("_aimCamera").objectReferenceValue = aimCam;
            aoSo.FindProperty("_player").objectReferenceValue = player.transform;
            aoSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(aimOffset);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log($"[CameraAimOffset] attached to Player. rig='{rig.name}', aimCam='{(aimCam != null ? aimCam.name : "<null>")}'");
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
