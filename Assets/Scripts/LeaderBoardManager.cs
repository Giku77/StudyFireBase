using Cysharp.Threading.Tasks;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine;

public class LeaderBoardManager : MonoBehaviour
{
    private static LeaderBoardManager _instance;
    public static LeaderBoardManager Instance => _instance;

    private DatabaseReference leaderBoardRef;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private async UniTaskVoid Start()
    {
        await FirebaseInitializer.Instance.WaitForInitilazationAsync();
        leaderBoardRef = FirebaseDatabase.DefaultInstance.RootReference.Child("leaderboard");
        Debug.Log("ScoreManager initialized and ready.");
    }

    public async UniTask<(bool success, string error)> SaveScoreAsync(int newScore)
    {
        if (!AuthManager.Instance.IsLoggedIn) return (false, "User is not logged in.");
        string userId = AuthManager.Instance.UserId;

        try
        {
            Debug.Log($"Saving new LeaderBoard score: {newScore}");

            DatabaseReference UIDRef = leaderBoardRef.Child(userId);
            DatabaseReference newUIDRef = UIDRef.Push();

            long clientMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var scoreData = new Dictionary<string, object>
            {
                { "uid", userId },
                { "nickname", ProfileManager.Instance.CachedProfile?.userName ?? "Unknown" },
                { "score", newScore },
                { "timestamp", ServerValue.Timestamp },
                { "timestampClient", clientMs }
            };
            //await newUIDRef.UpdateChildrenAsync(scoreData).AsUniTask();
            await newUIDRef.SetValueAsync(scoreData).AsUniTask();

            Debug.Log("LeaderBoard score saved successfully.");
            return (true, null);
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save LeaderBoard score: {ex.Message}");
            return (false, ex.Message);
        }
    }

    public async UniTask SaveBestToLeaderboardAsync(int newScore)
    {
        var uid = AuthManager.Instance.UserId;
        var name = ProfileManager.Instance.CachedProfile?.userName ?? "Unknown";
        var bestRef = FirebaseDatabase.DefaultInstance.RootReference
            .Child("leaderboard").Child(uid);

        await bestRef.RunTransaction(mutable =>
        {
            if (mutable.Value is Dictionary<string, object> cur &&
                cur.TryGetValue("score", out var s) && s is long curScore &&
                curScore >= newScore)
            {
                return TransactionResult.Success(mutable); 
            }

            long clientMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            mutable.Value = new Dictionary<string, object>
            {
                ["uid"] = uid,
                ["nickname"] = name,
                ["score"] = newScore,
                ["timestamp"] = ServerValue.Timestamp,
                ["timestampClient"] = clientMs
            };
            return TransactionResult.Success(mutable);
        }).AsUniTask();
    }

    public async UniTask<List<ScoreData>> LoadTopAsync(int topN = 10)
    {
        var refRoot = FirebaseDatabase.DefaultInstance.RootReference.Child("leaderboard");
        var snap = await refRoot.OrderByChild("score").LimitToLast(topN).GetValueAsync().AsUniTask();

        var list = new List<ScoreData>();
        foreach (var c in snap.Children)
            list.Add(ScoreData.FromJson(c.GetRawJsonValue()));
        list.Reverse();
        return list;
    }

}
