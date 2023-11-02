using System;
using Unity.Services.Authentication;
using UnityEngine;
using System.Text;
using Unity.Services.Core;
using System.Threading.Tasks;

#if UNITY_EDITOR
using System.Security.Cryptography;
#endif

public class PlayerWrapper : MonoBehaviour
{
    #region Singleton
    private static PlayerWrapper instance;

    // Static singleton property
    public static PlayerWrapper Instance
    {
        // ajout ET création du composant à un GameObject nommé "SingletonHolder" 
        get { return instance != null ? instance : (instance = new GameObject("PlayerWrapper").AddComponent<PlayerWrapper>()); }
        private set { instance = value; }
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);//le GameObject qui porte ce script ne sera pas détruit
    }

    #endregion

    public LocalPlayer localUser = new("", 0, false, "", PlayerStatus.Menu);

    public void InitPlayer()
    {
        if (string.IsNullOrEmpty(Application.cloudProjectId))
        {
            OnSignInFailed();
            return;
        }

        TrySignIn();
    }



    public async void TrySignIn()
    {
        try
        {
            var unityAuthenticationInitOptions = GenerateAuthenticationOptions(GetProfile());
            await InitializeAndSignInAsync(unityAuthenticationInitOptions);
            OnAuthSignIn();
        }

        catch (Exception)
        {
            OnSignInFailed();
        }
    }
    private void OnAuthSignIn()
    {
        Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");
        localUser.ID.Value = AuthenticationService.Instance.PlayerId;
    }

    private void OnSignInFailed()
    {
        Debug.Log("Sign in failed");
    }

    static string GetProfile()
    {
        var arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] == "-AuthProfile")
            {
                var profileId = arguments[i + 1];
                return profileId;
            }
        }

#if UNITY_EDITOR

        // When running in the Editor make a unique ID from the Application.dataPath.
        // This will work for cloning projects manually, or with Virtual Projects.
        // Since only a single instance of the Editor can be open for a specific
        // dataPath, uniqueness is ensured.
        var hashedBytes = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
        Array.Resize(ref hashedBytes, 16);
        // Authentication service only allows profile names of maximum 30 characters. We're generating a GUID based
        // on the project's path. Truncating the first 30 characters of said GUID string suffices for uniqueness.
        return new Guid(hashedBytes).ToString("N")[..30];
#else
            return "";
#endif
    }


    public InitializationOptions GenerateAuthenticationOptions(string profile)
    {
        try
        {
            var unityAuthenticationInitOptions = new InitializationOptions();
            if (profile.Length > 0)
            {
                unityAuthenticationInitOptions.SetProfile(profile);
            }

            return unityAuthenticationInitOptions;
        }
        catch (Exception e)
        {
            var reason = e.InnerException == null ? e.Message : $"{e.Message} ({e.InnerException.Message})";
            Debug.LogError("Authentication Error " + reason);
            //m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
            throw;
        }
    }

    public async Task InitializeAndSignInAsync(InitializationOptions initializationOptions)
    {
        try
        {
            await UnityServices.InitializeAsync(initializationOptions);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (Exception e)
        {
            var reason = e.InnerException == null ? e.Message : $"{e.Message} ({e.InnerException.Message})";
            Debug.LogError("Authentication Error " + reason);
            //m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
            throw;
        }
    }
}