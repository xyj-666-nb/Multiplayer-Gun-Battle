using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneCanvasRaycasterOptimizer
{
    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_initialized)
        {
            OptimizeActiveScene();
            return;
        }

        _initialized = true;
        OptimizeActiveScene();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        OptimizeActiveScene();
    }

    private static void OptimizeActiveScene()
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                continue;

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null || !raycaster.enabled)
                continue;

            if (HasInteractiveUi(canvas))
                continue;

            raycaster.enabled = false;
        }
    }

    private static bool HasInteractiveUi(Canvas canvas)
    {
        if (canvas.GetComponentsInChildren<Selectable>(true).Length > 0)
            return true;

        return canvas.GetComponentsInChildren<IEventSystemHandler>(true).Length > 0;
    }
}
