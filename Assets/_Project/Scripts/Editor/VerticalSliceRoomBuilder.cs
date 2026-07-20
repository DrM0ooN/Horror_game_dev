using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using HorrorGame.Core;
using HorrorGame.Player;
using HorrorGame.MiniGames;

namespace HorrorGame.EditorTools
{
    /// <summary>
    /// One-shot builder for the Section 6 vertical slice: a hub floor, one lockable challenge
    /// room, and the flashlight mini-game, wired end-to-end with placeholder primitives.
    /// Safe to re-run from the menu — it deletes and rebuilds its own "VerticalSlice" hierarchy
    /// each time, and only adds (never duplicates) components on the existing Player rig.
    /// </summary>
    public static class VerticalSliceRoomBuilder
    {
        private const string RootName = "VerticalSlice";
        private static readonly Vector3 RoomCenter = new Vector3(0f, 0f, 17.5f);

        // CharacterController is 2 units tall (see PlayerFirstPersonController) — these give a
                        // 26x18m interior, 6.5m wall height, and a 2.6m-tall doorway — the doorway stays
        // human-scaled on purpose, dwarfed by the much taller/wider dark walls around it.
        // (grown twice now: 6x7m -> 16x18m -> current; doorway height has stayed 2.6m throughout)
        // instead of a cramped box sized without the player in mind.
        private const float RoomHalfWidth = 13f;
        private const float RoomHalfDepth = 9f;
        private const float WallHeight = 6.5f;
        private const float WallThickness = 0.2f;
        private const float DoorwayHalfWidth = 0.9f;
                private const float DoorwayHeight = 2.6f;

        // Hub: an even larger enclosed space sitting immediately in front of (south of) the
        // challenge room, sharing the exact same doorway threshold. No separate north wall is
        // built for the hub -- the room's own south wall (with its doorway gap) already forms
        // that boundary at this same z-plane; a second one would just z-fight with it.
        private const float HubHalfWidth = 17f;
        private const float HubHalfDepth = 13f;
        private const float HubWallHeight = 8f;
                private static readonly Vector3 HubCenter = new Vector3(0f, 0f, RoomCenter.z - RoomHalfDepth - HubHalfDepth);

        // Where the player/players spawn at run start and get teleported back to on room solve
        // or fail. Change this one value to move it -- everything that uses it (BuildHubRespawnPoint)
        // just reads it. Currently the hub's center; not yet a final, deliberately-chosen spot.
        private static readonly Vector3 HubSpawnPosition = new Vector3(0f, 1f, HubCenter.z);

        private struct Palette
        {
            public Material Wall;
            public Material Floor;
            public Material Door;
            public Material Button;
            public Material Fragment;
            public Material Monster;
            public Material ThreatStep;
            public Material Furniture;
        }

        [MenuItem("HorrorGame/Build Vertical Slice Room")]
        public static void Build()
        {
            var existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot);
                Debug.Log("[VerticalSliceRoomBuilder] Removed previous VerticalSlice hierarchy before rebuilding.");
            }

            var root = new GameObject(RootName).transform;
            var palette = BuildPalette();

            BuildFloor(root, palette);
                        Debug.Log("[VerticalSliceRoomBuilder] Hub floor placeholder created.");

            BuildHubRoom(root, palette);
                        Debug.Log("[VerticalSliceRoomBuilder] Hub room (walls, ceiling, swinging light, monster props) built.");

            BuildNotificationHud(root);
            Debug.Log("[VerticalSliceRoomBuilder] Notification HUD wired.");

            var hubThreatSystem = BuildHubThreatSystem(root, palette);
            Debug.Log("[VerticalSliceRoomBuilder] HubThreatSystem + ThreatIndicatorView wired.");

            var hubRespawnPoint = BuildHubRespawnPoint(root);

            var monsterSwitcher = BuildRoom(root, palette, hubThreatSystem, hubRespawnPoint);
            Debug.Log("[VerticalSliceRoomBuilder] Challenge room (walls, door, button, flashlight mini-game) built and wired.");

            WirePlayer(monsterSwitcher);
            Debug.Log("[VerticalSliceRoomBuilder] Player rig wired: RoomPlayerMarker + FlashlightItem + FlashlightBeamDetector.");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[VerticalSliceRoomBuilder] Vertical slice build complete and scene saved.");
        }

