using UnityEngine;
using Firebase;
using Cysharp.Threading.Tasks;

public class FirebaseInitializer : MonoBehaviour
{

    private static FirebaseInitializer _instance;
    public static FirebaseInitializer Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("FirebaseInitializer");
                _instance = obj.AddComponent<FirebaseInitializer>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    private FirebaseApp firebaseApp;
    public FirebaseApp FirebaseApp => firebaseApp;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        InitializeFirebaseAsync().Forget();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private async UniTaskVoid InitializeFirebaseAsync()
    {
        Debug.Log("Initializing Firebase...");
        try 
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
            if (status == DependencyStatus.Available)
            {
                firebaseApp = FirebaseApp.DefaultInstance;
                isInitialized = true;
                Debug.Log($"Firebase initialized successfully. {firebaseApp.Name}");
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {status}");
                isInitialized = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Firebase initialization failed: {ex.Message}");
            isInitialized = false;
        }
    }

    private async UniTask WaitForInitilazationAsync()
    {
       await UniTask.WaitUntil(() => isInitialized);
    }
}
