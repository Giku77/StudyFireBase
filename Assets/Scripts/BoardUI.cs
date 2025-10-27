using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class BoardUI : MonoBehaviour
{

    public GameObject myScorePanel;
    public GameObject leaderBoardPanel;

    public GameObject LeaderBoardUIObject;
    public GameObject MyScoreBoardUIObject;

    public Transform LeaderBoardContentTransform;
    public Transform MyScoreBoardContentTransform;

    public TextMeshProUGUI MyScoreStatusText;
    public TextMeshProUGUI LeaderBoardStatusText;

    private List<GameObject> LeaderBoardItems = new List<GameObject>();
    private List<GameObject> MyScoreBoardItems = new List<GameObject>();
    //public TextMeshProUGUI UidText;
    //public TextMeshProUGUI NowNicknameText;

    private DatabaseReference myRef;
    private DatabaseReference readerRef;

    public Toggle LeaderBoardAutoRefreshToggle;

    private List<ScoreData> scoreHistory = new List<ScoreData>();

    private async UniTaskVoid Start()
    {
        await FirebaseInitializer.Instance.WaitForInitilazationAsync();
        Debug.Log("BoardUI initialized and ready.");
        var uid = AuthManager.Instance.UserId;
        myRef = FirebaseDatabase.DefaultInstance.RootReference.Child("scores").Child(uid).Child("history");
        myRef.ValueChanged += OnValueChanged;
        readerRef = FirebaseDatabase.DefaultInstance.RootReference.Child("leaderboard");
        readerRef.ValueChanged += OnValueChanged;
        LeaderBoardAutoRefreshToggle.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                readerRef.ValueChanged += OnValueChanged;
            }
            else
            {
                readerRef.ValueChanged -= OnValueChanged;
            }
        });
    }

    private void OnDestroy()
    {
        if (myRef != null)
        {
            myRef.ValueChanged -= OnValueChanged;
        }
        if (readerRef != null && LeaderBoardAutoRefreshToggle.isOn)
        {
            readerRef.ValueChanged -= OnValueChanged;
        }
    }

    private bool refreshing = false;
    private async void OnValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (refreshing) return;
        refreshing = true;
        await Init();
        refreshing = false;
    }

    public async void OnRefreshButton()
    {
        if (refreshing) return;
        refreshing = true;
        await Init();
        refreshing = false;
    }
    private async UniTask Init()
    {
        await FirebaseInitializer.Instance.WaitForInitilazationAsync();
        if (myScorePanel.activeSelf)
        {
            MyScoreStatusText.text = "로딩중...";
            await UpdateMyScoreBoard();
            MyScoreStatusText.text = "로딩 완료";
        }
        else if (leaderBoardPanel.activeSelf)
        {
            LeaderBoardStatusText.text = "로딩중...";
            await UpdateReaderBoard();
            LeaderBoardStatusText.text = "로딩 완료";
        }
    }

    private async UniTask UpdateReaderBoard()
    {
        scoreHistory = await LeaderBoardManager.Instance.LoadTopAsync();
        foreach (var item in LeaderBoardItems)
        {
            Destroy(item);
        }
        for (int i = 0; i < scoreHistory.Count; i++)
        {
            var item = scoreHistory[i];
            var l = Instantiate(LeaderBoardUIObject, LeaderBoardContentTransform);
            LeaderBoardItems.Add(l);
            var t = l.GetComponentInChildren<TextMeshProUGUI>();
            t.text = $"<color=#FFD400>{i + 1}위</color> : {item.nickname} {item.score}점";
        }
    }

    private async UniTask UpdateMyScoreBoard()
    {
        scoreHistory = await ScoreManager.Instance.LoadHistoryAsync();
        var uid = AuthManager.Instance.UserId;
        foreach (var item in MyScoreBoardItems)
        {
            Destroy(item);
        }
        for (int i = 0; i < scoreHistory.Count; i++)
        {
            var item = scoreHistory[i];
           if (item.uid != uid) continue;

            var l = Instantiate(MyScoreBoardUIObject, MyScoreBoardContentTransform);
            MyScoreBoardItems.Add(l);
            var t = l.GetComponentInChildren<TextMeshProUGUI>();
            t.text = $"<color=#FFD400>{i + 1}</color>         {item.score}점";
        }
    }

    public void Close(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    private async UniTask OpenPanelAsync(GameObject go)
    {
        go.SetActive(true);
        if (go == myScorePanel) leaderBoardPanel.SetActive(false);
        else if (go == leaderBoardPanel) myScorePanel.SetActive(false);

        await Init();
    }

    public void OpenPanel(GameObject go)
    {
        OpenPanelAsync(go).Forget();
    }


}
