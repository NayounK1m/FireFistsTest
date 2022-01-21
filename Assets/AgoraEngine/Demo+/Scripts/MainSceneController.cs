using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using agora_gaming_rtc;

//말 그대로 메인씬을 컨트롤 하는 스크립트 
public class MainSceneController : MonoBehaviour
{
    static IVideoChatClient app = null;

    private string HomeSceneName = "MainScene";

    // PLEASE KEEP THIS App ID IN SAFE PLACE
    // Get your own App ID at https://dashboard.agora.io/
    [Header("Agora Properties")]
    [SerializeField]
    private string AppID = "";

    [Header("UI Controls")]
    [SerializeField]
    private InputField channelInputField;   //채널 입력하는 input
    [SerializeField]
    private Text appIDText;                 //AppID text

    private bool _initialized = false;

    void Awake()
    {
        // keep this alive across scenes
        DontDestroyOnLoad(this.gameObject);
        channelInputField = GameObject.Find("ChannelName").GetComponent<InputField>();  //채널 이름 받아오기
    }

    void Start()
    {
        CheckAppId();
        LoadLastChannel();
    }
    
    private void CheckAppId()
    {
        //조건이 false면 메시지를 호출한다.
        Debug.Assert(AppID.Length > 10, "Please fill in your AppId first on Game Controller object.");
        if (AppID.Length > 10) {
            SetAppIdText();
	        _initialized = true; //초기화 ok
	    }
    }

    //앱 아이디를 나타내는 메서드
    void SetAppIdText()
    { 
        appIDText.text = "AppID:" + AppID.Substring(0, 4) + "********" + AppID.Substring(AppID.Length - 4, 4);
    }

    //채널 이름을 가져온다
    private void LoadLastChannel()
    {
        string channel = PlayerPrefs.GetString("ChannelName");  //채널 이름 가져오기
        if (!string.IsNullOrEmpty(channel)) //비어있으면 
        {
            GameObject go = GameObject.Find("ChannelName");
            InputField field = go.GetComponent<InputField>();

            field.text = channel;           //이름 채워주기
        }
    }

    //채널 이름 저장
    private void SaveChannelName()
    {
        if (!string.IsNullOrEmpty(channelInputField.text))
        {
            PlayerPrefs.SetString("ChannelName", channelInputField.text);
            PlayerPrefs.Save();
        }
    }

    //버튼에 따라서 씬을 찾아가는 로직
    public void HandleSceneButtonClick()
    {           
        string sceneFileName = "DesktopScreenShareScene";                       //씬이름 초기화 
        string channelName = channelInputField.text;                            //채널이름 초기화  

        //채널이름 체크
        if (string.IsNullOrEmpty(channelName))  
        {
            Debug.LogError("Channel name can not be empty!");
            return;
        }
        //AppID 체크 
        if (!_initialized)
        {
            Debug.LogError("AppID null or app is not initialized properly!");
            return;
        }

        // create app
        app = new DesktopScreenShare(); 

        if (app == null) return;

        //조건이 null이면 종료 콜백 연결
        app.OnViewControllerFinish += OnViewControllerFinish;
        // load engine
        app.LoadEngine(AppID);
        // join channel and jump to next scene
        app.Join(channelName);
        SaveChannelName();
        SceneManager.sceneLoaded += OnLevelFinishedLoading; // configure GameObject after scene is loaded
        SceneManager.LoadScene(sceneFileName, LoadSceneMode.Single);
    }

    public void OnViewControllerFinish()
    {
        if (!ReferenceEquals(app, null))
        {
            app = null; // delete app
            SceneManager.LoadScene(HomeSceneName, LoadSceneMode.Single);    //LoadSceneMode.Single : 기존 로드된 모든 씬을 종료하고 지정한 씬을 로드한다. 
        }
        Destroy(gameObject);
    }

    // configure GameObject after scene is loaded
    public void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (!ReferenceEquals(app, null))
        {
            app.OnSceneLoaded(); // call this after scene is loaded
        }
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    void OnApplicationPause(bool paused)
    {
        if (!ReferenceEquals(app, null))
        {
            app.EnableVideo(paused);
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit, clean up...");
        
        if (!ReferenceEquals(app, null))
        {
            app.UnloadEngine();
        }
        IRtcEngine.Destroy();
    }

    /// <param name="engine">Video Engine </param>
    void CheckDevices(IRtcEngine engine)
    {
        VideoDeviceManager deviceManager = VideoDeviceManager.GetInstance(engine);
        deviceManager.CreateAVideoDeviceManager();

        int cnt = deviceManager.GetVideoDeviceCount();
        Debug.Log("Device count =============== " + cnt);
    }
}
