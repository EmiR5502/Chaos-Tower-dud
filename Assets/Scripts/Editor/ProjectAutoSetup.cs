#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChaosTower.Managers;
using ChaosTower.Gameplay;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.IO;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ChaosTower.Editor
{
    public class ProjectAutoSetup : EditorWindow
    {
        private const string ResourcesFolder = "Assets/AutoSetup_Resources";

        [MenuItem("Chaos Tower/Auto Setup Entire Project")]
        public static void SetupProject()
        {
            // Clear selection and focus to avoid Inspector "MissingReference" errors
            Selection.activeGameObject = null;
            
            // Delay execution by one frame to allow Unity to close menus and Tooltips
            // which often cause SerializedObject exceptions if destroyed immediately.
            EditorApplication.delayCall += ExecuteSetupInternal;
        }

        private static void ExecuteSetupInternal()
        {
            // Re-verify selection is clear
            Selection.activeGameObject = null;

            // 0. Ensure Resources Folder exists
            if (!Directory.Exists(ResourcesFolder))
            {
                Directory.CreateDirectory(ResourcesFolder);
                AssetDatabase.Refresh();
            }

            // Clean current scene objects to avoid duplicates using safe destruction
            DestroyManagerIfExists("Systems");
            DestroyManagerIfExists("Gameplay");
            DestroyManagerIfExists("Environment");
            DestroyManagerIfExists("UI");
            DestroyManagerIfExists("EventSystem");

            // 1. Setup Tags
            AddTag("Block");
            AddTag("Platform");

            // 2. Get or Create Persistent Groups (Prefabs)
            GameObject systems = GetOrCreatePersistentObject("Systems");
            GameObject gameplay = GetOrCreatePersistentObject("Gameplay");
            GameObject environment = GetOrCreatePersistentObject("Environment");
            GameObject ui = GetOrCreatePersistentObject("UI");

            // 3. Populate / Update Groups
            SetupSystems(systems);
            SetupGameplay(gameplay);
            SetupEnvironment(environment);
            SetupUI(ui);
            SetupMainCamera();

            // 4. Save and Finish
            SaveGroupAsPrefab(systems, "Systems");
            SaveGroupAsPrefab(gameplay, "Gameplay");
            SaveGroupAsPrefab(environment, "Environment");
            SaveGroupAsPrefab(ui, "UI");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            // Force an inspector repaint to clear any ghost references
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            Debug.Log("Chaos Tower Auto-Setup Complete! Changes in 'AutoSetup_Resources' are preserved.");
        }

        private static GameObject GetOrCreatePersistentObject(string name)
        {
            string path = $"{ResourcesFolder}/{name}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance;

            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = name;
            }
            else
            {
                instance = new GameObject(name);
                SaveGroupAsPrefab(instance, name);
            }
            return instance;
        }

        private static void SaveGroupAsPrefab(GameObject obj, string name)
        {
            string path = $"{ResourcesFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(obj, path);
        }

        private static void SetupSystems(GameObject systems)
        {
            if (GameObject.Find("GameManager") == null) AddChildWithComponent<GameManager>(systems, "GameManager");
            if (GameObject.Find("ScoreManager") == null) AddChildWithComponent<ScoreManager>(systems, "ScoreManager");
            if (GameObject.Find("InputManager") == null) AddChildWithComponent<InputManager>(systems, "InputManager");
            if (GameObject.Find("SaveManager") == null) AddChildWithComponent<SaveManager>(systems, "SaveManager");
            
            GameObject camMgrObj = GameObject.Find("CameraManager");
            CameraManager camMgr;
            if (camMgrObj == null)
            {
                camMgr = AddChildWithComponent<CameraManager>(systems, "CameraManager");
                camMgrObj = camMgr.gameObject;
                
                var vcamObj = new GameObject("Virtual Camera");
                vcamObj.transform.SetParent(systems.transform);
                
                // Set the exact starting Transform you showed me
                vcamObj.transform.position = new Vector3(0, 7, -13.5f);
                vcamObj.transform.rotation = Quaternion.Euler(15f, 0, 0);

                var vcam = vcamObj.AddComponent<CinemachineCamera>();
                vcam.LookAt = GameObject.Find("FollowTarget")?.transform;
                vcam.Follow = GameObject.Find("FollowTarget")?.transform;
                
                var follow = vcamObj.AddComponent<CinemachineFollow>();
                follow.FollowOffset = new Vector3(0, 7, -13.5f);
                
                // Add RotationComposer to handle tracking, but the start will be your angle
                vcamObj.AddComponent<CinemachineRotationComposer>();
                camMgr.virtualCamera = vcam;
            }
            else
            {
                camMgr = camMgrObj.GetComponent<CameraManager>();
                var vcam = camMgr.virtualCamera;
                if (vcam != null)
                {
                    vcam.LookAt = GameObject.Find("FollowTarget")?.transform;
                    vcam.Follow = GameObject.Find("FollowTarget")?.transform;
                    var follow = vcam.GetComponent<CinemachineFollow>();
                    if (follow != null) follow.FollowOffset = new Vector3(0, 7, -13.5f);
                }
            }

            var amObj = systems.transform.Find("AudioManager")?.GetComponent<AudioManager>();
            if (amObj == null) amObj = AddChildWithComponent<AudioManager>(systems, "AudioManager");

            // Always ensure sources exist and are configured
            if (amObj.SFXSource == null) amObj.SFXSource = amObj.gameObject.GetComponent<AudioSource>() ?? amObj.gameObject.AddComponent<AudioSource>();
            if (amObj.MusicSource == null) amObj.MusicSource = amObj.gameObject.GetComponent<AudioSource>() ?? amObj.gameObject.AddComponent<AudioSource>();
            if (amObj.MenuMusicSource == null) 
            {
                // We need a dedicated source for menu music to avoid overlap
                var sources = amObj.GetComponents<AudioSource>();
                if (sources.Length < 3) amObj.MenuMusicSource = amObj.gameObject.AddComponent<AudioSource>();
                else amObj.MenuMusicSource = sources[2];
            }

            // Force production settings
            ConfigureSource(amObj.SFXSource, 0.8f, false);
            ConfigureSource(amObj.MusicSource, 0.6f, false);
            ConfigureSource(amObj.MenuMusicSource, 0.5f, true);

            // Always re-find and re-link clips
            amObj.BrickClip = FindAudioClip("Brick");
            amObj.MenuMusicClip = FindAudioClip("bricks menu");
            amObj.DropClip = FindAudioClip("Brick"); // Re-use brick for drop if generic drop is missing
            
            if (amObj.BrickClip == null) Debug.LogWarning("AutoSetup: Could not find 'Brick.mp3'");
            if (amObj.MenuMusicClip == null) Debug.LogWarning("AutoSetup: Could not find 'bricks menu.mp3'");
        }

        private static void ConfigureSource(AudioSource source, float vol, bool loop)
        {
            if (source == null) return;
            source.playOnAwake = false;
            source.loop = loop;
            source.volume = vol;
            source.spatialBlend = 0f; // Force 2D for global managers
            source.mute = false;
            source.enabled = true;
        }

        private static void SetupGameplay(GameObject gameplay)
        {
            var spawnerObj = GameObject.Find("BlockSpawner")?.GetComponent<BlockSpawner>();
            if (spawnerObj == null)
            {
                spawnerObj = AddChildWithComponent<BlockSpawner>(gameplay, "BlockSpawner");
                spawnerObj.SwingSpeed = 1.2f;
                spawnerObj.SpawnHeightOffset = 2.55f;
                var line = spawnerObj.gameObject.AddComponent<LineRenderer>();
                line.startWidth = 0.05f; line.endWidth = 0.05f;
                spawnerObj.CraneLine = line;
            }

            // Always update material
            if (spawnerObj != null && spawnerObj.CraneLine != null)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Msterials/BaseMat.mat");
                if (mat == null) mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BaseMat.mat");
                
                if (mat != null)
                {
                    spawnerObj.CraneLine.sharedMaterial = mat;
                }
                else if (spawnerObj.CraneLine.sharedMaterial == null)
                {
                    spawnerObj.CraneLine.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                }
            }

            var followTarget = GameObject.Find("FollowTarget");
            if (followTarget == null)
            {
                followTarget = new GameObject("FollowTarget");
                followTarget.transform.SetParent(gameplay.transform);
            }

            var tmObj = GameObject.Find("TowerManager")?.GetComponent<TowerManager>();
            if (tmObj == null)
            {
                tmObj = AddChildWithComponent<TowerManager>(gameplay, "TowerManager");
                tmObj.TowerRoot = new GameObject("TowerRoot").transform;
                tmObj.TowerRoot.SetParent(gameplay.transform);
            }
            // CRITICAL: Link the follow target to the manager so it ascends!
            tmObj.FollowTarget = followTarget.transform;
        }

        private static void SetupEnvironment(GameObject environment)
        {
            if (GameObject.Find("BasePlatform") == null)
            {
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.name = "BasePlatform";
                platform.transform.position = new Vector3(0, -0.5f, 0); // Ground is at -1, platform is 1 high, so center at -0.5
                platform.transform.localScale = new Vector3(10, 1, 10);
                platform.tag = "Platform";
                platform.transform.SetParent(environment.transform);
                
                var surface = platform.GetComponent<NavMeshSurface>();
                if (surface == null) surface = platform.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All; 
                surface.BuildNavMesh();
            }
            else
            {
                // Force a rebuild even if it exists, to be safe
                var surface = GameObject.Find("BasePlatform").GetComponent<NavMeshSurface>();
                if (surface != null) surface.BuildNavMesh();
            }

            var aiObj = GameObject.Find("AI_Agent");
            if (aiObj == null)
            {
                aiObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                aiObj.name = "AI_Agent";
                aiObj.transform.position = new Vector3(3, 0.5f, 3);
                aiObj.transform.SetParent(environment.transform);
                aiObj.AddComponent<NavMeshAgent>();
            }
            
            var ai = aiObj.GetComponent<AIController>();
            if (ai == null) ai = aiObj.AddComponent<AIController>();
            
            ai.EngineClip = FindAudioClip("bulldozer");
        }

        private static AudioClip FindAudioClip(string name)
        {
#if UNITY_EDITOR
            // Exact search first
            string[] guids = AssetDatabase.FindAssets(name + " t:AudioClip");
            if (guids.Length == 0) 
            {
                // Simple fuzzy search
                guids = AssetDatabase.FindAssets(name);
            }
            
            if (guids.Length > 0)
            {
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null)
                    {
                        Debug.Log($"AutoSetup: Found AudioClip '{name}' at {path}");
                        return clip;
                    }
                }
            }
            Debug.LogWarning($"AutoSetup: FAILED to find AudioClip '{name}'. Check your Assets/Sound folder.");
