using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using agora_gaming_rtc;
using agora_utilities;

//말 그대로 메인씬을 컨트롤 하는 스크립트 
public class MainSceneController : MonoBehaviour
{
    //static IVideoChatClient app = null;
    //캠 기능
    private IRtcEngine mRtcEngine;

    readonly List<AgoraNativeBridge.RECT> WinDisplays = new List<AgoraNativeBridge.RECT>();
    int CurrentDisplay = 0;

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
        string channelName = "please";                              //채널이름 초기화  

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

        // load engine
        LoadEngine(AppID);
        // join channel and jump to next scene
        Join(channelName);
       
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

    void OnApplicationPause(bool paused)  {}

    void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit, clean up...");
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

    #region test
    //동기화 끝내는 이벤트
    public event Action OnViewControllerFinish;
    //uint == 유저아이디 식별, VideoSurface == 영상 동기화 기능 
    protected Dictionary<uint, VideoSurface> UserVideoDict = new Dictionary<uint, VideoSurface>();
    //추측 : 본인 캠 식별용 유저아이디가 들어가는거같음 (동기화할때 보여지는거)
    protected const string SelfVideoName = "MyView";
    //채널이름
    protected string mChannel;
    //영상 화질
    protected bool _enforcing360p = false; // the local view of the remote user resolution

    /// <param name="channel"></param>
    public void Join(string channel)
    {
        Debug.Log("calling join (channel = " + channel + ")");

        if (mRtcEngine == null)
            return;

        mChannel = channel;

        // set callbacks (optional)
        mRtcEngine.OnJoinChannelSuccess = OnJoinChannelSuccess;
        mRtcEngine.OnUserJoined = OnUserJoined;
        mRtcEngine.OnUserOffline = OnUserOffline;
        mRtcEngine.OnVideoSizeChanged = OnVideoSizeChanged;
        // 캠 연결
        PrepareToJoin();

        // join channel
        mRtcEngine.JoinChannel(channel, null, 0);

        Debug.Log("initializeEngine done");
    }

    //캠 연결
    protected virtual void PrepareToJoin()
    {
        // enable video
        mRtcEngine.EnableVideo();
        // allow camera output callback
        mRtcEngine.EnableVideoObserver();
    }

    //RTC 채널 떠나기
    public virtual void Leave()
    {
        Debug.Log("calling leave");

        if (mRtcEngine == null)
            return;

        // leave channel
        mRtcEngine.LeaveChannel();
        // 비디오 탐색을 중지
        mRtcEngine.DisableVideoObserver();
    }

    //마이크 뮤트 기능(버튼 값 가져오기)
    protected bool MicMuted { get; set; }

