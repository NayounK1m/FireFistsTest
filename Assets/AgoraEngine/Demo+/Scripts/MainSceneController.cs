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
    private string AppID = "3ab0f8434af0422fbabfdd0b4e24c506";

    private bool _initialized = false;

    void Start()
    {
        CheckAppId();
    }
    
    private void CheckAppId()
    {
        //조건이 false면 메시지를 호출한다.
        Debug.Assert(AppID.Length > 10, "Please fill in your AppId first on Game Controller object.");
        if (AppID.Length > 10) 
        {
	        _initialized = true; //초기화 ok
	    }
    }


    //버튼에 따라서 씬을 찾아가는 로직
    public void HandleSceneButtonClick()
    {           
        string channelName = "please";                                          //채널이름 초기화  

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
        // load engine
        app.LoadEngine(AppID);
        // join channel and jump to next scene
        app.Join(channelName);

        new DesktopScreenShare().SetupUI();
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
