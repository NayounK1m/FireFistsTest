using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using agora_gaming_rtc;

public class DesktopScreenShare : PlayerViewControllerBase
{
    readonly List<AgoraNativeBridge.RECT> WinDisplays = new List<AgoraNativeBridge.RECT>();
    int CurrentDisplay = 0;

    //UI 설정
    protected override void SetupUI()
    {
        base.SetupUI();

        // Monitor Display info
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

    //화면공유.
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
