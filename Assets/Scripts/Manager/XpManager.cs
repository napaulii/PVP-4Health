using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SupabaseModels;
using User = SupabaseModels.User;

public class XpManager : MonoBehaviour
{
    public static XpManager Instance { get; private set; }

    [Header("Security")]
    [SerializeField] private string loginSceneName = "Login";

    [Header("XP Display Labels")]
    [SerializeField] private TextMeshProUGUI[] xpLabels;

    [Header("XP Bar")]
    [SerializeField] private int maxXp = 1000;
    [SerializeField] private float fillSpeed = 2f;
    [SerializeField] private Image fillImage;

    public UnityEvent<int> OnXpChanged = new UnityEvent<int>();
    public int Xp { get; private set; }

    private Coroutine _animCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private async void Start()
    {
        Debug.Log("<color=cyan>[XpManager]</color> Initializing...");
        await RefreshXpFromServer();
    }

    public async Task RefreshXpFromServer()
    {
        try
        {
            var currentUser = SupabaseManager.Instance.Auth.CurrentUser;

            if (currentUser == null)
            {
                Debug.LogError("<color=red>[XpManager]</color> No active user session! Redirecting to Login...");
                SceneManager.LoadScene(loginSceneName);
                return;
            }

            var response = await SupabaseManager.Instance
                .From<User>()
                .Where(u => u.Id == currentUser.Id)
                .Single();

            if (response != null)
            {
                Xp = response.Xp;
                Debug.Log($"<color=yellow>[XpManager]</color> XP synced: {Xp}");
                SetFillImmediate(Xp);
                OnXpChanged.Invoke(Xp);
            }
            else
            {
                Debug.LogWarning("[XpManager] Auth exists but DB profile missing. Redirecting...");
                SceneManager.LoadScene(loginSceneName);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[XpManager] Critical Error: {ex.Message}");
            SceneManager.LoadScene(loginSceneName);
        }
    }

    public async Task AddXp(int amount)
    {
        int newXp = Xp + amount;
        bool success = await UpdateDatabaseXp(newXp);
        if (success)
        {
            Xp = newXp;
            AnimateBarTo(Xp);
            OnXpChanged.Invoke(Xp);
        }
    }

    public async Task<bool> TrySpendXp(int amount)
    {
        if (Xp < amount) return false;

        int newXp = Xp - amount;
        bool success = await UpdateDatabaseXp(newXp);
        if (success)
        {
            Xp = newXp;
            AnimateBarTo(Xp);
            OnXpChanged.Invoke(Xp);
            return true;
        }
        return false;
    }

    private async Task<bool> UpdateDatabaseXp(int newXp)
    {
        try
        {
            var userId = SupabaseManager.Instance.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return false;

            await SupabaseManager.Instance
                .From<User>()
                .Where(u => u.Id == userId)
                .Set(u => u.Xp, newXp)
                .Update();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[XpManager] DB Update Failed: {ex.Message}");
            return false;
        }
    }

    // XpBar

    private void SetFillImmediate(int xp)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01((float)xp / maxXp);
    }

    private void AnimateBarTo(int xp)
    {
        if (fillImage == null) return;

        float target = Mathf.Clamp01((float)xp / maxXp);

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        _animCoroutine = StartCoroutine(AnimateFill(target));
    }

    private IEnumerator AnimateFill(float target)
    {
        while (!Mathf.Approximately(fillImage.fillAmount, target))
        {
            fillImage.fillAmount = Mathf.MoveTowards(
                fillImage.fillAmount,
                target,
                fillSpeed * Time.deltaTime
            );
            yield return null;
        }

        fillImage.fillAmount = target;
    }
}