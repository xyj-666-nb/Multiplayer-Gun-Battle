using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneCanvasRaycasterOptimizer
{
    private const string SkipButtonName = "SkipButton";
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
            if (canvas == null || ShouldKeepUiRaycasts(canvas))
                continue;

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = false;

            DisableGraphicRaycastTargets(canvas);
        }
    }

    private static bool ShouldKeepUiRaycasts(Canvas canvas)
    {
        if (canvas.GetComponentInParent<BasePanel>(true) != null)
            return true;

        if (HasSkipButton(canvas))
            return true;

        Scene scene = canvas.gameObject.scene;
        return !scene.IsValid() || !scene.isLoaded || scene.name == "DontDestroyOnLoad";
    }

    private static bool HasSkipButton(Canvas canvas)
    {
        Selectable[] selectables = canvas.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable != null && selectable.name == SkipButtonName)
                return true;
        }

        return false;
    }

    private static void DisableGraphicRaycastTargets(Canvas canvas)
    {
        Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }
}