private static Palette BuildPalette()
        {
            return new Palette
            {
                Wall = CreateMaterial(new Color(0.13f, 0.1f, 0.07f), texturePath: "Assets/_Project/Art/Textures/Wall_Texture.png", textureTiling: new Vector2(6f, 2f)),
                Floor = CreateMaterial(new Color(0.18f, 0.13f, 0.08f), texturePath: "Assets/_Project/Art/Textures/Floor_Texture.png", textureTiling: new Vector2(8f, 8f)),
                Door = CreateMaterial(new Color(0.42f, 0.27f, 0.12f), texturePath: "Assets/_Project/Art/Textures/Door_Texture.png", textureTiling: new Vector2(2f, 1f)),
                Button = CreateMaterial(new Color(0.85f, 0.6f, 0.2f), new Color(0.5f, 0.3f, 0.05f)),
                Fragment = CreateMaterial(new Color(0.95f, 0.78f, 0.3f), new Color(0.9f, 0.6f, 0.15f)),
                Monster = CreateMaterial(new Color(0.05f, 0.02f, 0.02f)),
                ThreatStep = CreateMaterial(new Color(0.55f, 0.1f, 0.05f), new Color(0.4f, 0.05f, 0.02f)),
                Furniture = CreateMaterial(new Color(0.2f, 0.14f, 0.09f), texturePath: "Assets/_Project/Art/Textures/Furniture_Texture.png", textureTiling: new Vector2(2f, 2f)),
            };
        }

private static Material CreateMaterial(Color color, Color? emission = null, string texturePath = null, Vector2? textureTiling = null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);

            // URP/Lit has no "_Color" property (Material.color is a no-op on it and silently
            // leaves the material default grey) — it uses "_BaseColor" instead.
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else
            {
                mat.color = color;
            }

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            if (!string.IsNullOrEmpty(texturePath))
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture != null)
                {
                    mat.mainTexture = texture;
                    // Primitive cubes keep fixed 0-1 UVs regardless of scale, so a shared material's
                    // texture would otherwise stretch a single tile across every wall/floor face
                    // (which differ wildly in size) -- an approximate flat tiling factor per material
                    // reads far better than that, even though it isn't exact per-instance.
                    mat.mainTextureScale = textureTiling ?? Vector2.one;

                    // URP/Lit multiplies _BaseColor against the texture sample -- leaving the base
                    // color at the original dark palette tone (chosen for the flat-color look) would
                    // multiply two similarly-dark values together and crush the texture to near-black.
                    // The texture's own colors already carry the intended look, so tint white here.
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", Color.white);
                    }
                    else
                    {
                        mat.color = Color.white;
                    }
                }
                else
                {
                    Debug.LogWarning($"[VerticalSliceRoomBuilder] Texture not found at '{texturePath}' — material left as a flat color.");
                }
            }

            return mat;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

private static void BuildFloor(Transform root, Palette palette)
        {
            // The previous VerticalSlice hierarchy (including any floor it made) was already
            // destroyed above, so any "Floor" still found here belongs to someone else (another
            // AI tool or the user) — reuse it instead of creating a second, overlapping one.
            var existingFloor = GameObject.Find("Floor");
            if (existingFloor != null)
            {
                // Reusing the object (geometry/position/scale untouched), but still apply the
                // current palette's Floor material -- otherwise this reused object silently keeps
                // whatever material it happened to have before, ignoring anything BuildPalette()
                // sets up (e.g. the procedural floor texture), which is never actually visible.
                var existingRenderer = existingFloor.GetComponent<MeshRenderer>();
                if (existingRenderer != null)
                {
                    existingRenderer.sharedMaterial = palette.Floor;
                }

                Debug.Log("[VerticalSliceRoomBuilder] Found an existing 'Floor' object already in the scene — reusing its geometry but applying the current Floor material.");
                return;
            }

            // Sized to comfortably cover the hub area plus the full 16x18m room footprint
            // (room now spans z:[8.5, 26.5] at RoomCenter.z=17.5, RoomHalfDepth=9) with margin.
            CreateBox("Floor", root, new Vector3(0f, -0.1f, 9f), new Vector3(30f, 0.2f, 40f), palette.Floor);
        }

