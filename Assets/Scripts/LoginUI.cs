using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField emailInputField;
    public TMP_InputField passwordInputField;

    public Button loginButton;
    public Button guestloginButton;
    public Button registerButton;

    public async UniTaskVoid Awake()
    {
        var authManager = AuthManager.Instance;
        var email = emailInputField.text;
        var password = passwordInputField.text;
        loginButton.onClick.AddListener(async () =>
        {
            var s = authManager.SignInWithEmailAsync(email, password);
        });
        guestloginButton.onClick.AddListener(() =>
        {
            var s = authManager.SignInAnonymouslyAsync();
        });
        registerButton.onClick.AddListener(async () =>
        {
            var s = authManager.CreateUserWithEmailAsync(email, password);
        });
    }
}
