using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

[ExecuteAlways]
public class AuthManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("Login Fields")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;

    [Header("Register Fields")]
    public TMP_InputField registerUserInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmInput;

    [Header("Feedback")]
    public TMP_Text feedbackText;

    private void Awake()
    {
        AutoAssign();
        WireButtons();
    }

    private void Start()
    {
        LoadCityBackground();
        ShowMain();
    }

    private void LoadCityBackground()
    {
        if (!SceneManager.GetSceneByName("Neon High City").isLoaded)
        {
            SceneManager.LoadSceneAsync("Neon High City", LoadSceneMode.Additive);
        }
    }

    private void AutoAssign()
    {
        var rt = GetComponent<RectTransform>();

        mainPanel = Ensure(mainPanel, rt, "MainPanel");
        loginPanel = Ensure(loginPanel, rt, "LoginPanel");
        registerPanel = Ensure(registerPanel, rt, "RegisterPanel");

        if (loginPanel != null)
        {
            loginEmailInput = Ensure(loginEmailInput, loginPanel.transform, "EmailInput");
            loginPasswordInput = Ensure(loginPasswordInput, loginPanel.transform, "PasswordInput");
        }

        if (registerPanel != null)
        {
            registerUserInput = Ensure(registerUserInput, registerPanel.transform, "UserInput");
            registerEmailInput = Ensure(registerEmailInput, registerPanel.transform, "EmailInput");
            registerPasswordInput = Ensure(registerPasswordInput, registerPanel.transform, "PasswordInput");
            registerConfirmInput = Ensure(registerConfirmInput, registerPanel.transform, "ConfirmInput");
        }

        feedbackText = Ensure(feedbackText, rt, "FeedbackText");
    }

    private T Ensure<T>(T field, Transform parent, string childName) where T : Component
    {
        if (field != null) return field;
        var child = parent.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private GameObject Ensure(GameObject field, Transform parent, string childName)
    {
        if (field != null) return field;
        var child = parent.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private void WireButtons()
    {
        if (mainPanel != null)
        {
            WireButton(mainPanel, "BtnLogin", ShowLogin);
            WireButton(mainPanel, "BtnRegister", ShowRegister);
        }

        if (loginPanel != null)
        {
            WireButton(loginPanel, "BtnSubmit", OnLoginSubmit);
            WireButton(loginPanel, "BtnBack", ShowMain);
        }

        if (registerPanel != null)
        {
            WireButton(registerPanel, "BtnSubmit", OnRegisterSubmit);
            WireButton(registerPanel, "BtnBack", ShowMain);
        }
    }

    private void WireButton(GameObject panel, string childName, UnityEngine.Events.UnityAction action)
    {
        var btn = panel.transform.Find(childName)?.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }
    }

    public void ShowMain()
    {
        mainPanel.SetActive(true);
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        ClearFeedback();
    }

    public void ShowLogin()
    {
        mainPanel.SetActive(false);
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        ClearFeedback();
    }

    public void ShowRegister()
    {
        mainPanel.SetActive(false);
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        ClearFeedback();
    }

    public void OnLoginSubmit()
    {
        string email = loginEmailInput?.text.Trim();
        string password = loginPasswordInput?.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowFeedback("Completa todos los campos");
            return;
        }

        ShowFeedback("Iniciando sesión...");
        Debug.Log($"Login attempt: {email}");
    }

    public void OnRegisterSubmit()
    {
        string user = registerUserInput?.text.Trim();
        string email = registerEmailInput?.text.Trim();
        string password = registerPasswordInput?.text.Trim();
        string confirm = registerConfirmInput?.text.Trim();

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
        {
            ShowFeedback("Completa todos los campos");
            return;
        }

        if (password != confirm)
        {
            ShowFeedback("Las contraseñas no coinciden");
            return;
        }

        if (password.Length < 6)
        {
            ShowFeedback("La contraseña debe tener al menos 6 caracteres");
            return;
        }

        ShowFeedback("Registrando usuario...");
        Debug.Log($"Register attempt: {user} - {email}");
    }

    private void ShowFeedback(string msg)
    {
        if (feedbackText != null)
            feedbackText.text = msg;
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }
}
