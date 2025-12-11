using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader I { get; private set; }

    [Header("Fade")]
    public Image fadeImage;
    public float fadeDuration = 0.5f;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage)
        {
            fadeImage.color = new Color(0, 0, 0, 1);
            fadeImage.raycastTarget = false; // ✅ 기본적으로 클릭 막지 않게
        }
    }

    void Start()
    {
        if (fadeImage)
        {
            fadeImage.raycastTarget = false;
            StartCoroutine(Fade(1f, 0f));
        }
    }

    public void LoadSceneWithFade(string sceneName)
    {
        if (!fadeImage) { SceneManager.LoadScene(sceneName); return; }
        StartCoroutine(CoLoad(sceneName));
    }

    IEnumerator CoLoad(string sceneName)
    {
        if (fadeImage) fadeImage.raycastTarget = true;  // ✅ 페이드 중에는 막음
        yield return Fade(0f, 1f);
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
        yield return new WaitForEndOfFrame();
        yield return Fade(1f, 0f);
        if (fadeImage) fadeImage.raycastTarget = false; // ✅ 완료 후 클릭 가능
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;

            // ★ fadeImage가 삭제되었거나 null이면 즉시 종료
            if (fadeImage == null)
                yield break;

            float a = Mathf.Lerp(from, to, t / fadeDuration);
            var c = fadeImage.color;
            fadeImage.color = new Color(c.r, c.g, c.b, a);

            yield return null;
        }

        if (fadeImage != null)
        {
            var c = fadeImage.color;
            fadeImage.color = new Color(c.r, c.g, c.b, to);
        }
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (fadeImage == null)
        {
            fadeImage = FindAnyObjectByType<Image>(); // 혹은 특정 경로로 찾기
        }
    }
}