private static HubThreatSystem BuildHubThreatSystem(Transform root, Palette palette)
        {
            var systemGo = new GameObject("HubThreatSystem");
            systemGo.transform.SetParent(root, false);
            var hubThreatSystem = systemGo.AddComponent<HubThreatSystem>();

            var indicatorGo = new GameObject("ThreatIndicator");
            indicatorGo.transform.SetParent(systemGo.transform, false);
            // Mounted against the hub's south wall (opposite the challenge room) so it reads as a
            // fixture on the wall rather than floating in the middle of the much bigger hub.
            indicatorGo.transform.localPosition = new Vector3(0f, 2.2f, HubCenter.z - HubHalfDepth + 0.3f);
            var indicatorView = indicatorGo.AddComponent<ThreatIndicatorView>();

            const int stepCount = 5;
            var steps = new Object[stepCount];
            for (var i = 0; i < stepCount; i++)
            {
                // "Orbs" per the hub brief, not the placeholder cubes used previously.
                var step = CreateSphere($"ThreatOrb_{i + 1}", indicatorGo.transform, new Vector3(i * 0.35f, 0f, 0f), 0.3f, palette.ThreatStep);
                step.SetActive(false);
                steps[i] = step;
            }

            SetObjectArrayField(indicatorView, "threatSteps", steps);
            SetObjectField(hubThreatSystem, "indicatorView", indicatorView);

            return hubThreatSystem;
        }

private static Transform BuildHubRespawnPoint(Transform root)
        {
            var go = new GameObject("HubRespawnPoint");
            go.transform.SetParent(root, false);
            go.transform.localPosition = HubSpawnPosition;

            return go.transform;
        }

        private static FlashlightMonsterSwitcher BuildRoom(Transform root, Palette palette, HubThreatSystem hubThreatSystem, Transform hubRespawnPoint)
        {
            var roomGo = new GameObject("ChallengeRoom_Flashlight");
            roomGo.transform.SetParent(root, false);
            roomGo.transform.position = RoomCenter;

            BuildWalls(roomGo.transform, palette);
            BuildFurniture(roomGo.transform, palette);

            var doorController = BuildDoor(roomGo.transform, palette);
            var startButton = BuildStartButton(roomGo.transform, palette);
            var monsterSwitcher = BuildMonster(roomGo.transform, palette);
            var fragments = BuildFragments(roomGo.transform, palette);
            BuildLights(roomGo.transform);

            var miniGameGo = new GameObject("FlashlightMiniGame");
            miniGameGo.transform.SetParent(roomGo.transform, false);
            var miniGame = miniGameGo.AddComponent<FlashlightMiniGame>();
            SetObjectArrayField(miniGame, "fragments", fragments);
            SetObjectField(miniGame, "monsterSwitcher", monsterSwitcher);
            SetObjectField(miniGame, "hubThreatSystem", hubThreatSystem);

            var roomCollider = roomGo.AddComponent<BoxCollider>();
            roomCollider.size = new Vector3(RoomHalfWidth * 2f - WallThickness, WallHeight, RoomHalfDepth * 2f - WallThickness + 0.4f);
            roomCollider.center = new Vector3(0f, WallHeight / 2f, 0f);

            var roomController = roomGo.AddComponent<RoomController>();
            SetIntField(roomController, "requiredPlayers", 1);
            SetObjectField(roomController, "doorController", doorController);
            SetObjectField(roomController, "miniGameBehaviour", miniGame);
            SetObjectField(roomController, "hubThreatSystem", hubThreatSystem);
            SetObjectArrayField(roomController, "hubRespawnPoints", new Object[] { hubRespawnPoint });

            SetObjectField(startButton, "roomController", roomController);

            return monsterSwitcher;
        }

        private static void BuildWalls(Transform roomRoot, Palette palette)
        {
            var w = RoomHalfWidth;
            var d = RoomHalfDepth;
            var h = WallHeight;
            var t = WallThickness;

            // North, east, west walls are full-length. South wall has a doorway gap.
            CreateBox("Wall_North", roomRoot, new Vector3(0f, h / 2f, d), new Vector3(w * 2f, h, t), palette.Wall);
            CreateBox("Wall_East", roomRoot, new Vector3(w, h / 2f, 0f), new Vector3(t, h, d * 2f), palette.Wall);
            CreateBox("Wall_West", roomRoot, new Vector3(-w, h / 2f, 0f), new Vector3(t, h, d * 2f), palette.Wall);

            var southSegmentWidth = w - DoorwayHalfWidth;
            var southSegmentCenterX = DoorwayHalfWidth + southSegmentWidth / 2f;
            CreateBox("Wall_South_Left", roomRoot, new Vector3(-southSegmentCenterX, h / 2f, -d), new Vector3(southSegmentWidth, h, t), palette.Wall);
            CreateBox("Wall_South_Right", roomRoot, new Vector3(southSegmentCenterX, h / 2f, -d), new Vector3(southSegmentWidth, h, t), palette.Wall);
            CreateBox("Wall_South_Lintel", roomRoot, new Vector3(0f, (DoorwayHeight + h) / 2f, -d), new Vector3(DoorwayHalfWidth * 2f, h - DoorwayHeight, t), palette.Wall);

            // Without a ceiling the box was open-topped, so the Directional Light (and skybox
            // ambient) flooded straight in and lit the room evenly with no falloff at all. A real
            // ceiling occludes both, so the two placed point lights become the only real sources.
            CreateBox("Ceiling", roomRoot, new Vector3(0f, h, 0f), new Vector3(w * 2f, t, d * 2f), palette.Wall);
        }