    protected virtual void SetupUI()
    {
        GameObject go = GameObject.Find(SelfVideoName);
        if (go != null)
        {
            UserVideoDict[0] = go.AddComponent<VideoSurface>(); //컴포넌트에 캠 동기화 스크립트 추가
        }
        //떠나기 버튼
        Button button = GameObject.Find("LeaveButton").GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnLeaveButtonClicked);
        }
        //마이크 뮤트 버튼
        Button mutton = GameObject.Find("MuteButton").GetComponent<Button>();
        if (mutton != null)
        {
            mutton.onClick.AddListener(() =>
            {
                MicMuted = !MicMuted;
                mRtcEngine.EnableLocalAudio(!MicMuted);
                mRtcEngine.MuteLocalAudioStream(MicMuted);
                Text text = mutton.GetComponentInChildren<Text>();
                text.text = MicMuted ? "Unmute" : "Mute";
            });
        }
        //사용자의 스크린값 변경될때마다 가져와서 거기에 맞게 화질 조정
        go = GameObject.Find("ToggleScale");
        if (go != null)
        {
            Toggle toggle = go.GetComponent<Toggle>();
            _enforcing360p = toggle.isOn; // initial value
            toggle.onValueChanged.AddListener((val) =>
            {
                _enforcing360p = val;
            });
        }
    }
    //떠나기 버튼 함수
    protected void OnLeaveButtonClicked()
    {
        Leave();
        UnloadEngine();

        if (OnViewControllerFinish != null)
        {
            OnViewControllerFinish();
        }
    }

    //캠 크기 변경되었을때
    protected virtual void OnVideoSizeChanged(uint uid, int width, int height, int rotation)
    {
        Debug.LogWarningFormat("uid:{3} OnVideoSizeChanged width = {0} height = {1} for rotation:{2}", 150, 100, rotation, uid);

        if (UserVideoDict.ContainsKey(uid))
        {
            GameObject go = UserVideoDict[uid].gameObject;
            Vector2 v2 = new Vector2(150, 100); //우리가 사이즈 바꾼거임...
            //RawImage를 통해 캠이 송출이 되는 것, 그 컴포넌트를 가져옴...
            RawImage image = go.GetComponent<RawImage>();
            if (_enforcing360p)
            {
                //화질에 맞게 스케일 가져오기
                v2 = AgoraUIUtils.GetScaledDimension(150, 100, 240f); //우리가 사이즈 바꾼거임...
            }
            //로테이션 값
            if (IsPortraitOrientation(rotation))
            {
                v2 = new Vector2(v2.y, v2.x);
            }
            image.rectTransform.sizeDelta = v2;
        }
    }

    bool IsPortraitOrientation(int rotation)
    {
        return rotation == 90 || rotation == 270;
    }

    /// <param name="appId">Get the APP ID from Agora account</param>

    //아고라 엔진 불러오기
    public void LoadEngine(string appId)
    {
        //아고라 AppID가 부여된 엔진을 가져옴
        mRtcEngine = IRtcEngine.GetEngine(appId);

        //디버깅
        mRtcEngine.OnError = (code, msg) =>
        {
            Debug.LogErrorFormat("RTC Error:{0}, msg:{1}", code, IRtcEngine.GetErrorDescription(code));
        };

        mRtcEngine.OnWarning = (code, msg) =>
        {
            Debug.LogWarningFormat("RTC Warning:{0}, msg:{1}", code, IRtcEngine.GetErrorDescription(code));
        };

        // mRtcEngine.SetLogFile(logFilepath);
        // enable log
        mRtcEngine.SetLogFilter(LOG_FILTER.DEBUG | LOG_FILTER.INFO | LOG_FILTER.WARNING | LOG_FILTER.ERROR | LOG_FILTER.CRITICAL);
    }

    // 아고라 엔진 끄기
    public virtual void UnloadEngine()
    {
        Debug.Log("calling unloadEngine");

        // delete
        if (mRtcEngine != null)
        {
            IRtcEngine.Destroy();  //엔진 Destroy
            mRtcEngine = null;
        }
    }


    /// <param name="pauseVideo"></param>
    //비디오 키고
    public void EnableVideo(bool pauseVideo)
    {
        if (mRtcEngine != null)
        {
            if (!pauseVideo) //만약, 비디오가 안멈췄으면
            {
                //비디오 켜주고
                mRtcEngine.EnableVideo();
            }
            else
            {   //비디오 꺼져
                mRtcEngine.DisableVideo();
            }
        }
    }

    public virtual void OnSceneLoaded()
    {
        //씬이 불러와지면, UI를 알아서 설정해줌
        SetupUI();
    }

    // implement engine callbacks
    protected virtual void OnJoinChannelSuccess(string channelName, uint uid, int elapsed)
    {
        Debug.Log("JoinChannelSuccessHandler: uid = " + uid);
    }

    // 우리가 많이 바꾼 코드...
    // 나를 기준으로 유저가 들어온 후에 호출되는 콜백
    protected virtual void OnUserJoined(uint uid, int elapsed)
    {
        string[] array = new string[3]; //4명만 유저 받기
        for (int i = 0; i < 4; i++)      //4명까지만 userID array배열에 저장
        {
            array[i] = uid.ToString();

            Debug.Log("onUserJoined: uid = " + uid + " elapsed = " + elapsed);

            // find a game object to render video stream from 'uid'
            GameObject go = GameObject.Find(array[i]);

            if (!ReferenceEquals(go, null)) //ID가 같은지 검사하고, 같으면 return (나랑 상대방 ID 식별)
            {
                return; // reuse
            }

            // 사용자가 들어오면 캠 이미지를 그려줌 --> 만들어준걸 캠동기화 값에 넣어줌(동기화 시킬 수 있도록)
            VideoSurface videoSurface = makeImageSurface(array[i], i);
            if (!ReferenceEquals(videoSurface, null)) //ID가 같은지 검사하고, 나랑 상대방 ID 다를때만
            {
                // 동기화 설정
                videoSurface.SetForUser(uid); //userID Set
                videoSurface.SetEnable(true); //캠 기능 수행 Set true
                videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage); //캠동기화 속성 설정
                videoSurface.SetGameFps(30); //프레임률
                videoSurface.EnableFilpTextureApply(enableFlipHorizontal: true, enableFlipVertical: false); //텍스처 조정 가능

                //들어온 유저의 비디오에다 위에서 설정한 동기화 값을 넣어줌
                UserVideoDict[uid] = videoSurface;

                //우리가 헤매고있는거
                //캠 포지션을 정해주고, transform 동기화 설정.
                Vector2 pos = AgoraUIUtils.GetRandomPosition(i);
                videoSurface.transform.localPosition = new Vector2(pos.x, pos.y);
            }
        }

    }

    // 사용자가 나갈때 콜백되는 함수
    protected virtual void OnUserOffline(uint uid, USER_OFFLINE_REASON reason)
    {
        // remove video stream
        Debug.Log("onUserOffline: uid = " + uid + " reason = " + reason);
        if (UserVideoDict.ContainsKey(uid))
        {
            //사용자 캠 저장된 리스트에서 해당 userid를 가진 것만 가져와서 동기화 값 변경
            var surface = UserVideoDict[uid];
            surface.SetEnable(false);
            UserVideoDict.Remove(uid);
            GameObject.Destroy(surface.gameObject);
        }
    }

    //캠 화면을 UI에 그려주는 기능
    protected VideoSurface makeImageSurface(string goName, int i)
    {
        GameObject go = new GameObject();

        if (go == null)
        {
            return null;
        }

        go.name = goName; //캔버스의 이름을 userID로 변경

        // to be renderered onto
        RawImage image = go.AddComponent<RawImage>(); //화면 송출될 수 있게 컴포넌트 추가
        image.rectTransform.sizeDelta = new Vector2(1, 1);// 스케일

        //유저의 캠화면이 담긴 캔버스를 이미 만들어져있는 캔버스의 자식으로 설정
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            go.transform.SetParent(canvas.transform);
        }


        // transform 값 변경
        go.transform.Rotate(0f, -180.0f, 180.0f); //로테이션 값
        Vector2 v2 = AgoraUIUtils.GetRandomPosition(i); //for문의 i값 받아와서 포지션 변경
        go.transform.position = new Vector2(v2.x, v2.y);
        go.transform.localScale = Vector3.one;

        // 설정한 비디오 동기화 값 리턴.
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }
    #endregion
}
