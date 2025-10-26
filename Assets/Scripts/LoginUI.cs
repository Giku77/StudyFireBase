using Cysharp.Threading.Tasks;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField emailInputField;
    public TMP_InputField passwordInputField;

    public Button loginButton;
    public Button guestloginButton;
    public Button registerButton;

    public Button profileButton;
    private TextMeshProUGUI profileButtonText;

    public GameObject LoginPanel;
    public GameObject ProfilePanel;

    public TextMeshProUGUI statusText;

    private string email;
    private string password;

    public async UniTaskVoid Start()
    {
        SetButtonInteractable(false);
        var destroyToken = this.GetCancellationTokenOnDestroy();
        var waitInit = UniTask.WaitUntil(
            () => AuthManager.Instance != null && AuthManager.Instance.IsInitialized,
            cancellationToken: destroyToken
        );
        var timeout = UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: destroyToken);

        var s = await UniTask.WhenAny(waitInit, timeout);
        if (s == 1) 
        {
            Debug.LogWarning("AuthManager init timeout");
            SetButtonInteractable(true);
            return;
        }
        profileButtonText = profileButton.GetComponentInChildren<TextMeshProUGUI>();
        SetButtonInteractable(true);
        var authManager = AuthManager.Instance;
        emailInputField.onValueChanged.AddListener((value) =>
        {
            email = value;
        });
        passwordInputField.onValueChanged.AddListener((value) =>
        {
            password = value;
        });
        loginButton.onClick.AddListener(() =>
        {
            OnLoginButtonClicked(email, password).Forget();
        });
        guestloginButton.onClick.AddListener(() =>
        {
            OnGeustLoginButtonClicked().Forget();
        });
        registerButton.onClick.AddListener(() =>
        {
            OnRegisterButtonClicked(email, password).Forget();
        });
        profileButton.onClick.AddListener(() =>
        {
            ProfilePanel.SetActive(!ProfilePanel.activeSelf);
        });
        UpdateUI().Forget();
    }

    public async UniTaskVoid UpdateUI()
    {
        if (AuthManager.Instance == null || !AuthManager.Instance.IsInitialized) return;
        bool isLoggedIn = AuthManager.Instance.IsLoggedIn;
        LoginPanel.SetActive(!isLoggedIn);
        profileButton.gameObject.SetActive(isLoggedIn);
        //profileButtonText.text = AuthManager.Instance.UserId;
    }

    private async UniTaskVoid OnLoginButtonClicked(string e, string p)
    {
        SetButtonInteractable(false);
        var authManager = AuthManager.Instance;
        var s = await authManager.SignInWithEmailAsync(e, p);
        if (!s.success)
        {
            SetStatusText($"Login Failed: {s.error}");
        }
        else UpdateUI().Forget();
        SetButtonInteractable(true);
    }
    private async UniTaskVoid OnRegisterButtonClicked(string e, string p)
    {
        SetButtonInteractable(false);
        var authManager = AuthManager.Instance;
        var s = await authManager.CreateUserWithEmailAsync(e, p);
        if (!s.success)
        {
            SetStatusText($"Register Failed: {s.error}");
        }
        else UpdateUI().Forget();
        SetButtonInteractable(true);
    }
    private async UniTaskVoid OnGeustLoginButtonClicked()
    {
        SetButtonInteractable(false);
        var authManager = AuthManager.Instance;
        var s = await authManager.SignInAnonymouslyAsync();
        if (!s.success)
        {
            SetStatusText($"Guest Login Failed: {s.error}");
        }
        else UpdateUI().Forget();
        SetButtonInteractable(true);
    }

    private void SetButtonInteractable(bool t)
    {
        loginButton.interactable = t;
        guestloginButton.interactable = t;
        registerButton.interactable = t;
    }

    private void SetStatusText(string str)
    {
        statusText.text = str;
    }
}
