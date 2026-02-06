using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 无限滚动图片脚本，给原始图片加装这个脚本可以做到无限滚动
/// 1.设置你需要无限滚动的图片资源，把Wrap Mode改成Repeat
/// 2.然后声明UI Raw Image（注意这里不是用的Image）
/// 3.实现的方法就是改变这个图片的UV坐标实现重复滚动
/// </summary>
/// 
public class InfiniteScrollerImageManager:SingleBehavior<InfiniteScrollerImageManager>
{
    public List<InfiniteScrollerImage> infiniteScrollerImagesList = new List<InfiniteScrollerImage>();

    #region 构造函数初始化
    public InfiniteScrollerImageManager()
    {
        //与Mono管理器关联
        MonoMange.Instance.AddLister_Update(UpdateList);
    }
    #endregion

    #region 注册与移除,以及更新滚动图片
    public InfiniteScrollerImage AddScrollerImage(RawImage Image,float Speed_X=0.1f, float Speed_Y=0.1f,bool IsMove=true)//返还你注册的列表
    {
        InfiniteScrollerImage pack = new InfiniteScrollerImage(Image, Speed_X, Speed_Y, IsMove);
        infiniteScrollerImagesList.Add(pack);
        return pack;
        
    }
    public void RemoveScrollerImage(InfiniteScrollerImage Pack)
    {
      if(  infiniteScrollerImagesList.Contains(Pack))
      {
            Pack.IsMove = false;
            infiniteScrollerImagesList.Remove(Pack);
      }
      else
      {
            Debug.LogWarning("没有在滚动图片列表中发现该滚动图片");
            return;
      }    
    }

    //清除所有的滚动图片
    public void RemoveAllScrollerImages()
    {
        foreach (var item in infiniteScrollerImagesList)
        {
            if (item.IsMove)
                item.IsMove = false;
        }
        infiniteScrollerImagesList.Clear();//清除列表
    }

    public void UpdateList()
    {
        if(infiniteScrollerImagesList.Count>0)
            foreach (var item in infiniteScrollerImagesList)
                item.Update();
    }

    #endregion

}

public class InfiniteScrollerImage 
{
    //如果进行优化应该结合Mono进行优化
    private RawImage Image;
    [Header("基础平滑速度设置")]
    public float Speed_X = 0.1f;
    public float Speed_Y = 0.1f;
    public bool IsMove=false;

    public InfiniteScrollerImage(RawImage Image, float Speed_X, float Speed_Y,bool IsMove=true)
    {
        this.Image = Image;
        this.Speed_X = Speed_X;
        this.Speed_Y = Speed_Y;
        this.IsMove = IsMove;
    }
    public void Update()
    {
        if(IsMove)
            Image.uvRect=new Rect(Image.uvRect.position+new Vector2(Speed_X, Speed_Y)*Time.deltaTime, Image.uvRect.size);
    }
    //提供设置速度的函数
    public void SetSpeed(float Xspeed,float Yspeed)
    {
        Speed_X=Xspeed;
        Speed_Y=Yspeed;
    }

}
