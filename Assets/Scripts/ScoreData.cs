using System;
using UnityEngine;

[Serializable]
public class ScoreData
{
    public int score;
    public long timestamp;
    public long timestampClient;
    public string uid;
    public string nickname;

    public ScoreData() { }

    public ScoreData(string uid, string nickname, int score, long stp, long timestampClient)
    {
        this.uid = uid;
        this.nickname = nickname;
        this.score = score;
        this.timestamp = stp;
        this.timestampClient = timestampClient;
        //timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public DateTime GetDateTime()
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;
    }

    public string GetDateString()
    {
        return GetDateTime().ToString("yyyy/MM/dd HH:mm:ss");
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    public static ScoreData FromJson(string json)
    {
        return JsonUtility.FromJson<ScoreData>(json);
    }

}