private static void BuildHubRoom(Transform root, Palette palette)
        {
            var hubGo = new GameObject("Hub");
            hubGo.transform.SetParent(root, false);
            hubGo.transform.position = HubCenter;

            BuildHubWalls(hubGo.transform, palette);
            BuildHubLight(hubGo.transform);
            BuildHubMonsterProps(hubGo.transform, palette);
        }

        private static void BuildHubWalls(Transform hubRoot, Palette palette)
        {
            var w = HubHalfWidth;
            var d = HubHalfDepth;
            var h = HubWallHeight;
            var t = WallThickness;

            CreateBox("Hub_Wall_South", hubRoot, new Vector3(0f, h / 2f, -d), new Vector3(w * 2f, h, t), palette.Wall);
            CreateBox("Hub_Wall_East", hubRoot, new Vector3(w, h / 2f, 0f), new Vector3(t, h, d * 2f), palette.Wall);
            CreateBox("Hub_Wall_West", hubRoot, new Vector3(-w, h / 2f, 0f), new Vector3(t, h, d * 2f), palette.Wall);
            CreateBox("Hub_Ceiling", hubRoot, new Vector3(0f, h, 0f), new Vector3(w * 2f, t, d * 2f), palette.Wall);
        }

        private static void BuildHubLight(Transform hubRoot)
        {
            // A pivot near the ceiling with the actual Light offset below it on a "cord" -- rotating
            // the pivot swings the light sideways like a pendulum. Meant to be the hub's single,
            // dominant light source (the hub is now fully enclosed, so sun/ambient can't leak in) --
            // SwingingFlickeringLight adds the gentle sway and occasional brief flicker.
            var pivotGo = new GameObject("Light_Hub_Pivot");
            pivotGo.transform.SetParent(hubRoot, false);
            pivotGo.transform.localPosition = new Vector3(0f, HubWallHeight - 0.2f, 0f);

            var lightGo = new GameObject("Light_Hub");
            lightGo.transform.SetParent(pivotGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, -2.2f, 0f);
            var light = lightGo.AddComponent<Light>();
            ConfigureWarmLight(light, intensity: 4f, range: 15f, colorTemperature: 2600f);

            pivotGo.AddComponent<SwingingFlickeringLight>();
        }

        private static void BuildHubMonsterProps(Transform hubRoot, Palette palette)
        {
            // Decorative "monster" props per the hub brief -- purely atmospheric, no collider,
            // matching the dark creature-prop style already used on the challenge room's furniture.
            var positions = new[]
            {
                new Vector3(-HubHalfWidth + 2f, 0.5f, -HubHalfDepth + 2f),
                new Vector3(HubHalfWidth - 2f, 0.5f, -HubHalfDepth + 2f),
                new Vector3(0f, 0.5f, HubHalfDepth - 1.5f),
            };

            for (var i = 0; i < positions.Length; i++)
            {
                var prop = CreateSphere($"HubMonsterProp_{i + 1}", hubRoot, positions[i], 1f, palette.Monster);
                Object.DestroyImmediate(prop.GetComponent<SphereCollider>());
            }
        }

