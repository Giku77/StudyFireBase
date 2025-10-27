using Cysharp.Threading.Tasks;
using Firebase.Database;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager _instance;
    public static ScoreManager Instance => _instance;

    private DatabaseReference scoresRef;

    private int cachedBestScore = 0;
    public int BestScore => cachedBestScore;

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
        scoresRef = FirebaseDatabase.DefaultInstance.RootReference.Child("scores");
        await LoadBestScoreAsync();
        Debug.Log("ScoreManager initialized and ready.");
    }

    private async UniTask LoadBestScoreAsync()
    {
        if (!AuthManager.Instance.IsLoggedIn) return;
        string userId = AuthManager.Instance.UserId;
        try
        {
            DataSnapshot snapshot = await scoresRef.Child(userId).Child("bestScore").GetValueAsync().AsUniTask();
            if (snapshot.Exists && int.TryParse(snapshot.Value.ToString(), out int bestScore))
            {
                cachedBestScore = bestScore;
                Debug.Log($"Loaded best score: {bestScore}");
            }
            else
            {
                cachedBestScore = 0;
                Debug.Log("No existing best score found. Set to 0.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load best score: {ex.Message}");
        }
    }

    public async UniTask<(bool success, string error)> SaveScoreAsync(int newScore)
    {
        if (!AuthManager.Instance.IsLoggedIn) return (false, "User is not logged in.");
        string userId = AuthManager.Instance.UserId;

        try
        {
            Debug.Log($"Saving new best score: {newScore}");

            DatabaseReference historyRef = scoresRef.Child(userId).Child("history");
            DatabaseReference newHistoryRef = historyRef.Push();

            long clientMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var scoreData = new Dictionary<string, object>
            {
                { "uid", userId },
                { "nickname", ProfileManager.Instance.CachedProfile?.userName ?? "Unknown" },
                { "score", newScore },
                { "timestamp", ServerValue.Timestamp },
                { "timestampClient", clientMs } 
            };
            //await newHistoryRef.UpdateChildrenAsync(scoreData).AsUniTask();
            await newHistoryRef.SetValueAsync(scoreData).AsUniTask();

            bool shouldUpdateBestScore = false;
            if (cachedBestScore == 0)
            {
                var bestScoreSnapshot = await scoresRef.Child(userId).Child("bestScore").GetValueAsync().AsUniTask();
                if (bestScoreSnapshot.Exists && int.TryParse(bestScoreSnapshot.Value.ToString(), out int bestScoreFromDb))
                {
                    cachedBestScore = bestScoreFromDb;
                    shouldUpdateBestScore = true;
                }
                else if (newScore > cachedBestScore)
                {
                    cachedBestScore = newScore;
                    shouldUpdateBestScore = true;
                }
            }
            else if (newScore > cachedBestScore)
            {
                cachedBestScore = newScore;
                shouldUpdateBestScore = true;
            }

            if (shouldUpdateBestScore) await UpdateBestScoreAsync(cachedBestScore);

            Debug.Log("Best score saved successfully.");
            return (true, null);
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save best score: {ex.Message}");
            return (false, ex.Message);
        }
    }


    private async UniTask UpdateBestScoreAsync(int newBestScore)
    {
        if (!AuthManager.Instance.IsLoggedIn) return;
        string userId = AuthManager.Instance.UserId;
        try
        {
            await scoresRef.Child(userId).Child("bestScore").SetValueAsync(newBestScore).AsUniTask();
            cachedBestScore = newBestScore;
            await LeaderBoardManager.Instance.SaveBestToLeaderboardAsync(newBestScore);
            Debug.Log($"Updated best score to: {newBestScore}");
        }
        catch (System.Exception ex)
        {
            Debug.LogErrorFormat($"Failed to update best score: {0}", ex.Message);
        }
    }

    public async UniTask<List<ScoreData>> LoadHistoryAsync(int limit = 10)
    {
        List<ScoreData> history = new List<ScoreData>();
        if (!AuthManager.Instance.IsLoggedIn) return history;
        string userId = AuthManager.Instance.UserId;
        try
        {
            Debug.Log("Loading score history...");
            DatabaseReference historyRef = scoresRef.Child(userId).Child("history");
            var byKey = await historyRef.OrderByKey().GetValueAsync().AsUniTask();
            Debug.Log($"byKey count = {byKey.ChildrenCount}");
            foreach (var c in byKey.Children)
            {
                var ts = c.Child("timestamp").Value;
                Debug.Log($"key={c.Key}, tsType={(ts == null ? "null" : ts.GetType().Name)}, tsVal={ts}");
            }

            var byTs = await historyRef.OrderByChild("timestamp").LimitToLast(10).GetValueAsync().AsUniTask();
            Debug.Log($"byTimestamp count = {byTs.ChildrenCount}");
            foreach (var c in byTs.Children)
            {
                var ts = c.Child("timestamp").Value;
                Debug.Log($"(byTs) key={c.Key}, tsType={(ts == null ? "null" : ts.GetType().Name)}, tsVal={ts}");
            }
            Query query = historyRef.OrderByChild("timestamp").LimitToLast(limit);
            DataSnapshot snapshot = await query.GetValueAsync().AsUniTask();
            if (snapshot.ChildrenCount == 0)
            {
                var all = await historyRef.OrderByKey().GetValueAsync().AsUniTask();
                foreach (var c in all.Children) history.Add(ScoreData.FromJson(c.GetRawJsonValue()));
                history = history.OrderByDescending(x => x.timestamp != 0 ? x.timestamp : x.timestampClient).Take(limit).ToList();
            }
            else
            {
                foreach (var c in snapshot.Children) history.Add(ScoreData.FromJson(c.GetRawJsonValue()));
                history.Reverse();
            }
            Debug.Log($"Loaded {history.Count} score history entries.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load score history: {ex.Message}");
        }
        return history;
    }

}
