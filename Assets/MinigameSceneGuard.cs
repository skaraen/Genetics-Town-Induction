using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // for GraphicRaycaster

/// <summary>
/// Since the project uses additive scene loading, this script disables
/// persistent event systems, cameras, audio listeners, and canvases from other scenes.
/// Keeps those that are in the same scene as this GameObject enabled.
/// </summary>
public class MinigameSceneGuard : MonoBehaviour
{
    [System.Serializable]
    public class AudioListenerState
    {
        public AudioListener listener;
        public bool wasEnabled;
    }

    [System.Serializable]
    public class CanvasState
    {
        public Canvas canvas;
        public bool wasEnabled;
    }

    [System.Serializable]
    public class RaycasterState
    {
        public GraphicRaycaster ray;
        public bool wasEnabled;
    }

    [SerializeField] private List<Behaviour> disabledBehaviours = new();
    [SerializeField] private List<AudioListenerState> scene1Audio = new();
    [SerializeField] private List<CanvasState> scene1Canvases = new();
    [SerializeField] private List<RaycasterState> scene1Raycasters = new();

    void Awake()
    {
        DisableExtraSceneObjects();
    }
    void OnDestroy()
    {
        EnableDisabledExtraSceneObjects();
    }

    /// <summary>
    /// Disables any event systems, cameras, audio listeners, and canvases that 
    /// are not in the same scene as this GameObject.
    /// </summary>
    private void DisableExtraSceneObjects()
    {

        void DisableAndCacheExtraEventSystems()
        {
            var allEventSystems = Resources.FindObjectsOfTypeAll<EventSystem>();
            EventSystem myEventSystem = null;

            foreach (var es in allEventSystems)
            {
                if (!es || !es.gameObject.scene.IsValid()) continue;

                if (es.gameObject.scene == gameObject.scene)
                {
                    myEventSystem = es;
                    es.enabled = true;
                    if (!es.gameObject.activeInHierarchy) es.gameObject.SetActive(true);
                    continue;
                }

                if (es.enabled)
                {
                    es.enabled = false;
                    disabledBehaviours.Add(es);
                }
            }

            if (myEventSystem == null)
            {
                var go = new GameObject("EventSystem (Minigame)");
                go.transform.SetParent(transform, false);
                myEventSystem = go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }
        void DisableAndCacheExtraCameras()
        {
            var allCameras = Resources.FindObjectsOfTypeAll<Camera>();
            foreach (var cam in allCameras)
            {
                if (!cam || !cam.gameObject.scene.IsValid()) continue;

                if (cam.gameObject.scene != gameObject.scene)
                {
                    if (cam.enabled)
                    {
                        cam.enabled = false;
                        disabledBehaviours.Add(cam);
                    }
                }
                else
                {
                    cam.enabled = true;
                }
            }
        }
        void DisableAndCacheExtraAudioListeners()
        {
            var allListeners = Resources.FindObjectsOfTypeAll<AudioListener>();
            bool scene2HasListener = false;

            foreach (var al in allListeners)
            {
                if (!al || !al.gameObject.scene.IsValid()) continue;

                if (al.gameObject.scene == gameObject.scene)
                {
                    scene2HasListener |= al.enabled;
                }
                else
                {
                    scene1Audio.Add(new AudioListenerState {
                        listener = al, 
                        wasEnabled = al.enabled
                    });
                    if (al.enabled) al.enabled = false;
                }
            }

            if (!scene2HasListener)
            {
                AudioListener pick = null;
                foreach (var al in allListeners)
                {
                    if (al && al.gameObject.scene == gameObject.scene) { pick = al; break; }
                }
                if (pick != null) pick.enabled = true;
                else
                {
                    var go = new GameObject("AudioListener (Minigame)");
                    go.transform.SetParent(transform, false);
                    go.AddComponent<AudioListener>();
                }
            }

        }
        void DisableAndCacheExtraCanvases()
        {
            // Disable ALL canvases that are not in the minigame scene.
            // This hides Screen Space - Overlay and World Space UI from Scene1.
            var allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (var cv in allCanvases)
            {
                if (!cv || !cv.gameObject.scene.IsValid()) continue;

                if (cv.gameObject.scene != gameObject.scene)
                {
                    scene1Canvases.Add(new CanvasState {
                        canvas = cv, 
                        wasEnabled = cv.enabled
                    });
                    if (cv.enabled) {
                        cv.enabled = false;
                        Debug.Log($"Disabled canvas [{cv.name}]");
                    }

                    // Also disable input on those UIs
                    var ray = cv.GetComponent<GraphicRaycaster>();
                    if (ray)
                    {
                        scene1Raycasters.Add(new RaycasterState {
                            ray = ray, 
                            wasEnabled = ray.enabled
                        });
                        if (ray.enabled) {
                            ray.enabled = false;
                            Debug.Log($"Disabled raycaster [{cv.name}, {ray.name}]");
                        }
                    }
                }
                else
                {
                    // Ensure minigame canvases are active
                    cv.enabled = true;
                    var ray = cv.GetComponent<GraphicRaycaster>();
                    if (ray) ray.enabled = true;
                }
            }
        }

        DisableAndCacheExtraEventSystems();
        DisableAndCacheExtraCameras();
        DisableAndCacheExtraAudioListeners();
        DisableAndCacheExtraCanvases();
    }

    /// <summary>
    /// Enables any event systems, cameras, audio listeners, and canvases that 
    /// were disabled in DisableExtraSceneObjects
    /// </summary>
    private void EnableDisabledExtraSceneObjects()
    {
        // Re-enable EventSystems & Cameras we disabled
        foreach (var b in disabledBehaviours)
            if (b) b.enabled = true;
        disabledBehaviours.Clear();

        // Restore AudioListeners
        foreach (AudioListenerState ls in scene1Audio)
            if (ls.listener)
                ls.listener.enabled = ls.wasEnabled;
        scene1Audio.Clear();

        // Restore Canvases
        foreach (CanvasState cs in scene1Canvases)
            if (cs.canvas) {
                cs.canvas.enabled = cs.wasEnabled;
                Debug.Log($"Return canvas: [{cs.canvas.name}, {cs.wasEnabled}]");
            }
        scene1Canvases.Clear();

        // Restore Raycasters
        foreach (RaycasterState rs in scene1Raycasters)
            if (rs.ray) {
                rs.ray.enabled = rs.wasEnabled;
                Debug.Log($"Return raycaster: [{rs.ray.name}, {rs.wasEnabled}]");
            }
        scene1Raycasters.Clear();
    }

}