private static void BuildNotificationHud(Transform root)
        {
            var hudGo = new GameObject("NotificationHud");
            hudGo.transform.SetParent(root, false);

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/NotificationHud.uxml");
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/_Project/UI/NotificationHudPanelSettings.asset");

            if (visualTree == null || panelSettings == null)
            {
                Debug.LogError("[VerticalSliceRoomBuilder] Could not load NotificationHud UXML/PanelSettings assets -- HUD not wired.");
                return;
            }

            var uiDocument = hudGo.AddComponent<UIDocument>();
            uiDocument.visualTreeAsset = visualTree;
            uiDocument.panelSettings = panelSettings;

            hudGo.AddComponent<NotificationHud>();
        }


        private static GameObject CreateSphere(string name, Transform parent, Vector3 localPosition, float diameter, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }


        private static void BuildFurniture(Transform roomRoot, Palette palette)
        {
            var d = RoomHalfDepth;
            var flankX = RoomHalfWidth - 0.8f;
            var positions = new[] { -flankX, flankX };
            foreach (var x in positions)
            {
                var cabinet = CreateBox($"Furniture_Cabinet_{(x < 0 ? "L" : "R")}", roomRoot, new Vector3(x, 0.8f, -d + 0.4f), new Vector3(0.6f, 1.6f, 0.4f), palette.Furniture);

                var prop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                prop.name = "CreatureProp_Placeholder";
                prop.transform.SetParent(cabinet.transform, false);
                prop.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                prop.transform.localScale = Vector3.one * 0.35f;
                prop.GetComponent<MeshRenderer>().sharedMaterial = palette.Monster;
                Object.DestroyImmediate(prop.GetComponent<SphereCollider>());
            }
        }

        private static RoomDoorController BuildDoor(Transform roomRoot, Palette palette)
        {
            var doorGo = new GameObject("Door");
            doorGo.transform.SetParent(roomRoot, false);
            doorGo.transform.localPosition = new Vector3(0f, DoorwayHeight / 2f, -RoomHalfDepth);

            var closedVisual = CreateBox("DoorClosedVisual", doorGo.transform, Vector3.zero, new Vector3(DoorwayHalfWidth * 2f, DoorwayHeight, 0.15f), palette.Door);
            Object.DestroyImmediate(closedVisual.GetComponent<BoxCollider>());

            var openVisual = new GameObject("DoorOpenVisual");
            openVisual.transform.SetParent(doorGo.transform, false);

            var blocker = doorGo.AddComponent<BoxCollider>();
            blocker.size = new Vector3(DoorwayHalfWidth * 2f, DoorwayHeight, 0.15f);
            blocker.enabled = false;

            closedVisual.SetActive(false);
            openVisual.SetActive(true);

            var doorController = doorGo.AddComponent<RoomDoorController>();
            SetObjectField(doorController, "doorBlocker", blocker);
            SetObjectField(doorController, "openVisual", openVisual);
            SetObjectField(doorController, "closedVisual", closedVisual);

            return doorController;
        }

        private static RoomStartButton BuildStartButton(Transform roomRoot, Palette palette)
        {
            var buttonGo = CreateBox("StartButton", roomRoot, new Vector3(0f, 0.9f, -RoomHalfDepth + 0.7f), Vector3.one * 0.4f, palette.Button);
            buttonGo.GetComponent<BoxCollider>().isTrigger = true;

            var startButton = buttonGo.AddComponent<RoomStartButton>();
            var activator = buttonGo.AddComponent<RoomStartButtonActivator>();
            SetObjectField(activator, "startButton", startButton);

            return startButton;
        }

