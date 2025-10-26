using Cysharp.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class ProfileUI : MonoBehaviour
{

    public GameObject profilePanel;
    public GameObject editNicknamePanel;
    public GameObject createNicknamePanel; 

    public TextMeshProUGUI NicknameText;
    public TextMeshProUGUI UidText;
    public TextMeshProUGUI NowNicknameText;


    private string Nickname => ProfileManager.Instance.CachedProfile?.userName;
    private bool HasNickname => !string.IsNullOrEmpty(Nickname);

    private string UID => AuthManager.Instance.UserId;

    public async UniTaskVoid Start()
    {
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
            return;
        }
        await ProfileManager.Instance.WaitForReadyAsync();

        if (await ProfileManager.Instance.ProfileExistAsync())
        {
            var (profile, err) = await ProfileManager.Instance.LoadProfileAsync();
            if (err != null) Debug.LogWarning(err);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (HasNickname)
        {
            NicknameText.text = $"닉네임 : {Nickname}";
            NowNicknameText.text = $"현재 : {Nickname}";
        }
        else
        {
            NicknameText.text = "닉네임 : ";
            NowNicknameText.text = "현재 : ";
        }
        UidText.text = $"UID : {UID}";
    }

    //private void Awake()
    //{
    //    editNicknameButton.onClick.AddListener(() =>
    //    {
    //        OnEditNicknameButtonClicked().Forget();
    //    });
    //}

    public void SignOut()
    {
        AuthManager.Instance.SignOut();
        editNicknamePanel.SetActive(false);
        createNicknamePanel.SetActive(false);
        profilePanel.SetActive(false);
        var loginUI = FindFirstObjectByType<LoginUI>();
        loginUI.UpdateUI().Forget();
    }

    public void Close(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    public void OpenNicknamePanel()
    {
        if (HasNickname)
        {
            createNicknamePanel.SetActive(false);
            editNicknamePanel.SetActive(true);
        }
        else
        {
            createNicknamePanel.SetActive(true);
            editNicknamePanel.SetActive(false);
        }
    }

    public void OnEditNicknameButtonClicked(TMP_InputField itf)
    {
        _OnEditNicknameButtonClicked(itf).Forget();
    }

    private async UniTaskVoid _OnEditNicknameButtonClicked(TMP_InputField tif)
    {
        string newNickname = tif.text;
        var (success, error) = await ProfileManager.Instance.SaveProfileAsync(newNickname);
        if (success)
        {
            UpdateUI();
        }
        else
        {
            Debug.LogError($"Failed to update nickname: {error}");
        }
    }

}
