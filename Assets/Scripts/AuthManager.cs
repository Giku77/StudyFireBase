using Cysharp.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;

public class AuthManager : MonoBehaviour
{

    private static AuthManager _instance;
    public static AuthManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("AuthManager");
                _instance = obj.AddComponent<AuthManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    private FirebaseAuth auth;
    private FirebaseUser user;
    public FirebaseUser User => user;

    public bool IsLoggedIn => user != null; 

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    public string UserId => user != null ? user.UserId : string.Empty;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (_instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
    }
    public async UniTaskVoid Start()
    {
        await FirebaseInitializer.Instance.WaitForInitilazationAsync();
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += OnAuthStateChanger;
        user = auth.CurrentUser;

        if (user != null)
        {
            Debug.Log($"User is signed in: {user.UserId}");
        }
        else
        {
            Debug.Log("No user is signed in.");
        }
        isInitialized = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            auth.StateChanged -= OnAuthStateChanger;
            _instance = null;
        }
    }

    public async UniTask<(bool success, string error)> SignInAnonymouslyAsync()
    {
        try 
        { 
            Debug.Log("User signed in anonymously.");
            AuthResult result = await auth.SignInAnonymouslyAsync().AsUniTask();
            user = result.User;
            Debug.Log($"Anonymous user signed in: {user.UserId}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Anonymous sign-in failed: {ex.Message}");
            return (false, ex.Message);
        }

        return (true, "");
    }

    public async UniTask<(bool success, string error)> CreateUserWithEmailAsync(string e, string p)
    {
        try 
        {
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(e, p).AsUniTask();
            user = result.User;
            Debug.Log("User created with email.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"User creation failed: {ex.Message}");
            return (false, ex.Message);
        }
        return (true, "");
    }

    public async UniTask<(bool success, string error)> SignInWithEmailAsync(string e, string p)
    {
        try 
        {
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(e, p).AsUniTask();
            user = result.User;
            Debug.Log("User signed in with email.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Email sign-in failed: {ex.Message}");
            return (false, ex.Message);
        }
        return (true, "");
    }

    public void SignOut()
    {
        if(auth != null && user != null)
        {
            Debug.Log("User signed out.");
            auth.SignOut();
            user = null;
        }
    }

    private string ParseFirebaseError(string ex)
    {
        return ex;
    }

    private void OnAuthStateChanger(object sender, System.EventArgs e)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("User signed out: " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("User signed in: " + user.UserId);
            }
        }
    }

}