private static FlashlightMonsterSwitcher BuildMonster(Transform roomRoot, Palette palette)
        {
            var anchorsGo = new GameObject("MonsterAnchors");
            anchorsGo.transform.SetParent(roomRoot, false);

            // Anchor positions are expressed as fractions of RoomHalfWidth/RoomHalfDepth (carried
            // over from the original 6x7m layout: ~73% out to the side walls, ~74% toward the north
            // wall for the front pair, ~66% toward the south/doorway wall for the back one) so they
            // scale proportionally with the room footprint instead of floating at old fixed offsets.
            var anchorPositions = new[]
            {
                new Vector3(-RoomHalfWidth * 0.7333f, 1.2f, RoomHalfDepth * 0.7429f),
                new Vector3(RoomHalfWidth * 0.7333f, 1.2f, RoomHalfDepth * 0.7429f),
                new Vector3(0f, 1.2f, -RoomHalfDepth * 0.6571f),
            };
            var anchors = new Object[anchorPositions.Length];
            for (var i = 0; i < anchorPositions.Length; i++)
            {
                var anchorGo = new GameObject($"MonsterAnchor_{i + 1}");
                anchorGo.transform.SetParent(anchorsGo.transform, false);
                anchorGo.transform.localPosition = anchorPositions[i];
                anchors[i] = anchorGo.transform;
            }

            var monsterGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            monsterGo.name = "Monster_Placeholder";
            monsterGo.transform.SetParent(roomRoot, false);
            monsterGo.GetComponent<MeshRenderer>().sharedMaterial = palette.Monster;
            monsterGo.GetComponent<CapsuleCollider>().isTrigger = true;

            var monsterSwitcher = monsterGo.AddComponent<FlashlightMonsterSwitcher>();
            SetObjectArrayField(monsterSwitcher, "anchors", anchors);
            SetIntField(monsterSwitcher, "startingAnchorIndex", 0);

            return monsterSwitcher;
        }

private static Object[] BuildFragments(Transform roomRoot, Palette palette)
        {
            // Positions are expressed as fractions of RoomHalfWidth/RoomHalfDepth (carried over
            // from the original 6x7m layout: ~67% out toward the side walls, ~66% toward the
            // north/south walls for the four corner-ish fragments, ~80% toward the north wall for
            // the fifth) so they scale proportionally with the room footprint instead of floating
            // clustered near the center of the much bigger room.
            var fragmentPositions = new[]
            {
                new Vector3(-RoomHalfWidth * 0.6667f, 1f, -RoomHalfDepth * 0.6571f),
                new Vector3(RoomHalfWidth * 0.6667f, 1f, -RoomHalfDepth * 0.6571f),
                new Vector3(-RoomHalfWidth * 0.6667f, 1f, RoomHalfDepth * 0.6571f),
                new Vector3(RoomHalfWidth * 0.6667f, 1f, RoomHalfDepth * 0.6571f),
                new Vector3(0f, 1f, RoomHalfDepth * 0.8f),
            };

            var fragments = new Object[fragmentPositions.Length];
            for (var i = 0; i < fragmentPositions.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"KeyFragment_{i + 1}";
                go.transform.SetParent(roomRoot, false);
                go.transform.localPosition = fragmentPositions[i];
                go.transform.localScale = Vector3.one * 0.3f;
                go.GetComponent<MeshRenderer>().sharedMaterial = palette.Fragment;
                go.GetComponent<SphereCollider>().isTrigger = true;

                var fragment = go.AddComponent<FlashlightKeyFragment>();
                SetObjectField(fragment, "collectedVisual", go);
                fragments[i] = fragment;
            }

            return fragments;
        }

private static void BuildLights(Transform roomRoot)
        {
            // Kept deliberately dim/tight now that scene ambient is near-black (see the global
            // lighting fix logged in the project brief) — these are mood/orientation lights only,
            // not enough to see the room by; the player's own flashlight is the primary light.
            var ceilingGo = new GameObject("Light_Ceiling");
            ceilingGo.transform.SetParent(roomRoot, false);
            ceilingGo.transform.localPosition = new Vector3(0f, WallHeight - 0.3f, 0f);
            var ceilingLight = ceilingGo.AddComponent<Light>();
            ConfigureWarmLight(ceilingLight, intensity: 1f, range: 2.2f, colorTemperature: 2400f);

            var secondaryGo = new GameObject("Light_Secondary");
            secondaryGo.transform.SetParent(roomRoot, false);
            secondaryGo.transform.localPosition = new Vector3(0f, 1.4f, -RoomHalfDepth + 0.6f);
            var secondaryLight = secondaryGo.AddComponent<Light>();
            ConfigureWarmLight(secondaryLight, intensity: 0.6f, range: 1.8f, colorTemperature: 2600f);
        }

        private static void ConfigureWarmLight(Light light, float intensity, float range, float colorTemperature)
        {
            light.type = LightType.Point;
            light.color = Color.white;
            light.useColorTemperature = true;
            light.colorTemperature = colorTemperature;
            light.intensity = intensity;
            light.range = range;

            // Soft shadows so walls/furniture actually occlude these lights — without shadows
            // every surface in range gets lit flat with no contrast, regardless of range/intensity.
            light.shadows = LightShadows.Soft;
        }