#endif
            return null;
        }

        private static void SetupUI(GameObject parent)
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                canvasObj.transform.SetParent(parent.transform);
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (GameObject.Find("EventSystem") == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.transform.SetParent(parent.transform);
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            var uiMgr = parent.GetComponentInChildren<UIManager>();
            if (uiMgr == null) uiMgr = AddChildWithComponent<UIManager>(parent, "UIManager");

            if (uiMgr.MenuPanel == null) uiMgr.MenuPanel = FindDeep(canvasObj.transform, "MenuPanel")?.gameObject ?? CreatePanel(canvasObj.transform, "MenuPanel");
            if (uiMgr.GameOverPanel == null) uiMgr.GameOverPanel = FindDeep(canvasObj.transform, "GameOverPanel")?.gameObject ?? CreatePanel(canvasObj.transform, "GameOverPanel");
            
            if (uiMgr.LoadingPanel == null)
            {
                uiMgr.LoadingPanel = FindDeep(canvasObj.transform, "LoadingPanel")?.gameObject ?? CreatePanel(canvasObj.transform, "LoadingPanel");
                var loadingImg = uiMgr.LoadingPanel.GetComponent<Image>();
                if (loadingImg != null) loadingImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                
                if (uiMgr.LoadingPanel.transform.childCount == 0)
                    CreateText(uiMgr.LoadingPanel.transform, "LoadingLabel", "LOADING...", 40);
            }

            if (uiMgr.SettingsPanel == null)
            {
                uiMgr.SettingsPanel = FindDeep(canvasObj.transform, "SettingsPanel")?.gameObject ?? CreatePanel(canvasObj.transform, "SettingsPanel");
                var settingsImg = uiMgr.SettingsPanel.GetComponent<Image>();
                if (settingsImg != null) settingsImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
                
                if (uiMgr.SettingsPanel.transform.childCount == 0)
                {
                    CreateText(uiMgr.SettingsPanel.transform, "SettingsTitle", "SETTINGS", 45);
                    
                    // Volume Control
                    CreateText(uiMgr.SettingsPanel.transform, "VolumeLabel", "MASTER VOLUME", 24).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);
                    uiMgr.VolumeSlider = CreateSlider(uiMgr.SettingsPanel.transform, "VolumeSlider");
                    uiMgr.VolumeSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
                    
                    // Bloom Control
                    uiMgr.BloomToggle = CreateToggle(uiMgr.SettingsPanel.transform, "BloomToggle", "BLOOM EFFECT");
                    uiMgr.BloomToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);

                    var closeBtn = CreateButton(uiMgr.SettingsPanel.transform, "CloseSettingsButton", "CLOSE");
                    closeBtn.onClick.AddListener(() => uiMgr.ShowSettings(false));
                    var rt = closeBtn.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0, -150);
                }
            }

            if (uiMgr.PausePanel == null)
            {
                uiMgr.PausePanel = FindDeep(canvasObj.transform, "PausePanel")?.gameObject ?? CreatePanel(canvasObj.transform, "PausePanel");
                var pauseImg = uiMgr.PausePanel.GetComponent<Image>();
                if (pauseImg != null) pauseImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

                if (uiMgr.PausePanel.transform.childCount == 0)
                {
                    CreateText(uiMgr.PausePanel.transform, "PauseTitle", "PAUSED", 45);
                    var resumeBtn = CreateButton(uiMgr.PausePanel.transform, "ResumeButton", "RESUME");
                    resumeBtn.onClick.AddListener(GameManager.Instance.ResumeGame);
                    
                    var menuBtn = CreateButton(uiMgr.PausePanel.transform, "MenuButton", "MENU");
                    menuBtn.onClick.AddListener(GameManager.Instance.OpenMenu);
                    
                    resumeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
                    menuBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
                }
            }

            if (uiMgr.ScoreText == null) uiMgr.ScoreText = FindDeep(canvasObj.transform, "ScoreText")?.GetComponent<TextMeshProUGUI>() ?? CreateText(canvasObj.transform, "ScoreText", "0", 50);
            if (uiMgr.FeedbackText == null) uiMgr.FeedbackText = FindDeep(canvasObj.transform, "FeedbackText")?.GetComponent<TextMeshProUGUI>() ?? CreateText(canvasObj.transform, "FeedbackText", "", 60);
            
            if (uiMgr.StartButton == null)
            {
                uiMgr.StartButton = FindDeep(canvasObj.transform, "StartButton")?.GetComponent<Button>() ?? CreateButton(uiMgr.MenuPanel.transform, "StartButton", "START");
                uiMgr.StartButton.onClick.AddListener(GameManager.Instance.StartGame);
            }

            if (uiMgr.SettingsButton == null)
            {
                uiMgr.SettingsButton = FindDeep(canvasObj.transform, "SettingsButton")?.GetComponent<Button>() ?? CreateButton(uiMgr.MenuPanel.transform, "SettingsButton", "SETTINGS");
                uiMgr.SettingsButton.onClick.AddListener(() => uiMgr.ShowSettings(true));
                uiMgr.SettingsButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);
            }

            if (uiMgr.QuitButton == null)
            {
                uiMgr.QuitButton = FindDeep(canvasObj.transform, "QuitButton")?.GetComponent<Button>() ?? CreateButton(uiMgr.MenuPanel.transform, "QuitButton", "QUIT");
                uiMgr.QuitButton.onClick.AddListener(GameManager.Instance.QuitGame);
                uiMgr.QuitButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
            }

            if (uiMgr.PauseButton == null)
            {
                uiMgr.PauseButton = FindDeep(canvasObj.transform, "PauseButton")?.GetComponent<Button>() ?? CreateButton(canvasObj.transform, "PauseButton", "||");
                uiMgr.PauseButton.onClick.AddListener(GameManager.Instance.TogglePause);
                var rt = uiMgr.PauseButton.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-50, -50);
                rt.sizeDelta = new Vector2(60, 60);
            }

            if (uiMgr.RestartButton == null)
            {
                uiMgr.RestartButton = FindDeep(canvasObj.transform, "RestartButton")?.GetComponent<Button>() ?? CreateButton(uiMgr.GameOverPanel.transform, "RestartButton", "RESTART");
                uiMgr.RestartButton.onClick.AddListener(GameManager.Instance.RestartGame);
            }
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }

        private static T AddChildWithComponent<T>(GameObject parent, string name) where T : Component
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            return obj.AddComponent<T>();
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return obj;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 50);
            rt.anchoredPosition = Vector2.zero;
            
            return tmp;
        }

        private static Slider CreateSlider(Transform parent, string name)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            obj.transform.SetParent(parent);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250, 30);
            
            obj.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            var slider = obj.GetComponent<Slider>();
            
            // Background
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(obj.transform);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = Color.gray;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(obj.transform);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one; faRt.offsetMin = new Vector2(5, 5); faRt.offsetMax = new Vector2(-5, -5);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform);
            var fRt = fill.GetComponent<RectTransform>();
            fRt.offsetMin = fRt.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = Color.white;
            
            slider.fillRect = fRt;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 1;
            
            return slider;
        }

        private static Toggle CreateToggle(Transform parent, string name, string labelText)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            obj.transform.SetParent(parent);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250, 40);
            
            var toggle = obj.GetComponent<Toggle>();

            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(obj.transform);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(30, 30);
            bgRt.anchoredPosition = new Vector2(-100, 0);
            bg.GetComponent<Image>().color = Color.gray;

            GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform);
            var cRt = check.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.2f, 0.2f); cRt.anchorMax = new Vector2(0.8f, 0.8f);
            cRt.offsetMin = cRt.offsetMax = Vector2.zero;
            check.GetComponent<Image>().color = Color.white;
            
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = true;

            var label = CreateText(obj.transform, "Label", labelText, 20);
            label.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 0);
            label.alignment = TextAlignmentOptions.Left;
            
            return toggle;
        }

        private static Button CreateButton(Transform parent, string name, string text)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent);
            
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 50); // Default workable size
            
            var img = obj.GetComponent<Image>();
            img.raycastTarget = true; // Ensure it can be clicked
            
            var btn = obj.GetComponent<Button>();
            
            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(obj.transform);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
            
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // Text shouldn't block button
            
            return btn;
        }

        private static void SetupMainCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
            }
            if (mainCam.GetComponent<CinemachineBrain>() == null) mainCam.gameObject.AddComponent<CinemachineBrain>();
            if (mainCam.GetComponent<AudioListener>() == null) mainCam.gameObject.AddComponent<AudioListener>();
        }

        private static void DestroyManagerIfExists(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                // Use safe destruction for Editor scripts
                Undo.DestroyObjectImmediate(obj);
            }
        }

        private static void AddTag(string tag)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            }
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
#endif
