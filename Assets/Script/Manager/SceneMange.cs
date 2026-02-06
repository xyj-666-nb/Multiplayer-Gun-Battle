using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneMange : SingleBehavior<SceneMange>
{
    //同步生成场景方法
    public void LoadScene(string sceneName, UnityAction CallBack = null)
    {
        Debug.Log($"加载场景{sceneName}");
        SceneManager.LoadScene(sceneName);
        //调用回调函数
        CallBack?.Invoke();
    }
    //异步生成场景方法
    public void LoadSceneAsync(string sceneName, UnityAction CallBack = null)
    {
        MonoMange.Instance.StartCoroutine(ReallyLoadAsync(sceneName, CallBack));
    }

    private IEnumerator ReallyLoadAsync(string sceneName, UnityAction CallBack, string SceneGameName = "", Sprite SceneImage = null)
    {
        AsyncOperation ao = SceneManager.LoadSceneAsync(sceneName);
        UImanager.Instance.ShowPanel<SceneLoadProgressPanel>();//显示加载进度面板

        if (SceneGameName != "" && SceneImage != null)
            UImanager.Instance.GetPanel<SceneLoadProgressPanel>().SetInfoPanel(SceneGameName, SceneImage);

        //等待场景加载完毕
        while (!ao.isDone)
        {
            EventCenter.Instance.TriggerEvent<float>(E_EventType.E_LoadSceneChange, ao.progress);// 触发加载进度事件，传入数据
            yield return 0;
        }
        EventCenter.Instance.TriggerEvent<float>(E_EventType.E_LoadSceneChange, 1);// 避免最后的1没有发送出去
        //调用回调函数
        CallBack?.Invoke();
    }
}
