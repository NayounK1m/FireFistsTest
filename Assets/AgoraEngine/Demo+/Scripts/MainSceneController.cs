using UnityEngine.UI;
using UnityEngine;
using agora_gaming_rtc;
using System.Collections.Generic;

//말 그대로 메인씬을 컨트롤 하는 스크립트 
public class MainSceneController : MonoBehaviour
{
    static IVideoChatClient app = null;
    private IRtcEngine mRtcEngine;

    readonly List<AgoraNativeBridge.RECT> WinDisplays = new List<AgoraNativeBridge.RECT>();
    int CurrentDisplay = 0;

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
       mRtcEngine = IRtcEngine.GetEngine(AppID);

    }
    
    private void CheckAppId()
    {
        //조건이 false면 메시지를 호출한다.
        Debug.Assert(AppID.Length > 10, "Please fill in your AppId first on Game Controller object.");
        if (AppID.Length > 10) {
            
	        _initialized = true; //초기화 ok
	    }
    }


    //버튼에 따라서 씬을 찾아가는 로직
    public void HandleSceneButtonClick()
    {           
                         //씬이름 초기화 
        string channelName = "please";                            //채널이름 초기화  

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
      
        // load engine
        app.LoadEngine(AppID);
        // join channel and jump to next scene
        app.Join(channelName);
       
        var winDispInfoList = AgoraNativeBridge.GetWinDisplayInfo();
        if (winDispInfoList != null)
        {
            foreach (var dpInfo in winDispInfoList)
            {
                WinDisplays.Add(dpInfo.MonitorInfo.monitor);
            }
        }

        Button button = GameObject.Find("ShareDisplayButton").GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(ShareDisplayScreen);
        }

        button = GameObject.Find("StopShareButton").GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => { mRtcEngine.StopScreenCapture(); });
        }

        GameObject quad = GameObject.Find("DisplayPlane"); //화면공유용 캔버스 따로 만들어서 여기다 넣기.
        if (ReferenceEquals(quad, null))
        {
            Debug.Log("Error: failed to find DisplayPlane");
            return;
        }
        else
        {
            quad.AddComponent<VideoSurface>();
        }
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

     int displayID0or1 = 0;
    void ShareDisplayScreen()
    {
        ScreenCaptureParameters sparams = new ScreenCaptureParameters
        {
            captureMouseCursor = true, //마우스커서까지 화면공유에 포함시키기
            frameRate = 15             //프레임 딜레이
        };

        mRtcEngine.StopScreenCapture(); //스크린캡처 중단.

        ShareWinDisplayScreen(CurrentDisplay); //송출
        CurrentDisplay = (CurrentDisplay + 1) % WinDisplays.Count;
    }

    void ShareWinDisplayScreen(int index)
    {
        var screenRect = new Rectangle
        {
            x = WinDisplays[index].left,
            y = WinDisplays[index].top,
            width = WinDisplays[index].right - WinDisplays[index].left,
            height = WinDisplays[index].bottom - WinDisplays[index].top
        };
        Debug.Log(string.Format(">>>>> Start sharing display {0}: {1} {2} {3} {4}", index, screenRect.x,
            screenRect.y, screenRect.width, screenRect.height));
        var ret = mRtcEngine.StartScreenCaptureByScreenRect(screenRect,
            new Rectangle { x = 0, y = 0, width = 0, height = 0 }, default(ScreenCaptureParameters));
    }

    void TestRectCrop(int order)
    {
        // Assuming you have two display monitors, each of 1920x1080, position left to right:
        Rectangle screenRect = new Rectangle() { x = 0, y = 1080, width = 1920 * 2, height = 1080 };
        Rectangle regionRect = new Rectangle() { x = order * 1920, y = 1080, width = 1920, height = 1080 };

        int rc = mRtcEngine.StartScreenCaptureByScreenRect(screenRect,
            regionRect,
            default(ScreenCaptureParameters)
            );
        if (rc != 0) Debug.LogWarning("rc = " + rc);
    }
}