private static void WirePlayer(FlashlightMonsterSwitcher monsterSwitcher)
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[VerticalSliceRoomBuilder] Could not find a 'Player' GameObject in the scene — skipped player wiring.");
                return;
            }

            // Same single source of truth as the hub respawn point, so "where you start" and
            // "where you get sent back to" can never drift apart — move HubSpawnPosition and both
            // follow.
            player.transform.position = HubSpawnPosition;

            var playerCamera = GameObject.Find("PlayerCamera");
            var flashlightGo = GameObject.Find("PlayerFlashlight");
            if (flashlightGo == null)
            {
                Debug.LogError("[VerticalSliceRoomBuilder] Could not find 'PlayerFlashlight' GameObject in the scene — skipped flashlight wiring.");
            }
            else
            {
                if (playerCamera != null)
                {
                    flashlightGo.transform.SetParent(playerCamera.transform, false);
                    flashlightGo.transform.localPosition = Vector3.zero;
                    flashlightGo.transform.localRotation = Quaternion.identity;
                }

                var light = flashlightGo.GetComponent<Light>();
                if (light != null)
                {
                    light.type = LightType.Spot;
                                        light.spotAngle = 40f;
                    light.range = 10f;
                    light.color = Color.white;
                    light.useColorTemperature = true;
                    light.colorTemperature = 2400f;
                                        light.intensity = 6f;
                }

                var flashlightItem = flashlightGo.GetComponent<FlashlightItem>();
                if (flashlightItem == null)
                {
                    flashlightItem = flashlightGo.AddComponent<FlashlightItem>();
                }

                SetObjectField(flashlightItem, "flashlightLight", light);

                var detector = flashlightGo.GetComponent<FlashlightBeamDetector>();
                if (detector == null)
                {
                    detector = flashlightGo.AddComponent<FlashlightBeamDetector>();
                }

                SetObjectField(detector, "beamOrigin", flashlightGo.transform);
                                SetObjectField(detector, "monsterSwitcher", monsterSwitcher);

                // Explicit, not just the script's field default — Unity does not retroactively
                // apply a changed [SerializeField] default to an already-serialized component, so
                // without this a rebuild on an existing detector would silently keep a stale range.
                SetFloatField(detector, "range", 10f);

                // Reuse whatever InputActionAsset is already wired on the player's first-person
                // controller (rather than hardcoding an asset path) so the Interact action used
                // for fragment collection always matches the asset actually driving movement/look.
                var playerController = player.GetComponent<PlayerFirstPersonController>();
                if (playerController != null)
                {
                    var controllerSo = new SerializedObject(playerController);
                    var assetProp = controllerSo.FindProperty("inputAsset");
                    var inputAsset = assetProp != null ? assetProp.objectReferenceValue as InputActionAsset : null;
                    if (inputAsset != null)
                    {
                        SetObjectField(detector, "inputAsset", inputAsset);
                    }
                    else
                    {
                        Debug.LogWarning("[VerticalSliceRoomBuilder] PlayerFirstPersonController has no InputActionAsset assigned — FlashlightBeamDetector's Interact action was not wired. Assign the asset on the Player and re-run the build.");
                    }
                }
                else
                {
                    Debug.LogWarning("[VerticalSliceRoomBuilder] Could not find PlayerFirstPersonController on 'Player' — FlashlightBeamDetector's Interact action was not wired.");
                }

                var marker = player.GetComponent<RoomPlayerMarker>();
                if (marker == null)
                {
                    marker = player.AddComponent<RoomPlayerMarker>();
                }

                SetIntField(marker, "playerId", 0);
                SetObjectField(marker, "bodyRoot", player.transform);
                SetObjectField(marker, "flashlightItem", flashlightItem);
            }
        }

        private static void SetObjectField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[VerticalSliceRoomBuilder] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetObjectArrayField(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[VerticalSliceRoomBuilder] Array field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedProperties();
        }

        private static void SetIntField(Object target, string fieldName, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[VerticalSliceRoomBuilder] Int field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            prop.intValue = value;
            so.ApplyModifiedProperties();
        }

private static void SetFloatField(Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[VerticalSliceRoomBuilder] Float field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            prop.floatValue = value;
            so.ApplyModifiedProperties();
        }

    }
}
