using UnityEngine;
using System;

[Serializable]
public class UserProfile
{
    public string userName;
    public string email;
    public long createAt;

    public UserProfile() { }
    public UserProfile(string userName, string email)
    {
        this.userName = userName;
        this.email = email;
        createAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static UserProfile FromJson(string json)
    {
        return JsonUtility.FromJson<UserProfile>(json);
    }
}
