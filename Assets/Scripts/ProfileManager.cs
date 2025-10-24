using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    private static ProfileManager _instance;
    public static ProfileManager Instance => _instance;

    private DatabaseReference _databaseReference;
    private DatabaseReference _userProfilesRef;
    private UserProfile _cachedProfile;
    public UserProfile CachedProfile => _cachedProfile;

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
        _databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        _userProfilesRef = _databaseReference.Child("users");

        Debug.Log("ProfileManager initialized and ready.");
    }

    public async UniTask<(bool success, string error)> SaveProfileAsync(string nickname)
    {
        if (!AuthManager.Instance.IsLoggedIn) return (false, "User is not logged in.");
        string userId = AuthManager.Instance.UserId;
        string email = AuthManager.Instance.User.Email ?? "_";

        try
        {
            Debug.Log($"Saving profile... {nickname}");
            UserProfile profile = new UserProfile(nickname, email);
            string json = profile.ToJson();
            await _userProfilesRef.Child(userId).SetRawJsonValueAsync(json).AsUniTask();
            _cachedProfile = profile;
            Debug.Log("Profile saved successfully.");
            return (true, null);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save profile: {ex.Message}");
            return (false, ex.Message);
        }
    }

    public async UniTask<(UserProfile profile, string error)> LoadProfileAsync(string nickname)
    {
        if (!AuthManager.Instance.IsLoggedIn) return (null, "User is not logged in.");
        string userId = AuthManager.Instance.UserId;

        try
        {
            Debug.Log($"Loading profile for userId: {userId}");
            DataSnapshot snapshot = await _userProfilesRef.Child(userId).GetValueAsync().AsUniTask();

            if (!snapshot.Exists)
            {
                Debug.LogWarning("Profile does not exist.");
                return (null, "Profile does not exist.");
            }
            string json = snapshot.GetRawJsonValue();
            UserProfile profile = UserProfile.FromJson(json);
            _cachedProfile = profile;
            Debug.Log("Profile loaded successfully.");
            return (profile, null);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save profile: {ex.Message}");
            return (null, ex.Message);
        }
    }

    public async UniTask<(bool success, string error)> UpdateNicknameAsync(string nickname)
    {
        if (!AuthManager.Instance.IsLoggedIn) return (false, "User is not logged in.");
        string userId = AuthManager.Instance.UserId;
        try
        {
           Debug.Log($"Updating nickname to: {nickname}");
            await _userProfilesRef.Child(userId).Child("nickname").SetValueAsync(nickname).AsUniTask();
            if (_cachedProfile != null)
            {
                _cachedProfile.userName = nickname;
            }
            Debug.Log("Nickname updated successfully.");
            return (true, null);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update nickname: {ex.Message}");
            return (false, ex.Message);
        }
    }

    public async UniTask<bool> ProfileExistAsync()
    {
        if (!AuthManager.Instance.IsLoggedIn) return false;
        string userId = AuthManager.Instance.UserId;
        try
        {
            Debug.Log($"Checking if profile exists for userId: {userId}");
            DataSnapshot snapshot = await _userProfilesRef.Child(userId).GetValueAsync().AsUniTask();
            bool exists = snapshot.Exists;
            Debug.Log($"Profile existence check: {exists}");
            return exists;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to check profile existence: {ex.Message}");
            return false;
        }
    }
}
