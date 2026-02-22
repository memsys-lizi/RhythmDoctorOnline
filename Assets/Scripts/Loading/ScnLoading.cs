using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScnLoading : MonoBehaviour
{
    private static ScnLoading instance;
    public static ScnLoading Instance
    {
        get { return instance; }
    }

    [Header("UI组件")]
    [Tooltip("动画控制器")]
    public Animator animator;
    [Tooltip("进度条")]
    public Slider progressSlider;
    [Tooltip("百分比文本")]
    public Text progressText;
    [Tooltip("提示文本")]
    public Text tipText;

    [Header("时间设置")]
    [Tooltip("最低显示时间（秒）")]
    public float minimumLoadTime = 3f;
    [Tooltip("结束动画等待时间（秒）")]
    public float endWaitTime = 3f;

    // 加载状态
    private bool isLoading = false;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化时自动获取组件
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (progressSlider == null)
            progressSlider = GetComponentInChildren<Slider>();

        // 获取所有Text组件并分配
        if (progressText == null || tipText == null)
        {
            Text[] textComponents = GetComponentsInChildren<Text>();
            foreach (Text text in textComponents)
            {
                if (text.name.ToLower().Contains("tip") && tipText == null)
                    tipText = text;
                else if (text.name.ToLower().Contains("progress") || text.name.ToLower().Contains("percent"))
                    progressText = text;
                else if (progressText == null)
                    progressText = text; // 默认第一个Text作为进度文本
            }
        }
    }

    /// <summary>
    /// 静态方法：加载指定场景
    /// </summary>
    /// <param name="sceneName">要加载的场景名称</param>
    public static void LoadScenes(string sceneName)
    {
        if (Instance != null && !Instance.isLoading)
        {
            Instance.StartCoroutine(Instance.LoadingSequence(sceneName));
        }
        else if (Instance == null)
        {
            Debug.LogError("ScnLoading实例不存在！请确保场景中有ScnLoading组件。");
        }
        else
        {
            Debug.LogWarning("已经在加载中，请等待当前加载完成。");
        }
    }
    
    /// <summary>
    /// 静态方法：显示加载动画（不加载场景）
    /// </summary>
    /// <param name="tipMessage">可选的提示信息，为null则使用随机提示</param>
    public static void ShowLoading(string tipMessage = null)
    {
        if (Instance != null && !Instance.isLoading)
        {
            Instance.StartCoroutine(Instance.ShowLoadingAnimation(tipMessage));
        }
        else if (Instance == null)
        {
            Debug.LogError("ScnLoading实例不存在！请确保场景中有ScnLoading组件。");
        }
    }
    
    /// <summary>
    /// 静态方法：隐藏加载动画
    /// </summary>
    public static void HideLoading()
    {
        if (Instance != null && Instance.isLoading)
        {
            Instance.StartCoroutine(Instance.HideLoadingAnimation());
        }
    }
    
    /// <summary>
    /// 静态方法：显示加载动画并执行操作
    /// </summary>
    /// <param name="operation">要执行的操作（协程）</param>
    /// <param name="tipMessage">可选的提示信息</param>
    public static void ShowLoadingWithOperation(IEnumerator operation, string tipMessage = null)
    {
        if (Instance != null && !Instance.isLoading)
        {
            Instance.StartCoroutine(Instance.LoadingWithOperationSequence(operation, tipMessage));
        }
        else if (Instance == null)
        {
            Debug.LogError("ScnLoading实例不存在！请确保场景中有ScnLoading组件。");
        }
    }
    
    /// <summary>
    /// 加载序列协程
    /// </summary>
    IEnumerator LoadingSequence(string sceneName)
    {
        isLoading = true;
        
        // 设置随机提示
        if (tipText != null)
        {
            tipText.text = ScrTips.GetFormattedTips();
        }
        
        // 等待一帧确保界面完全激活
        yield return null;
        
        // 播放开始动画
        if (animator != null)
        {
            animator.Play("loading_start");
        }
        
        // 等待开始动画播放
        yield return new WaitForSeconds(0.5f);
        
        // 开始异步加载目标场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        // 进度更新循环
        float startTime = Time.time;
        bool realLoadComplete = false;
        
        while (true)
        {
            float elapsedTime = Time.time - startTime;
            float realProgress = asyncLoad.progress;
            
            // 检查真实加载是否完成
            if (asyncLoad.progress >= 0.9f && !realLoadComplete)
            {
                realLoadComplete = true;
            }
            
            // 计算显示进度
            float displayProgress = CalculateDisplayProgress(elapsedTime, realProgress, realLoadComplete);
            
            // 更新UI
            UpdateLoadingUI(displayProgress);
            
            // 检查是否可以完成加载
            if (realLoadComplete && elapsedTime >= minimumLoadTime)
            {
                UpdateLoadingUI(1f);
                asyncLoad.allowSceneActivation = true;
                break;
            }
            
            yield return null;
        }
        
        // 等待场景完全激活
        yield return new WaitUntil(() => asyncLoad.isDone);
        
        // 播放结束动画
        if (animator != null)
        {
            animator.Play("loading_end");
        }
        
        // 等待结束时间
        yield return new WaitForSeconds(endWaitTime);
        
        isLoading = false;
    }
    
    /// <summary>
    /// 计算显示进度
    /// </summary>
    float CalculateDisplayProgress(float elapsedTime, float realProgress, bool realLoadComplete)
    {
        if (!realLoadComplete)
        {
            return realProgress; // 显示真实进度
        }
        else
        {
            if (elapsedTime < minimumLoadTime)
            {
                return 0.99f; // 卡在99%等待时间
            }
            else
            {
                return 1f; // 显示100%完成
            }
        }
    }
    
    /// <summary>
    /// 更新加载界面UI
    /// </summary>
    void UpdateLoadingUI(float progress)
    {
        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }
        
        if (progressText != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100);
            progressText.text = percentage + "%";
        }
    }
    
    /// <summary>
    /// 显示加载动画协程（不加载场景）
    /// </summary>
    IEnumerator ShowLoadingAnimation(string tipMessage)
    {
        isLoading = true;
        
        // 设置提示信息
        if (tipText != null)
        {
            tipText.text = string.IsNullOrEmpty(tipMessage) ? ScrTips.GetFormattedTips() : tipMessage;
        }
        
        // 重置进度条
        UpdateLoadingUI(0f);
        
        // 等待一帧确保界面完全激活
        yield return null;
        
        // 播放开始动画
        if (animator != null)
        {
            animator.Play("loading_start");
        }
        
        // 等待开始动画播放
        yield return new WaitForSeconds(0.5f);
        
        // 模拟加载进度（循环显示）
        float progress = 0f;
        while (isLoading)
        {
            progress += Time.deltaTime * 0.3f; // 缓慢增加进度
            if (progress > 0.99f)
            {
                progress = 0.99f; // 保持在99%
            }
            UpdateLoadingUI(progress);
            yield return null;
        }
    }
    
    /// <summary>
    /// 隐藏加载动画协程
    /// </summary>
    IEnumerator HideLoadingAnimation()
    {
        // 显示100%完成
        UpdateLoadingUI(1f);
        
        // 播放结束动画
        if (animator != null)
        {
            animator.Play("loading_end");
        }
        
        // 等待结束时间
        yield return new WaitForSeconds(endWaitTime);
        
        isLoading = false;
    }
    
    /// <summary>
    /// 显示加载动画并执行操作协程
    /// </summary>
    IEnumerator LoadingWithOperationSequence(IEnumerator operation, string tipMessage)
    {
        isLoading = true;
        
        // 设置提示信息
        if (tipText != null)
        {
            tipText.text = string.IsNullOrEmpty(tipMessage) ? ScrTips.GetFormattedTips() : tipMessage;
        }
        
        // 等待一帧确保界面完全激活
        yield return null;
        
        // 播放开始动画
        if (animator != null)
        {
            animator.Play("loading_start");
        }
        
        // 等待开始动画播放
        yield return new WaitForSeconds(0.5f);
        
        // 重置进度条
        UpdateLoadingUI(0f);
        
        // 执行传入的操作
        yield return StartCoroutine(operation);
        
        // 显示100%完成
        UpdateLoadingUI(1f);
        
        // 播放结束动画
        if (animator != null)
        {
            animator.Play("loading_end");
        }
        
        // 等待结束时间
        yield return new WaitForSeconds(endWaitTime);
        
        isLoading = false;
    }
    
    /// <summary>
    /// 播放开始音效（动画事件调用）
    /// </summary>
    public void PlayStartSound()
    {
        // RuntimeManager.PlayOneShot("event:/Loading_start");
    }

    /// <summary>
    /// 播放结束音效（动画事件调用）
    /// </summary>
    public void PlayEndSound()
    {
        // RuntimeManager.PlayOneShot("event:/Loading_end");
    }
}
