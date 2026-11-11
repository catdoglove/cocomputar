#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
namespace CoComputar
{
    // 별도 QA 저장 경로와 가상 입력 장치만 사용한다. 일반 실행에는 추가되지 않는다.
    public sealed partial class PlaythroughQA : MonoBehaviour
    {
        GameApp app; Keyboard keyboard; Mouse mouse; readonly List<string> results=new(); string output; float start;
        void Awake() { start=Time.realtimeSinceStartup;Application.logMessageReceived+=Log; }
        void Log(string message,string stack,LogType type) { if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert) {File.AppendAllText(Path.GetFullPath("../QA-errors.txt"),message+"\n"+stack+"\n");Quit(1);} }
        void Update() { if(Time.realtimeSinceStartup-start>(Array.IndexOf(Environment.GetCommandLineArgs(),"-coFullStagesQA")>=0?1000:450)) {Debug.LogError("QA 제한 시간 초과");} }
        IEnumerator Start()
        {
            app=GameApp.Instance; output=Path.GetFullPath("../QA");Directory.CreateDirectory(output);
            Application.targetFrameRate=60;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach(var device in InputSystem.devices.ToArray()) if(device is Mouse || device is Keyboard) InputSystem.DisableDevice(device);
            keyboard=InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(40,40)});
            yield return new WaitForSeconds(2);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coStage09QA")>=0)
            {
                yield return Stage09Checks();File.WriteAllLines(Path.Combine(output,"stage09-checks.txt"),results);Debug.Log("STAGE09 QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coSpecialQA")>=0)
            {
                yield return SpecialChecks();File.WriteAllLines(Path.Combine(output,"special-checks.txt"),results);Debug.Log("SPECIAL QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coStage08QA")>=0)
            {
                yield return Stage08Checks();File.WriteAllLines(Path.Combine(output,"stage08-checks.txt"),results);Debug.Log("STAGE08 QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coTextCursorQA")>=0)
            {
                yield return TextCursorChecks();File.WriteAllLines(Path.Combine(output,"text-cursor-checks.txt"),results);Debug.Log("TEXT CURSOR QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coStage07QA")>=0)
            {
                yield return Stage07Checks();File.WriteAllLines(Path.Combine(output,"stage07-checks.txt"),results);Debug.Log("STAGE07 QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coSortingToolbarQA")>=0)
            {
                yield return SortingToolbarChecks();File.WriteAllLines(Path.Combine(output,"sorting-toolbar-checks.txt"),results);Debug.Log("SORTING TOOLBAR QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coStage45QA")>=0)
            {
                yield return Stage45TransitionChecks();File.WriteAllLines(Path.Combine(output,"stage45-checks.txt"),results);Debug.Log("STAGE45 QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coTitlePastDoorQA")>=0)
            {
                yield return TitlePastDoorChecks();File.WriteAllLines(Path.Combine(output,"title-past-door-checks.txt"),results);Debug.Log("TITLE PAST DOOR QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coConsoleFontQA")>=0)
            {
                app.Save.NewGame("Stage01");app.Save.Flag("stage01.wandered");app.Begin(false);yield return null;
                yield return LoadingChecks();File.WriteAllLines(Path.Combine(output,"console-font-checks.txt"),results);Debug.Log("CONSOLE FONT QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coWindowQA")>=0)
            {
                yield return WindowChecks();File.WriteAllLines(Path.Combine(output,"window-checks.txt"),results);Debug.Log("WINDOW QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coFontQA")>=0)
            {
                yield return FontPreview();File.WriteAllLines(Path.Combine(output,"font-checks.txt"),results);Debug.Log("FONT QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coPortraitPreview")>=0)
            {
                yield return PortraitPreview();File.WriteAllLines(Path.Combine(output,"portrait-checks.txt"),results);Debug.Log("PORTRAIT QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coPhotoCloseQA")>=0)
            {
                yield return PhotoCloseChecks();File.WriteAllLines(Path.Combine(output,"stage05-photo-close-checks.txt"),results);Debug.Log("PHOTO CLOSE QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coCardInputQA")>=0)
            {
                yield return CardInputChecks();File.WriteAllLines(Path.Combine(output,"card-input-checks.txt"),results);Debug.Log("CARD INPUT QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coFolderViewQA")>=0)
            {
                yield return FolderViewChecks();File.WriteAllLines(Path.Combine(output,"folder-view-checks.txt"),results);Debug.Log("FOLDER VIEW QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coStage06QA")>=0)
            {
                yield return Stage06Checks();File.WriteAllLines(Path.Combine(output,"stage06-checks.txt"),results);Debug.Log("STAGE06 QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coExplorerQA")>=0)
            {
                yield return ExplorerChecks();File.WriteAllLines(Path.Combine(output,"explorer-checks.txt"),results);Debug.Log("EXPLORER QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coEncodingQA")>=0)
            {
                yield return EncodingChecks();File.WriteAllLines(Path.Combine(output,"encoding-checks.txt"),results);Debug.Log("ENCODING QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coHalfClearQA")>=0)
            {
                yield return HalfClearChecks();File.WriteAllLines(Path.Combine(output,"half-clear-checks.txt"),results);Debug.Log("HALF CLEAR QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coStage05QA")>=0)
            {
                yield return Stage05Checks();File.WriteAllLines(Path.Combine(output,"stage05-checks.txt"),results);Debug.Log("STAGE05 QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coDiaryLockQA")>=0)
            {
                yield return DiaryLockChecks();yield return Stage04Checks();File.WriteAllLines(Path.Combine(output,"diary-lock-checks.txt"),results);Debug.Log("DIARY LOCK QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coDreamWindowQA")>=0)
            {
                yield return DreamWindowChecks();yield return Stage04Checks();File.WriteAllLines(Path.Combine(output,"dream-window-checks.txt"),results);Debug.Log("DREAM WINDOW QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coStage04QA")>=0)
            {
                yield return Stage04Checks();File.WriteAllLines(Path.Combine(output,"stage04-checks.txt"),results);Debug.Log("STAGE04 QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coSkipUnlockQA")>=0)
            {
                yield return SkipUnlockChecks();File.WriteAllLines(Path.Combine(output,"skip-unlock-checks.txt"),results);Debug.Log("SKIP UNLOCK QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coPastImageQA")>=0)
            {
                yield return PastImageChecks();File.WriteAllLines(Path.Combine(output,"past-image-checks.txt"),results);Debug.Log("PAST IMAGE QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coStage03QA")>=0)
            {
                yield return Stage03Checks();File.WriteAllLines(Path.Combine(output,"stage03-checks.txt"),results);Debug.Log("STAGE03 QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coKeyDialogueVisualQA")>=0)
            {
                yield return KeyDialogueVisual();Debug.Log("KEY DIALOGUE VISUAL QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coKeyDialogueQA")>=0)
            {
                yield return KeyDialogueChecks();File.WriteAllLines(Path.Combine(output,"key-dialogue-checks.txt"),results);Debug.Log("KEY DIALOGUE QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coFlowerKeyQA")>=0)
            {
                yield return FlowerKeyChecks();File.WriteAllLines(Path.Combine(output,"flower-key-checks.txt"),results);Debug.Log("FLOWER KEY QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coPastQA")>=0)
            {
                yield return PastChecks();File.WriteAllLines(Path.Combine(output,"past-checks.txt"),results);Debug.Log("PAST QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coInputCRTQA")>=0)
            {
                yield return InputCRTChecks();File.WriteAllLines(Path.Combine(output,"input-crt-checks.txt"),results);Debug.Log("INPUT CRT QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coRestartQA")>=0)
            {
                yield return RestartChecks();File.WriteAllLines(Path.Combine(output,"restart-checks.txt"),results);Debug.Log("RESTART QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coDateQA")>=0)
            {
                yield return DateChecks();if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coDateOnlyQA")<0){yield return TulipChecks();yield return Stage02Checks();}File.WriteAllLines(Path.Combine(output,"date-checks.txt"),results);Debug.Log("DATE QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coTulipQA")>=0)
            {
                yield return TulipChecks();File.WriteAllLines(Path.Combine(output,"tulip-checks.txt"),results);Debug.Log("TULIP QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coWaterQA")>=0)
            {
                yield return WaterChecks();yield return Stage02Checks();File.WriteAllLines(Path.Combine(output,"water-checks.txt"),results);Debug.Log("WATER QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coTrashQA")>=0)
            {
                yield return TrashChecks();yield return CtrlChecks();yield return Stage02Checks();File.WriteAllLines(Path.Combine(output,"trash-checks.txt"),results);Debug.Log("TRASH QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coStage02QA")>=0)
            {
                yield return Stage02Checks();File.WriteAllLines(Path.Combine(output,"stage02-checks.txt"),results);Debug.Log("STAGE02 QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coDialogueCloseQA")>=0)
            {
                yield return DialogueCloseChecks();
                File.WriteAllLines(Path.Combine(output,"dialogue-close-checks.txt"),results);Debug.Log("DIALOGUE CLOSE QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coCrossFolderQA")>=0)
            {
                dragLayerQA=true;yield return CrossFolderChecks();yield return WindowDropChecks();yield return DragMailChecks();
                File.WriteAllLines(Path.Combine(output,"cross-folder-checks.txt"),results);Debug.Log("CROSS FOLDER QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coDragLayerQA")>=0)
            {
                dragLayerQA=true;yield return WindowDropChecks();yield return DragMailChecks();
                Check(dragLayerFrames>100 && app.DragLayer.childCount==0,"드래그 전용 상위 표시·입력 통과 및 종료 후 정리");
                File.WriteAllLines(Path.Combine(output,"drag-layer-checks.txt"),results);Debug.Log("DRAG LAYER QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coWindowDropQA")>=0)
            {
                yield return WindowDropChecks();yield return DragMailChecks();
                File.WriteAllLines(Path.Combine(output,"window-drop-checks.txt"),results);Debug.Log("WINDOW DROP QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coSpaceMenuQA")>=0)
            {
                yield return SpaceMenuChecks();
                File.WriteAllLines(Path.Combine(output,"space-menu-checks.txt"),results);Debug.Log("SPACE MENU QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coFocusQA")>=0)
            {
                yield return MenuChecks();
                File.WriteAllLines(Path.Combine(output,"focus-checks.txt"),results);Debug.Log("FOCUS QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coFileMailQA")>=0)
            {
                yield return FileOpenChecks();yield return DragMailChecks();yield return LoadingChecks();
                File.WriteAllLines(Path.Combine(output,"file-mail-checks.txt"),results);Debug.Log("FILE MAIL QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coExpressionQA")>=0)
            {
                yield return ExpressionChecks();
                File.WriteAllLines(Path.Combine(output,"expression-checks.txt"),results);Debug.Log("EXPRESSION QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coCameraQA")>=0)
            {
                app.Save.NewGame("Stage01");app.Begin(false);yield return new WaitForSeconds(.2f);
                cameraQA=true;yield return MazeChecks();
                Check(cameraFrames>100 && centeredFrames>10,"이동 중 카메라 범위 및 중앙 추적 프레임 검사");
                File.WriteAllLines(Path.Combine(output,"camera-checks.txt"),results);Debug.Log("CAMERA QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coDragMailQA")>=0)
            {
                yield return DragMailChecks();
                File.WriteAllLines(Path.Combine(output,"drag-mail-checks.txt"),results);Debug.Log("DRAG MAIL QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coPolishQA")>=0)
            {
                yield return MenuChecks();yield return PolishChecks();
                File.WriteAllLines(Path.Combine(output,"polish-checks.txt"),results);Debug.Log("POLISH QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coMenuQA")>=0)
            {
                yield return MenuChecks();
                File.WriteAllLines(Path.Combine(output,"menu-checks.txt"),results);Debug.Log("MENU QA PASS "+results.Count);Quit(0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coLayoutQA")>=0)
            {
                yield return LayoutChecks();
                File.WriteAllLines(Path.Combine(output,"layout-checks.txt"),results);Debug.Log("LAYOUT QA PASS "+results.Count);Quit(0);yield break;
            }
            Check(app.Catalog.theme.font.atlasPopulationMode==TMPro.AtlasPopulationMode.Dynamic,"동적 한글 폰트");
            Check(app.Catalog.musicGroup && app.Catalog.effectsGroup,"음악과 효과음 믹서 분리");
            app.Save.NewGame("Stage01");
            app.Save.Options.music=.35f;
            app.Save.Options.filter=0;app.ApplyOptions();Capture("00-filter-off");
            app.Save.Options.filter=.65f;app.ApplyOptions();
            Check(Mathf.Abs(app.Catalog.crt.material.GetFloat("_Intensity")-.65f)<.001f,"필터 슬라이더와 CRT 머티리얼 연동");
            Capture("01-title");
            yield return MouseClick(Buttons().First(b=>b.name=="Option").GetComponent<RectTransform>());
            Check(app.Windows.GetComponentInChildren<TitleMenuPanel>(),"Input System 마우스 클릭으로 옵션 패널 열기");
            // 펼침 애니메이션 동안 입력이 잠기므로 실제 조작 가능 시점까지 기다린다.
            yield return new WaitForSeconds(.25f);
            float volume=app.Save.Options.music;yield return Press(Key.RightArrow);
            Check(app.Save.Options.music>volume,"키보드 오른쪽으로 음악 슬라이더 조절");
            yield return new WaitForSeconds(.25f);
            yield return Press(Key.Escape);
            Check(!app.Windows.GetComponentInChildren<TitleMenuPanel>(),"Esc로 타이틀 옵션 패널 닫기");
            Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSeconds(.6f);
            var viewport=app.UICamera.rect;Check(Mathf.Abs(viewport.width*Screen.width/(viewport.height*Screen.height)-4f/3f)<.01f,"와이드 화면의 4:3 뷰포트");Capture("11-widescreen");
            Screen.SetResolution(1024,768,FullScreenMode.Windowed);yield return new WaitForSeconds(.4f);
            foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1440,900),new Vector2Int(900,1000)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSeconds(.4f);
                var r=app.UICamera.rect;Check(Mathf.Abs(r.width*Screen.width/(r.height*Screen.height)-4f/3f)<.01f,$"{size.x}×{size.y} 비율 유지");
                yield return MouseClick(Buttons().First(b=>b.name=="Option").GetComponent<RectTransform>());yield return new WaitForSeconds(.2f);
                Check(app.Windows.GetComponentInChildren<TitleMenuPanel>(),$"{size.x}×{size.y} 클릭 좌표 일치");
                Capture($"resolution-{size.x}x{size.y}");
                yield return Press(Key.Escape);
            }
            Screen.SetResolution(1024,768,FullScreenMode.Windowed);yield return new WaitForSeconds(.4f);
            app.ShowTitle();yield return null;
            // 실제 키보드 메뉴 경로를 통해 시작한다.
            yield return Press(Key.Enter);
            Check(Buttons().Any(b=>b.name=="게임 시작하기"),"키보드 Start 메뉴");
            yield return new WaitForSeconds(.25f);
            yield return MouseClick(Buttons().First(b=>b.name=="게임 시작하기").GetComponent<RectTransform>());
            if(Buttons().Any(b=>b.name=="확인")) {yield return new WaitForSeconds(.2f);yield return MouseClick(Buttons().Last(b=>b.name=="확인").GetComponent<RectTransform>());}
            yield return MazeChecks();
            yield return SingleWindowChecks();
            app.Save.UnlockDialogueSkip();yield return CtrlChecks();
            yield return Icon("npc");yield return new WaitForSeconds(.3f);Capture("04-dialogue");
            var hits=new List<RaycastResult>();var pointerData=Pointer(RectTransformUtility.WorldToScreenPoint(app.UICamera,Icons().First(i=>i.Id=="sort").transform.position));
            EventSystem.current.RaycastAll(pointerData,hits);Check(hits.Count>0 && hits[0].gameObject.transform.IsChildOf(app.Modal),"대화 모달의 아이콘 클릭 차단");
            yield return FinishDialogue();Check(app.Stages.State==StageState.SortNormal,"첫 대화 → 퍼즐 1 해금");
            yield return Icon("sort");yield return new WaitForSeconds(.3f);Capture("05-sort");
            yield return Icon("document_0");Check(FindWindow("일정.txt - 메모장")!=null,"문서 미리보기");FindWindow("일정.txt - 메모장").Close();
            yield return Drop("document_0","picture");
            yield return Icon("npc");yield return FinishDialogue();Check(app.Stages.State==StageState.SortNormal,"오답 검수는 진행을 잠금");
            yield return Icon("sort");yield return new WaitForSeconds(.2f);
            yield return Icon("picture");yield return new WaitForSeconds(.2f);
            var recoverIcon=Icons().Last(i=>i.Id=="document_0");
            yield return MouseDrag(RectTransformUtility.WorldToScreenPoint(app.UICamera,recoverIcon.Rect.TransformPoint(recoverIcon.Rect.rect.center)),RectTransformUtility.WorldToScreenPoint(app.UICamera,app.Root.TransformPoint(new Vector3(400,-670,0))));
            FindWindow("사진첩").Close();yield return null;Check(app.Stages.Sorting.Location("document_0")=="","잘못 넣은 파일 회수");
            foreach(var file in app.Stages.Sorting.Files.Where(f=>!f.hidden)) yield return Drop(file.id,file.category);
            Check(app.Stages.Sorting.IsCorrect(false),"일반 파일 8개 드래그 정리");
            yield return Icon("npc");yield return FinishDialogue();Check(app.Stages.State==StageState.SortHidden,"1차 검수 → 숨김 퍼즐");
            yield return Icon("sort");yield return new WaitForSeconds(.2f);
            yield return ClickButton("설정 · 숨김파일 표시");yield return new WaitForSeconds(.25f);Capture("06-hidden");
            var currentWindow=FindWindow("작업 폴더 — File Explorer");currentWindow.Close();yield return null;yield return Icon("sort");yield return new WaitForSeconds(.2f);
            Check(FindWindow("작업 폴더 — File Explorer").ShowHidden && Icons().Any(i=>i.Id=="document_3"),"창 재개방 시 숨김 체크 및 파일 표시 유지");
            yield return Icon("document");
            Check(FindWindow("문서함").ShowHidden,"하위 폴더 이동 시 숨김 표시 공유");
            FindWindow("문서함").Close();yield return null;
            var hiddenButton=FindWindow("작업 폴더 — File Explorer").GetComponentsInChildren<Button>(true).First(b=>b.name=="설정 · 숨김파일 표시");
            yield return MouseClick(hiddenButton.GetComponent<RectTransform>());
            Check(!new SaveService().Hidden("explorer") && !Icons().Any(i=>i.Id=="document_3"),"직접 체크 해제 시 숨김 파일 감추기 및 저장");
            yield return MouseClick(hiddenButton.GetComponent<RectTransform>());
            Check(new SaveService().Hidden("explorer") && !new SaveService().Hidden("mail"),"숨김 설정 디스크 재로드 및 구역 독립성");
            foreach(var file in app.Stages.Sorting.Files.Where(f=>f.hidden)) yield return Drop(file.id,file.category);
            yield return Icon("npc");yield return FinishDialogue();Check(app.Stages.State==StageState.Maze,"2차 검수와 결말 → 1989년");
            app.Stages.ToggleDesktopHidden();yield return ClickButton("숨김파일 표시");yield return Icon("toxic");yield return Icon("inside");
            yield return new WaitForSeconds(.2f);
            Check(!Icons().Any(i=>i.Id=="5.txt"),"바탕화면과 우체통 숨김 표시 독립");
            yield return Drop("1.txt","mail");Check(!app.Save.Progress.flags.Contains("stage01.mail"),"가짜 편지 무반응");
            var mail=FindWindow("남겨진 편지");mail.GetComponentsInChildren<Button>(true).First(b=>b.name=="설정 · 숨김파일 표시").onClick.Invoke();
            yield return null;Capture("07-mail");yield return Drop("5.txt","mail");yield return new WaitForSeconds(3.6f);
            Check(app.Save.Progress.flags.Contains("stage01.mail"),"진짜 편지 완료 플래그 저장");
            foreach(var w in app.Windows.GetComponentsInChildren<FakeWindow>()) w.Close();yield return null;
            yield return Icon("1989");yield return Icon("summer");yield return Icon("mem");yield return Icon("home");yield return Icon("door");yield return new WaitForSeconds(.25f);Capture("08-maze");
            yield return Icon("door.exe");yield return new WaitForSeconds(3.4f);Capture("09-cmd");yield return new WaitForSeconds(5);
            float transitionDeadline=Time.realtimeSinceStartup+15;
            while(app.Stages.Current.stageId!="Stage02"&&Time.realtimeSinceStartup<transitionDeadline)yield return null;
            Check(app.Stages.Current.stageId=="Stage02","CMD → 데이터 전용 Stage02 전환");
            var reloaded=new SaveService();Check(reloaded.Progress.cleared.Contains("Stage01") && reloaded.Progress.stageId=="Stage02" && reloaded.Progress.flags.Contains("stage01.mail"),"재로드: 스테이지·클리어·사이드 기록 유지");
            app.Save.Options.filter=.37f;app.Save.SaveOptions();Check(Mathf.Abs(new SaveService().Options.filter-.37f)<.001f,"옵션 별도 영구 저장");
            Capture("10-stage02");
            yield return ClockChecks();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coFullStagesQA")>=0){yield return Stage02Checks(true);yield return Stage03Checks(true);yield return Stage06Checks();yield return Stage07Checks(true);}
            app.Save.NewGame("Stage01");var reset=new SaveService();Check(reset.Progress.cleared.Count==0 && reset.Progress.flags.All(f=>f=="stage06.starObtained") && reset.Options.hiddenViews.Count==0 && reset.Options.filter==.37f,"새 게임: 진행·숨김 표시 초기화, 다른 옵션 유지");
            File.WriteAllText(Path.Combine(output,"playthrough.txt"),string.Join("\n",results));
            Debug.Log("QA PASS "+results.Count);yield return new WaitForSeconds(1);Quit(0);
        }
        void Check(bool value,string label) {if(!value) throw new Exception("QA 실패: "+label);results.Add("통과: "+label);Debug.Log("QA 통과: "+label);}
        IEnumerator DialogueCloseChecks()
        {
            app.Save.NewGame("Stage01");app.Save.Flag("stage01.wandered");app.Begin(false);yield return null;
            app.Stages.Sorting.Move("document_0","picture");app.Stages.OpenSorting();yield return new WaitForSeconds(.2f);
            foreach(var category in new[]{"document","picture","music"}) {yield return Icon(category);yield return new WaitForSeconds(.2f);}
            WindowApps.Note("대화 전 문서","검사");WindowApps.Picture("대화 전 그림","cat");WindowApps.Alert("재생할 수 없습니다");app.OpenOptions();
            app.gameObject.AddComponent<MailPuzzle>().Open();
            UIFactory.Panel(app.Windows,"프로그램 패널",500,500,100,100,Color.black);
            var opened=app.Windows.GetComponentsInChildren<FakeWindow>();int closed=0;foreach(var w in opened)w.Closed+=()=>closed++;
            Check(opened.Length>=9,"폴더·문서·이미지·음악 안내·옵션·우편 창 열기");
            yield return new WaitForSeconds(.25f);yield return MouseClick(Icons().First(i=>i.Id=="npc").Rect);
            Check(app.Dialogue.Active && !app.Windows.GetComponentInChildren<FakeWindow>() && app.Windows.childCount==0,"NPC 클릭 대화 시작 즉시 모든 창·프로그램 패널 닫기");
            Check(closed==opened.Length && !app.GetComponent<MailPuzzle>(),"각 창의 종료 콜백과 우편 컴포넌트 정리");
            Check(app.DragLayer.childCount==0 && app.Modal.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t=>t.name=="텍스트"),"드래그 잔상 정리 및 대화 UI 유지");Capture("dialogue-clean-desktop");
            yield return FinishDialogue();yield return new WaitForSeconds(.3f);
            Check(app.Windows.childCount==0 && !app.Dialogue.Active && app.Stages.State==StageState.SortNormal,"대화 종료 후 깨끗한 바탕화면·진행 유지");
            yield return MouseClick(Icons().First(i=>i.Id=="sort").Rect);yield return new WaitForSeconds(.2f);
            Check(FindWindow("작업 폴더 — File Explorer") && app.Stages.Sorting.Location("document_0")=="picture","대화 후 작업 폴더 재열기 및 파일 상태 보존");
            yield return Icon("picture");yield return new WaitForSeconds(.2f);yield return MouseClick(Icons().First(i=>i.Id=="document_0").Rect);yield return new WaitForSeconds(.2f);
            Check(FindWindow("일정.txt - 메모장"),"대화 후 하위 폴더·문서 자유롭게 재열기");
            app.CloseAllWindows();app.CloseAllWindows();yield return null;Check(app.Windows.childCount==0,"공통 닫기 함수 반복 호출 안전");
            app.OpenComputer();yield return new WaitForSeconds(.2f);yield return MouseClick(Icons().First(i=>i.Id=="npc").Rect);
            Check(app.Dialogue.Active && !FindWindow("내 컴퓨터 - 메모장"),"다음 NPC 대화에서도 재개방 창 자동 닫기");
            yield return FinishDialogue();Check(app.Windows.childCount==0,"후속 대화 종료 후에도 자동 복원 없음");
        }
        IEnumerator CrossFolderChecks()
        {
            app.Save.NewGame("Stage01");app.Save.Flag("stage01.wandered");app.Begin(false);yield return null;
            app.Stages.Sorting.Move("document_0","document");app.Stages.OpenSorting();yield return new WaitForSeconds(.2f);
            foreach(var category in new[]{"document","picture","music"}) {yield return Icon(category);yield return new WaitForSeconds(.2f);}
            FakeWindow Folder(string category)=>app.Windows.GetComponentsInChildren<FakeWindow>().First(w=>w.Key=="category:"+category);
            foreach(var pair in new[]{new[]{"document","picture"},new[]{"picture","music"},new[]{"music","document"}})
            {
                var from=Folder(pair[0]);var to=Folder(pair[1]);
                foreach(var w in new[]{Folder("document"),Folder("picture"),Folder("music")})w.Rect.anchoredPosition=new Vector2(900,-650);
                from.Rect.anchoredPosition=new Vector2(20,-110);from.Focus();to.Rect.anchoredPosition=new Vector2(350,-300);to.Focus();
                var item=from.GetComponentsInChildren<FakeIcon>().First(i=>i.Id=="document_0");
                Vector2 StartPoint()=>RectTransformUtility.WorldToScreenPoint(app.UICamera,item.Rect.TransformPoint(item.Rect.rect.center));
                Vector2 EndPoint(float y)=>RectTransformUtility.WorldToScreenPoint(app.UICamera,to.Content.TransformPoint(new Vector3(250,-y,0)));
                yield return MouseDrag(StartPoint(),EndPoint(-25));
                Check(app.Stages.Sorting.Location(item.Id)==pair[0],"창 간 이동: 대상 제목줄 반려 "+pair[1]);
                yield return MouseDrag(StartPoint(),EndPoint(160));yield return null;
                Check(app.Stages.Sorting.Location("document_0")==pair[1] && !from.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="document_0") && to.GetComponentsInChildren<FakeIcon>().Count(i=>i.Id=="document_0")==1,"창 간 직접 이동 및 양쪽 갱신 "+pair[0]+" → "+pair[1]);
            }
            var source=Folder("document");var destination=Folder("picture");
            FindWindow("작업 폴더 — File Explorer").Close();yield return null;
            source.Rect.anchoredPosition=new Vector2(20,-110);source.Focus();destination.Rect.anchoredPosition=new Vector2(350,-300);destination.Focus();
            var file=source.GetComponentsInChildren<FakeIcon>().First(i=>i.Id=="document_0");
            yield return MouseDrag(RectTransformUtility.WorldToScreenPoint(app.UICamera,file.Rect.TransformPoint(file.Rect.rect.center)),RectTransformUtility.WorldToScreenPoint(app.UICamera,destination.Content.TransformPoint(new Vector3(250,-160,0))));
            Check(app.Stages.Sorting.Location("document_0")=="picture","상위 작업 폴더가 닫혀도 창 간 이동 가능");Capture("cross-folder-move");
            app.Stages.OpenSorting();yield return new WaitForSeconds(.2f);
            var sorting=FindWindow("작업 폴더 — File Explorer");sorting.Rect.anchoredPosition=new Vector2(200,-80);sorting.Focus();
            destination.Rect.anchoredPosition=new Vector2(20,-350);destination.Focus();
            file=destination.GetComponentsInChildren<FakeIcon>().First(i=>i.Id=="document_0");
            var folderIcon=sorting.GetComponentsInChildren<FakeIcon>().First(i=>i.Id=="music");
            yield return MouseDrag(RectTransformUtility.WorldToScreenPoint(app.UICamera,file.Rect.TransformPoint(file.Rect.rect.center)),RectTransformUtility.WorldToScreenPoint(app.UICamera,folderIcon.Rect.TransformPoint(folderIcon.Rect.rect.center)));
            Check(app.Stages.Sorting.Location("document_0")=="music" && Folder("music").GetComponentsInChildren<FakeIcon>().Count(i=>i.Id=="document_0")==1,"하위 폴더 → 폴더 아이콘 이동 및 이미 열린 대상 갱신");
        }
        IEnumerator WindowDropChecks()
        {
            foreach(var category in new[]{"document","picture","music"})
            {
                app.Save.NewGame("Stage01");app.Save.Flag("stage01.wandered");app.Begin(false);yield return null;
                var record=app.Stages.Sorting.Files.First(f=>f.category==category && !f.hidden);
                foreach(var other in app.Stages.Sorting.Files)if(other.id!=record.id)app.Stages.Sorting.Move(other.id,"document");
                app.Stages.OpenSorting();yield return new WaitForSeconds(.2f);yield return Icon(category);yield return new WaitForSeconds(.2f);
                var dest=app.Windows.GetComponentsInChildren<FakeWindow>().First(w=>w.Key=="category:"+category);dest.Rect.anchoredPosition=new Vector2(350,-420);
                var item=Icons().First(i=>i.Id==record.id && i.transform.IsChildOf(FindWindow("작업 폴더 — File Explorer").transform));
                Vector2 StartPoint()=>RectTransformUtility.WorldToScreenPoint(app.UICamera,item.Rect.TransformPoint(item.Rect.rect.center));
                Vector2 Target(float x,float y)=>RectTransformUtility.WorldToScreenPoint(app.UICamera,dest.Content.TransformPoint(new Vector3(x,-y,0)));
                yield return MouseDrag(StartPoint(),Target(120,-25));
                Check(app.Stages.Sorting.Location(record.id)=="","제목줄 드롭은 파일 이동 없음: "+category);
                WindowApps.Note("드롭 가림 검사","앞 창");yield return new WaitForSeconds(.2f);
                var cover=FindWindow("드롭 가림 검사 - 메모장");cover.Rect.anchoredPosition=new Vector2(350,-420);
                yield return MouseDrag(StartPoint(),Target(120,80));
                Check(app.Stages.Sorting.Location(record.id)=="","다른 창 뒤의 폴더에 잘못 드롭되지 않음: "+category);
                cover.Close();yield return null;
                yield return MouseDrag(StartPoint(),Target(120,80));yield return null;
                Check(app.Stages.Sorting.Location(record.id)==category,"열린 폴더 내용 영역 드롭 성공: "+category);
                Check(dest.GetComponentsInChildren<FakeIcon>().Count(i=>i.Id==record.id)==1 && !FindWindow("작업 폴더 — File Explorer").GetComponentsInChildren<FakeIcon>().Any(i=>i.Id==record.id),"상위 목록 제거·열린 폴더 즉시 갱신: "+category);
                Capture("window-drop-"+category);
            }
        }
        IEnumerator SpaceMenuChecks()
        {
            app.Save.NewGame("Stage01");app.Save.Flag("space-menu-marker");
            yield return Press(Key.Space);yield return new WaitForSeconds(.25f);
            Check(Buttons().Any(b=>b.name=="게임 시작하기") && !Buttons().Any(b=>b.name=="취소"),"Start를 연 Space가 하위 항목까지 중복 실행되지 않음");
            var first=Buttons().First(b=>b.name=="게임 시작하기");int clicks=0;first.onClick.AddListener(()=>clicks++);
            yield return Hold(Key.Space,.5f);
            Check(clicks==1 && Buttons().Any(b=>b.name=="확인") && !app.GetComponent<PrologueController>(),"게임 시작하기 Space: 클릭과 같은 확인 UI, 홀드 중 1회 실행");
            yield return Press(Key.Space);
            Check(Buttons().Any(b=>b.name=="취소") && app.Save.Progress.flags.Contains("space-menu-marker"),"시작 확인 UI에는 Space 단축키 미적용");
            yield return MouseClick(Buttons().First(b=>b.name=="취소").GetComponent<RectTransform>());yield return new WaitForSeconds(.25f);
            yield return Press(Key.DownArrow);yield return Press(Key.Space);yield return new WaitForSeconds(.2f);
            Check(app.GetComponent<PrologueController>() && app.Save.Progress.flags.Contains("space-menu-marker"),"이어하기 Space: 저장 유지하며 기존 Begin(false) 실행");
            app.ShowTitle();yield return new WaitForSeconds(.2f);
            yield return MouseClick(Buttons().First(b=>b.name=="Start").GetComponent<RectTransform>());yield return new WaitForSeconds(.25f);
            var next=Buttons().First(b=>b.name=="이어하기");next.interactable=false;EventSystem.current.SetSelectedGameObject(next.gameObject);
            yield return Press(Key.Space);Check(Buttons().Any(b=>b.name=="게임 시작하기") && !app.GetComponent<PrologueController>(),"비활성 이어하기는 Space로 실행되지 않음");
            yield return Press(Key.Escape);app.OpenOptions();yield return new WaitForSeconds(.25f);
            var panel=app.Windows.GetComponentInChildren<TitleMenuPanel>();float music=app.Save.Options.music;
            yield return Press(Key.Space);Check(panel && app.Windows.GetComponentInChildren<TitleMenuPanel>()==panel && music==app.Save.Options.music && !app.GetComponent<PrologueController>(),"Option에서 Space는 실행·설정 변경 없음");
            app.ResetScreen();app.Desktop();yield return null;app.OpenComputer();yield return new WaitForSeconds(.2f);
            yield return Press(Key.Space);Check(FindWindow("내 컴퓨터 - 메모장") && !app.GetComponent<PrologueController>(),"바탕화면 문서창에 Start Space 단축키 영향 없음");
        }
        IEnumerator MenuChecks()
        {
            app.Save.NewGame("Stage01");app.Save.Options.music=.2f;
            yield return MouseClick(Buttons().First(b=>b.name=="Start").GetComponent<RectTransform>());
            yield return new WaitForSeconds(.25f);
            var panel=app.Windows.GetComponentInChildren<TitleMenuPanel>();
            Check(panel && !app.Windows.GetComponentInChildren<FakeWindow>(),"Start는 창 없이 초록 패널로 표시");
            Check(((RectTransform)panel.Content.parent).anchoredPosition==new Vector2(550,-415) && panel.Content.parent.localScale==Vector3.one,"Start 옆 위치 및 펼침 완료");
            var first=panel.GetComponentsInChildren<Button>().First(b=>b.name=="게임 시작하기");
            var next=panel.GetComponentsInChildren<Button>().First(b=>b.name=="이어하기");
            Check(first.GetComponentInChildren<TMPro.TextMeshProUGUI>().color==app.Catalog.theme.pink,"초기 키보드 포커스 분홍색");
            Check(first.transform.Find("포커스 화살표").GetComponent<TMPro.TextMeshProUGUI>().enabled && !next.transform.Find("포커스 화살표").GetComponent<TMPro.TextMeshProUGUI>().enabled,"Start 초기 선택에만 화살표 표시");
            yield return Press(Key.DownArrow);yield return null;
            Check(EventSystem.current.currentSelectedGameObject==next.gameObject && next.GetComponentInChildren<TMPro.TextMeshProUGUI>().color==app.Catalog.theme.pink,"키보드 이동 시 이어하기 분홍색");
            Check(!first.transform.Find("포커스 화살표").GetComponent<TMPro.TextMeshProUGUI>().enabled && next.transform.Find("포커스 화살표").GetComponent<TMPro.TextMeshProUGUI>().enabled,"키보드 이동 시 화살표 이동");
            var point=RectTransformUtility.WorldToScreenPoint(app.UICamera,first.transform.TransformPoint(first.GetComponent<RectTransform>().rect.center));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
            Check(first.GetComponentInChildren<TMPro.TextMeshProUGUI>().color==app.Catalog.theme.pink,"마우스 호버 분홍색");
            Check(first.transform.Find("포커스 화살표").GetComponent<TMPro.TextMeshProUGUI>().enabled && first.transform.Find("포커스 화살표").GetComponent<TMPro.TextMeshProUGUI>().color==app.Catalog.theme.pink,"Start 호버 화살표 분홍색");
            Check(!next.transform.Find("포커스 화살표").GetComponent<TMPro.TextMeshProUGUI>().enabled,"마우스 이동 시 선택 화살표 1개 유지");Capture("menu-start");
            yield return MouseClick(panel.Content);Check(app.Windows.GetComponentInChildren<TitleMenuPanel>(),"패널 내부 빈 공간 클릭은 닫지 않음");
            yield return MouseClick(panel.GetComponent<RectTransform>());
            Check(!app.Windows.GetComponentInChildren<TitleMenuPanel>(),"패널 바깥 클릭으로 닫기");
            yield return MouseClick(Buttons().First(b=>b.name=="Option").GetComponent<RectTransform>());yield return new WaitForSeconds(.25f);
            panel=app.Windows.GetComponentInChildren<TitleMenuPanel>();
            Check(panel && panel.GetComponentsInChildren<Slider>().Length==4 && !app.Windows.GetComponentInChildren<FakeWindow>(),"Option 패널과 분리된 4개 설정");
            float volume=app.Save.Options.music;yield return Press(Key.RightArrow);
            Check(app.Save.Options.music>volume && Mathf.Abs(new SaveService().Options.music-app.Save.Options.music)<.001f,"키보드 옵션 조절 및 저장");
            var optionSlider=panel.GetComponentsInChildren<Slider>()[1];
            yield return MouseClick(optionSlider.GetComponent<RectTransform>());yield return Press(Key.RightArrow);
            Check(panel.GetComponentsInChildren<Slider>().All(sl=>sl.transition==Selectable.Transition.None && !sl.GetComponent<TitleMenuTint>() && sl.targetGraphic.canvasRenderer.GetColor()==Color.white) && panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>().All(t=>t.color==app.Catalog.theme.green),"Option 호버·선택 색상 고정, 슬라이더 입력 유지");
            Capture("menu-option");yield return Press(Key.Escape);
            Check(!app.Windows.GetComponentInChildren<TitleMenuPanel>(),"Esc 닫기");
            app.ResetScreen();app.Desktop();app.OpenOptions();yield return null;
            Check(FindWindow("Option"),"바탕화면 옵션 창 기존 동작 유지");
        }
        IEnumerator PolishChecks()
        {
            var legacy=Path.Combine(Application.temporaryCachePath,"CRT-Migration-"+Guid.NewGuid());Directory.CreateDirectory(legacy);
            File.WriteAllText(Path.Combine(legacy,"options.json"),"{\"music\":0.3,\"effects\":0.4,\"filter\":0.37}");
            var migrated=new SaveService(legacy);
            Check(migrated.Options.crtVersion==1 && Mathf.Abs(migrated.Options.curvature-.37f)<.001f && Mathf.Abs(migrated.Options.filter-.37f)<.001f,"이전 단일 필터 설정의 곡률 마이그레이션");
            app.ShowTitle();yield return null;app.OpenOptions();yield return new WaitForSeconds(.3f);
            var panel=app.Windows.GetComponentInChildren<TitleMenuPanel>();var sliders=panel.GetComponentsInChildren<Slider>();
            app.Save.Options.curvature=.2f;app.Save.Options.filter=.4f;app.ApplyOptions();sliders[2].SetValueWithoutNotify(.2f);sliders[3].SetValueWithoutNotify(.4f);
            EventSystem.current.SetSelectedGameObject(sliders[2].gameObject);yield return Press(Key.RightArrow);
            Check(app.Save.Options.curvature>.2f && app.Save.Options.filter==.4f,"곡률 슬라이더가 화면 효과 값을 변경하지 않음");
            float bend=app.Save.Options.curvature;EventSystem.current.SetSelectedGameObject(sliders[3].gameObject);yield return Press(Key.RightArrow);
            Check(app.Save.Options.filter>.4f && app.Save.Options.curvature==bend,"화면 효과 슬라이더가 곡률 값을 변경하지 않음");
            var loaded=new SaveService();Check(loaded.Options.curvature==bend && loaded.Options.filter==app.Save.Options.filter,"두 CRT 설정 디스크 재로드 유지");Capture("polish-options");
            app.Save.Options.curvature=1;app.Save.Options.filter=0;app.ApplyOptions();
            Check(app.Catalog.crt.material.GetVector("_Shape").y==app.Catalog.crt.curvature && app.Catalog.crt.material.GetFloat("_Intensity")==0,"화면 효과 0에서도 곡률 독립 적용");Capture("polish-curve-only");
            app.Save.Options.curvature=0;app.Save.Options.filter=1;app.ApplyOptions();
            Check(app.Catalog.crt.material.GetVector("_Shape").y==0 && app.Catalog.crt.material.GetFloat("_Intensity")==1,"곡률 0에서도 화면 효과 독립 적용");Capture("polish-effects-only");
            app.Save.Options.curvature=.65f;app.Save.Options.filter=.65f;app.ApplyOptions();app.Save.SaveOptions();
            app.ShowTitle();yield return new WaitForSeconds(.1f);yield return MouseClick(Buttons().First(b=>b.name=="Start").GetComponent<RectTransform>());yield return new WaitForSeconds(.25f);
            yield return MouseClick(Buttons().First(b=>b.name=="게임 시작하기").GetComponent<RectTransform>());yield return new WaitForSeconds(.25f);
            Check(!app.Windows.GetComponentInChildren<FakeWindow>() && Buttons().Any(b=>b.name=="취소"),"새 게임 확인도 타이틀 전용 UI");Capture("polish-confirm");
            yield return MouseClick(Buttons().First(b=>b.name=="취소").GetComponent<RectTransform>());yield return new WaitForSeconds(.25f);
            Check(Buttons().Any(b=>b.name=="이어하기") && !app.GetComponent<PrologueController>(),"취소 시 Start 메뉴 복귀");
            yield return MouseClick(Buttons().First(b=>b.name=="게임 시작하기").GetComponent<RectTransform>());yield return new WaitForSeconds(.25f);
            yield return MouseClick(Buttons().First(b=>b.name=="확인").GetComponent<RectTransform>());yield return new WaitForSeconds(.4f);
            Check(app.GetComponent<PrologueController>() && !app.Windows.GetComponentInChildren<TitleMenuPanel>(),"확인 시 미로 시작 및 패널 정리");
            app.ResetScreen();app.Desktop();yield return null;
            var icon=Icons().First(i=>i.Id=="computer");var art=icon.GetComponentsInChildren<Image>().First(i=>i.sprite);var scale=icon.transform.localScale;
            var point=RectTransformUtility.WorldToScreenPoint(app.UICamera,icon.Rect.TransformPoint(icon.Rect.rect.center));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;yield return null;
            Check(icon.transform.localScale==scale && art.color!=Color.white,"아이콘 누름: 크기 고정 및 색상 틴트");Capture("polish-icon-pressed");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
            Check(icon.transform.localScale==scale && art.color==Color.white && FindWindow("내 컴퓨터 - 메모장"),"아이콘 해제: 색상 복원 및 기존 클릭 동작");
        }
        IEnumerator DragMailChecks()
        {
            app.Save.NewGame("Stage01");app.Save.Flag("stage01.wandered");app.Begin(false);yield return new WaitForSeconds(.2f);
            app.Stages.OpenSorting();yield return new WaitForSeconds(.2f);
            yield return Drop("document_0","picture");yield return Icon("picture");yield return new WaitForSeconds(.25f);
            var item=Icons().Last(i=>i.Id=="document_0");var original=item.Rect.anchoredPosition;
            yield return MouseClick(item.Rect);Check(app.Stages.Sorting.Location("document_0")=="picture","클릭으로는 폴더에서 꺼내지 않음");
            FindWindow("일정.txt - 메모장").Close();yield return null;
            Vector2 Position()=>RectTransformUtility.WorldToScreenPoint(app.UICamera,item.Rect.TransformPoint(item.Rect.rect.center));
            Vector2 ScreenPoint(float x,float y)=>RectTransformUtility.WorldToScreenPoint(app.UICamera,app.Root.TransformPoint(new Vector3(x,-y,0)));
            yield return MouseDrag(Position(),ScreenPoint(600,450));
            Check(app.Stages.Sorting.Location("document_0")=="picture" && item.Rect.anchoredPosition==original,"폴더 창 안 드롭: 원위치 복귀");
            yield return MouseDrag(Position(),ScreenPoint(100,650));
            Check(app.Stages.Sorting.Location("document_0")=="picture" && item.Rect.anchoredPosition==original,"바탕화면 드롭 금지 및 원위치 복귀");
            yield return MouseDrag(Position(),ScreenPoint(400,670));yield return null;
            Check(app.Stages.Sorting.Location("document_0")=="" && Icons().Count(i=>i.Id=="document_0")==1,"창 밖 작업 폴더 드롭: 파일 1개만 복귀");
            Check(FindWindow("사진첩"),"파일 꺼낸 후 하위 폴더 유지");Capture("drag-folder-return");
            foreach(var w in app.Windows.GetComponentsInChildren<FakeWindow>())w.Close();yield return null;
            app.Save.SetHidden("mail",true);app.gameObject.AddComponent<MailPuzzle>().Open();yield return new WaitForSeconds(.3f);
            yield return Drop("1.txt","mail");Check(!app.Save.Progress.flags.Contains("stage01.mail"),"가짜 편지는 원위치 및 미완료 유지");
            yield return MouseClick(Icons().First(i=>i.Id=="5.txt").Rect);yield return new WaitForSeconds(.2f);
            WindowApps.Note("5.txt","동명이지만 다른 문서");
            for(int i=1;i<=4;i++) WindowApps.Note(i+".txt","열린 편지","mail:"+i+".txt");
            FindWindow("남겨진 편지").Focus();yield return null;
            yield return Drop("5.txt","mail");yield return new WaitForSeconds(.15f);
            Check(!app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key.StartsWith("note:mail:")) && app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="note:5.txt"),"열린 편지 1~5.txt 모두 닫기, 동명 일반 문서는 유지");
            var mailboxIcon=Icons().First(i=>i.Id=="mailbox");
            Check(!mailboxIcon.enabled && !mailboxIcon.GetComponent<Image>().raycastTarget,"배달 완료 즉시 우체통 입력 차단");
            var particles=app.Windows.GetComponentsInChildren<PixelDriftGraphic>();
            Check(app.Save.Progress.flags.Contains("stage01.mail") && particles.Length==4,"진짜 편지 배달 저장 및 남은 4개 편지 픽셀 연출");
            Check(!Icons().Any(i=>i.Id=="5.txt") && particles.All(p=>!p.raycastTarget),"배달한 편지 제거 및 입자 입력 차단");
            Canvas.ForceUpdateCanvases();var mesh=particles[0].canvasRenderer.GetMesh();var before=mesh.vertices;
            Check(before.Length>1000,"원본 아이콘을 다수 픽셀 조각으로 분할");Capture("mail-pixels-early");
            yield return new WaitForSeconds(.85f);Canvas.ForceUpdateCanvases();mesh=particles[0].canvasRenderer.GetMesh();
            Check(mesh.vertices.Length>0 && mesh.vertices.Where((v,i)=>i<before.Length && v.y>before[i].y).Count()>100,"픽셀 입자가 위로 이동");
            Check(mesh.colors32.Select(c=>c.a).Distinct().Count()>10,"입자별 지연·수명에 따른 서로 다른 투명도");Capture("mail-pixels-floating");
            FindWindow("남겨진 편지").GetComponentsInChildren<Button>(true).First(b=>b.name=="설정 · 숨김파일 표시").onClick.Invoke();yield return null;
            Check(app.Windows.GetComponentsInChildren<PixelDriftGraphic>().Length==4,"설정 토글로 소멸 연출이 끊기지 않음");
            yield return new WaitForSeconds(2.6f);
            Check(!app.Windows.GetComponentInChildren<PixelDriftGraphic>() && !Icons().Any(i=>i.Id.EndsWith(".txt")),"연출 완료 후 편지·입자 정리");Capture("mail-pixels-done");
            FindWindow("남겨진 편지").Close();yield return null;app.gameObject.AddComponent<MailPuzzle>().Open();yield return new WaitForSeconds(.2f);
            Check(!Icons().Any(i=>i.Id.EndsWith(".txt")),"재열기 후 배달 완료 유지");
            var completed=Icons().First(i=>i.Id=="mailbox");int sounds=0;Action<string> audio=key=>sounds++;app.Audio.Played+=audio;
            yield return MouseClick(completed.Rect);app.Audio.Played-=audio;
            Check(!completed.enabled && !completed.GetComponent<Image>().raycastTarget && sounds==0,"재열기 후 완료 우체통 클릭: 효과음·반응 없음");
        }
        IEnumerator FileOpenChecks()
        {
            app.Save.NewGame("Stage01");app.Save.Flag("stage01.wandered");app.Begin(false);yield return null;
            foreach(var category in new[]{"document","picture","music"})
            {
                var record=app.Stages.Sorting.Files.First(f=>f.category==category && !f.hidden);app.Stages.Sorting.Move(record.id,category);
                app.Stages.OpenSorting();yield return new WaitForSeconds(.2f);yield return Icon(category);yield return new WaitForSeconds(.2f);
                yield return MouseClick(Icons().First(i=>i.Id==record.id).Rect);yield return new WaitForSeconds(.2f);
                string title=category=="document"?record.name+" - 메모장":category=="picture"?record.name+" - 이미지 뷰어":"알림";
                Check(FindWindow(title) && app.Stages.Sorting.Location(record.id)==category,"폴더 안 클릭 열기 및 위치 유지: "+category);
                foreach(var w in app.Windows.GetComponentsInChildren<FakeWindow>())w.Close();yield return null;
            }
        }
        IEnumerator LoadingChecks()
        {
            foreach(var w in app.Windows.GetComponentsInChildren<FakeWindow>())w.Close();yield return null;
            int sounds=0;Action<string> audio=key=>{if(key=="sfx_ding")sounds++;};app.Audio.Played+=audio;
            app.Stages.RequestClear("door.exe");float deadline=Time.realtimeSinceStartup+18;int lines=0;var times=new List<float>();bool fonts=true,captured=false;int textFrames=0;
            while(app.Stages.Current.stageId!="Stage02" && Time.realtimeSinceStartup<deadline)
            {
                var texts=app.Modal.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                if(texts.Length>0){textFrames++;fonts&=texts.All(t=>t.font==app.Catalog.theme.titleFont);}
                var label=texts.FirstOrDefault(t=>t.text.StartsWith("system load success"));
                if(label&&!captured){Capture("console-neo-font");captured=true;}
                if(label) {int count=label.text.Split(new[]{"system loading"},StringSplitOptions.None).Length-1;if(count>lines) {times.Add(Time.realtimeSinceStartup);lines=count;}}
                yield return null;
            }
            app.Audio.Played-=audio;
            Check(fonts&&textFrames>0,"CMD 제목·진행률·연속 출력 모두 타이틀 Neo 폰트 공유");
            Check(lines==42 && sounds==43,"system loading 42줄 각각 띠링 호출(시작 완료음 포함 총 43회)");
            Check(times.Count==42 && times[5]-times[0]>(times[41]-times[36])*2,"실제 출력 간격: 처음보다 마지막이 2배 이상 빠름");
            Check(app.Stages.Current.stageId=="Stage02","가속 출력 이후 다음 스테이지 전환");
        }
        IEnumerator ExpressionChecks()
        {
            app.ResetScreen();app.Desktop();
            var mapping=new ExpressionCatalog{characters=new[]{
                new CharacterExpressions{id="girl",side="right",speakerAliases=new[]{"V양","독백"},expressions=new[]{new ExpressionEntry{id="normal",spriteKey="portrait_v"},new ExpressionEntry{id="happy",spriteKey="portrait_folder"},new ExpressionEntry{id="broken",spriteKey="missing_sprite"}}},
                new CharacterExpressions{id="folder",side="left",speakerAliases=new[]{"폴더"},expressions=new[]{new ExpressionEntry{id="normal",spriteKey="portrait_folder"},new ExpressionEntry{id="sad",spriteKey="portrait_v"}}},
                new CharacterExpressions{id="guide",side="left",speakerAliases=new[]{"안내인"},expressions=new[]{new ExpressionEntry{id="normal",spriteKey="portrait_v"},new ExpressionEntry{id="surprised",spriteKey="portrait_folder"}}}}};
            var mapJson=new TextAsset(JsonUtility.ToJson(mapping));app.Dialogue.LoadExpressions(mapJson);
            Check(app.Dialogue.SetExpression("V양","happy"),"화자 별칭으로 캐릭터 표정 매핑");
            var script=new DialogueDocument{sequences=new[]{new DialogueSequence{id="test",lines=new[]{
                new DialogueLine{speaker="V양",characterId="girl",expressionId="happy",portraitKey="portrait_v",text="표정 데이터 검사"},
                new DialogueLine{speaker="폴더",characterId="folder",expressionId="sad",text="왼쪽 표정 검사"},
                new DialogueLine{speaker="안내인",characterId="guide",expressionId="surprised",text="새 캐릭터 검사"}}}}};
            var scriptJson=new TextAsset(JsonUtility.ToJson(script));app.Dialogue.Load(scriptJson);app.Dialogue.Play(()=>{},"test");yield return null;
            var right=app.Modal.GetComponentsInChildren<Image>().First(i=>i.name=="오른쪽 여자아이");var left=app.Modal.GetComponentsInChildren<Image>().First(i=>i.name=="왼쪽 NPC");
            Check(right.sprite==app.Catalog.sprites.Get("portrait_folder"),"대사 expressionId 적용 및 기존 portraitKey보다 우선");
            Check(app.Dialogue.SetExpression("girl","normal") && right.sprite==app.Catalog.sprites.Get("portrait_v"),"SetExpression 호출 즉시 오른쪽 표정 교체");
            Check(!app.Dialogue.SetExpression("girl","unknown") && right.sprite==app.Catalog.sprites.Get("portrait_v"),"미등록 표정은 기본 normal로 복원");
            Check(!app.Dialogue.SetExpression("girl","broken") && right.sprite==app.Catalog.sprites.Get("portrait_v"),"이미지가 누락된 매핑은 기본 표정으로 복원");
            Check(!app.Dialogue.SetExpression("missing","happy") && left.sprite==app.Catalog.sprites.Get("portrait_folder"),"미등록 캐릭터는 기존 CG 유지");
            yield return Press(Key.Space);yield return Press(Key.Space);yield return null;
            Check(left.sprite==app.Catalog.sprites.Get("portrait_v") && right.sprite==app.Catalog.sprites.Get("portrait_v"),"다음 대사의 왼쪽 캐릭터만 표정 변경");
            yield return Press(Key.Space);yield return Press(Key.Space);yield return null;
            Check(left.sprite==app.Catalog.sprites.Get("portrait_folder"),"데이터로 추가한 NPC 표정 적용");
            app.Dialogue.SetExpression("folder","sad");
            Check(left.sprite==app.Catalog.sprites.Get("portrait_folder"),"현재 표시하지 않는 캐릭터 변경이 다른 NPC를 덮지 않음");
            yield return FinishDialogue();app.Dialogue.LoadExpressions(Resources.Load<TextAsset>("CharacterExpressions"));
            Destroy(mapJson);Destroy(scriptJson);
            yield return LayoutChecks();
        }
        IEnumerator LayoutChecks()
        {
            app.Save.Options.music=.22f;app.Save.Options.effects=.44f;app.Save.Options.filter=.66f;
            foreach(var view in new[]{"explorer","desktop","mail"}) app.Save.SetHidden(view,true);
            var loaded=new SaveService();Check(loaded.Hidden("explorer") && loaded.Hidden("desktop") && loaded.Hidden("mail"),"기존 게임 재로드에서는 숨김 표시 유지");
            app.Save.NewGame("Stage01");loaded=new SaveService();
            Check(app.Save.Options.hiddenViews.Count==0 && loaded.Options.hiddenViews.Count==0,"새 게임 시 모든 구역의 숨김 표시 꺼짐 및 디스크 저장");
            Check(loaded.Options.music==.22f && loaded.Options.effects==.44f && loaded.Options.filter==.66f,"새 게임에서도 음악·효과음·필터 유지");
            FakeWindow.TryCreate(app.Windows,"sorting","설정 복원 검사",out var window);window.AddHiddenToggle(()=>{});
            Check(!window.ShowHidden && !window.Content.GetComponentsInChildren<RectTransform>(true).First(r=>r.name=="체크 표시").gameObject.activeSelf,"새 게임 이후 새 창의 숨김 체크 꺼짐");window.Close();yield return null;
            app.ResetScreen();app.Desktop();
            var fixture=new TextAsset(JsonUtility.ToJson(new DialogueDocument{sequences=new[]{new DialogueSequence{id="LAYOUT",lines=new[]{new DialogueLine{speaker="폴더",text="여기는 왼쪽 자리입니다.",portraitKey="portrait_folder"},new DialogueLine{speaker="V양",text="저는 오른쪽에 있어요.",portraitKey="portrait_v"},new DialogueLine{speaker="안내인",text="다른 NPC도 왼쪽에 표시됩니다.",portraitKey="portrait_folder"}}}}}));
            app.Dialogue.Load(fixture);app.Dialogue.Play(()=>{},"LAYOUT");yield return new WaitForSeconds(.2f);
            var npc=app.Modal.GetComponentsInChildren<Image>().First(i=>i.name=="왼쪽 NPC");
            var girl=app.Modal.GetComponentsInChildren<Image>().First(i=>i.name=="오른쪽 여자아이");
            Check(npc.rectTransform.anchoredPosition==new Vector2(32,-190) && girl.rectTransform.anchoredPosition==new Vector2(667,-180) && npc.sprite==app.Catalog.sprites.Get("portrait_folder") && girl.sprite==app.Catalog.sprites.Get("portrait_v"),"폴더/NPC 왼쪽, 여자아이 오른쪽 좌표 및 이미지");
            Check(npc.color==Color.white && girl.color!=Color.white,"폴더 발화 시 왼쪽 강조");Capture("layout-npc-left");
            yield return Press(Key.Space);yield return Press(Key.Space);yield return new WaitForSeconds(.15f);
            Check(girl.color==Color.white && npc.color!=Color.white && girl.sprite==app.Catalog.sprites.Get("portrait_v"),"여자아이 발화·표정 교체는 오른쪽");Capture("layout-girl-right");
            yield return Press(Key.Space);yield return Press(Key.Space);yield return new WaitForSeconds(.15f);
            Check(npc.color==Color.white && girl.color!=Color.white && npc.sprite==app.Catalog.sprites.Get("portrait_folder"),"다른 NPC의 발화·표정 교체도 왼쪽");
            yield return FinishDialogue();Destroy(fixture);
        }
        static readonly Vector2Int[] directions={Vector2Int.right,Vector2Int.down,Vector2Int.left,Vector2Int.up};
        List<Vector2Int> MazePath(PrologueController maze,Vector2Int target)
        {
            var origin=maze.Cell;var queue=new Queue<Vector2Int>();queue.Enqueue(origin);
            var previous=new Dictionary<Vector2Int,Vector2Int>{{origin,origin}};
            while(queue.Count>0)
            {
                var cell=queue.Dequeue();if(cell==target) break;
                foreach(var dir in directions)
                {
                    var next=cell+dir;if(!maze.Walkable(next.x,next.y) || previous.ContainsKey(next)) continue;
                    previous.Add(next,cell);queue.Enqueue(next);
                }
            }
            if(!previous.ContainsKey(target)) throw new Exception("도달할 수 없는 미로 타일: "+target);
            var path=new List<Vector2Int>();for(var cell=target;cell!=origin;cell=previous[cell]) path.Add(cell);
            path.Add(origin);path.Reverse();return path;
        }
        bool cameraQA;int cameraFrames,centeredFrames;
        void VerifyCamera(PrologueController maze)
        {
            if(!cameraQA)return;
            var min=maze.CameraOffset+PrologueController.Origin*maze.ViewScale;
            var max=min+new Vector2(maze.Zone.rows[0].Length,maze.Zone.rows.Length)*PrologueController.Tile*maze.ViewScale;
            if(min.x>.01f || min.y>112.01f || max.x<1023.99f || max.y<695.99f)throw new Exception("카메라가 맵 밖을 노출함");
            var center=maze.PlayerScreenCenter;
            if(min.x<-.1f && max.x>1024.1f && Mathf.Abs(center.x-512)>.1f)throw new Exception("수평 중앙 추적 실패");
            if(min.y<111.9f && max.y>696.1f && Mathf.Abs(center.y-384)>.1f)throw new Exception("수직 중앙 추적 실패");
            if(Vector2.Distance(center,new Vector2(512,384))<.1f)centeredFrames++;
            if(cameraFrames++==200)Capture("camera-follow");
        }
        IEnumerator WalkMaze(PrologueController maze,Vector2Int target)
        {
            foreach(var cell in MazePath(maze,target))
            {
                var point=PrologueController.Center(cell.x,cell.y);float deadline=Time.realtimeSinceStartup+3;
                while(Vector2.Distance(maze.Position,point)>3.5f)
                {
                    if(Time.realtimeSinceStartup>deadline) throw new Exception("키보드 미로 이동 실패: "+cell+" 위치 "+maze.Position);
                    var delta=point-maze.Position;
                    Key key=Mathf.Abs(delta.x)>2.5f?(delta.x>0?Key.D:Key.A):(delta.y>0?Key.S:Key.W);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;
                    if(maze.Blocked(maze.Position)) throw new Exception("이동 중 벽 관통");
                    VerifyCamera(maze);
                }
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            }
        }
        IEnumerator MazePortal(PrologueController maze,string id)
        {
            var portal=maze.Zone.portals.First(p=>p.id==id);yield return WalkMaze(maze,new Vector2Int(portal.x,portal.y));
            var position=maze.Position;yield return Press(Key.Space);yield return new WaitForSeconds(.06f);
            Check(maze.Transitioning && maze.FadeAlpha>0 && maze.FadeAlpha<1,"구역 전환 페이드: "+id);
            yield return Hold(Key.D,.05f);
            Check(maze.Position==position,"페이드 중 이동 잠금: "+id);
            float deadline=Time.realtimeSinceStartup+3;
            while(maze && maze.Transitioning && Time.realtimeSinceStartup<deadline) yield return null;
            if(!string.IsNullOrEmpty(portal.target))
            {
                var arrival=maze.Zone.portals.First(p=>p.id==portal.arrival);
                Check(maze.Zone.id==portal.target && !maze.Blocked(maze.Position) && Vector2.Distance(maze.Position,PrologueController.Center(arrival.spawnX,arrival.spawnY))<1,"출입구 옆 안전 위치: "+portal.target);
                Capture("maze-"+maze.Zone.id);
            }
        }
        IEnumerator MazeChecks()
        {
            yield return null;
            var maze=app.GetComponent<PrologueController>();
            Check(maze && maze.Zone.id=="paper" && !maze.Transitioning && !app.ScreenRoot.Find("메뉴바") && !Icons().Any(i=>i.Id=="computer"),"게임 시작 즉시 미로 표시: 바탕화면 없음");
            Capture("maze-paper");var spawn=maze.Position;
            yield return Hold(Key.W,.45f);yield return Hold(Key.A,.45f);
            Check(maze.Cell==new Vector2Int(1,1) && !maze.Blocked(maze.Position) && maze.Position!=spawn,"시작 즉시 조작 가능, 바깥 경계 충돌");
            foreach(var expected in new[]{"paper","water","signal"})
            {
                Check(maze.Zone.id==expected,"실제 키보드 이동으로 구역 도달: "+expected);
                var floors=new List<Vector2Int>();
                for(int y=1;y<maze.Zone.rows.Length-1;y++) for(int x=1;x<maze.Zone.rows[y].Length-1;x++) if(maze.Walkable(x,y)) floors.Add(new Vector2Int(x,y));
                Func<Vector2Int,int> degree=c=>directions.Count(d=>maze.Walkable(c.x+d.x,c.y+d.y));
                var dead=floors.Where(c=>degree(c)==1 && !maze.Zone.portals.Any(p=>p.x==c.x&&p.y==c.y)).OrderBy(c=>MazePath(maze,c).Count).First();
                var origin=maze.Cell;yield return WalkMaze(maze,dead);yield return WalkMaze(maze,origin);
                Check(floors.Any(c=>degree(c)>=3) && maze.Cell==origin,"갈림길·막다른 길 탐색 및 돌아오기: "+expected);
                var wallCell=floors.First(c=>directions.Any(d=>c.x+d.x>0 && c.x+d.x<20 && c.y+d.y>0 && c.y+d.y<12 && !maze.Walkable(c.x+d.x,c.y+d.y)));
                yield return WalkMaze(maze,wallCell);
                var direction=directions.First(d=>!maze.Walkable(wallCell.x+d.x,wallCell.y+d.y));
                yield return Hold(direction.x>0?Key.D:direction.x<0?Key.A:direction.y>0?Key.S:Key.W,.45f);
                Check(maze.Cell==wallCell && !maze.Blocked(maze.Position),"구역 장애물 충돌: "+expected);
                if(expected!="signal") yield return MazePortal(maze,expected=="paper"?"to-water":"to-signal");
            }
            yield return MazePortal(maze,"to-paper");
            Check(maze.Zone.id=="paper","세 구역 순환 경로 C → A 복귀");
            yield return MazePortal(maze,"to-signal");
            yield return DoorCaptureChecks(maze);
            Check(!app.GetComponent<PrologueController>() && app.Save.Progress.flags.Contains("stage01.wandered") && Icons().Any(i=>i.Id=="npc"),"실제 출구 도달 후 기존 게임 연결");
        }
        IEnumerator DoorCaptureChecks(PrologueController maze)
        {
            var exit=maze.Zone.portals.First(p=>p.id=="exit");
            yield return WalkMaze(maze,new Vector2Int(exit.x,exit.y-2));yield return Press(Key.Space);
            Check(!maze.Transitioning && !maze.CaptureSequence,"출구에서 멀리 떨어진 Space는 연출 미발동");
            yield return WalkMaze(maze,new Vector2Int(exit.x,exit.y));var position=maze.Position;
            yield return Press(Key.Space);var sequence=maze.CaptureSequence;
            Check(sequence && sequence.Phase==CapturePhase.Blackout && sequence.Layer.GetComponent<UnityEngine.UI.Image>().color==Color.black && sequence.Layer.childCount==2 && !app.CursorLayer.gameObject.activeSelf,"Space 직후 불투명 암전: 문·주인공만 표시");
            Capture("capture-01-blackout");
            app.Dialogue.Play(()=>{},"S1_INTRO");Check(!app.Dialogue.Active,"문 연출 중 대화 이벤트 차단");
            var observed=new HashSet<CapturePhase>();bool locked=true,single=true,morph=false,turned=false,attached=false,offscreen=false;
            int frames=0,steps=0;float deadline=Time.realtimeSinceStartup+12;Vector3 retreatScale=Vector3.zero;
            while(maze && Time.realtimeSinceStartup<deadline)
            {
                if(sequence)
                {
                    var phase=sequence.Phase;
                    if(observed.Add(phase) && phase!=CapturePhase.Morph && phase!=CapturePhase.Complete) Capture("capture-phase-"+phase);
                    locked &= maze.Transitioning && maze.Position==position;
                    single &= app.GetComponents<DoorCaptureSequence>().Length==1;
                    steps=Mathf.Max(steps,sequence.BackSteps);
                    if(phase==CapturePhase.Morph && sequence.MorphProgress>.4f && sequence.MorphProgress<.8f && !morph)
                    {morph=true;Capture("capture-02-morph");}
                    if(phase==CapturePhase.Recoil) retreatScale=sequence.Actor.GetComponentsInChildren<UnityEngine.UI.Image>().Last().rectTransform.localScale;
                    if(phase==CapturePhase.Run) turned=retreatScale.x!=sequence.Actor.GetComponentsInChildren<UnityEngine.UI.Image>().Last().rectTransform.localScale.x;
                    if(phase==CapturePhase.Drag) attached |= sequence.Actor.parent==sequence.Pursuer;
                    if(phase==CapturePhase.FinalBlack) offscreen=sequence.Pursuer.anchoredPosition.x>1100 && sequence.Actor.parent==sequence.Pursuer;
                }
                // 연출 내내 이동 키와 Space 연타를 넣어 중복/개입을 검사한다.
                InputSystem.QueueStateEvent(keyboard,frames++%4<2?new KeyboardState(Key.Space,Key.D):new KeyboardState(Key.A));
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check(morph && observed.Contains(CapturePhase.Recoil),"흰 손잡이의 중간 변형 → 완성 커서");
            Check(steps==3 && turned && observed.Contains(CapturePhase.Run),"커서를 바라보는 세 걸음 뒷걸음질 후 등지고 도주");
            Check(observed.Contains(CapturePhase.Lunge) && attached,"커서 돌진 후 주인공 포획·동일 부모 이동");
            Check(offscreen && observed.Contains(CapturePhase.FinalBlack),"커서와 주인공 함께 화면 밖 이동 후 암전");
            Check(locked && single,"이동 키 및 Space 연타에도 조작 잠금·단일 실행");
            Check(!maze && !app.CinematicActive && app.CursorLayer.gameObject.activeSelf && !app.Modal.Find("문 포획 암전"),"바탕화면 전환 후 입력 차단막·커서 상태 복원");
            Capture("capture-07-desktop");
        }
        IEnumerator CtrlChecks()
        {
            var fixture=new TextAsset(JsonUtility.ToJson(new DialogueDocument{sequences=new[]{new DialogueSequence{id="QA",lines=Enumerable.Range(0,8).Select(i=>new DialogueLine{speaker="QA",text=new string((char)('가'+i),100)}).ToArray()}}}));
            app.Dialogue.Load(fixture);app.Dialogue.Play(()=>{},"QA");yield return new WaitForSeconds(.2f);
            var body=app.Modal.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Last();
            Check(body.maxVisibleCharacters>0 && body.maxVisibleCharacters<100,"Ctrl 미입력: 원래 타이핑 속도");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl));
            float revealDeadline=Time.realtimeSinceStartup+.5f;while(body.maxVisibleCharacters<100&&Time.realtimeSinceStartup<revealDeadline)yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());string stoppedLine=body.text;yield return new WaitForSeconds(.25f);
            Check(app.Dialogue.Active && body.text==stoppedLine && body.maxVisibleCharacters==100,"Ctrl 누르면 즉시 출력, 떼면 자동 넘김 중단");
            yield return Press(Key.Space);yield return new WaitForSeconds(.15f);
            Check(body.text!=stoppedLine && body.maxVisibleCharacters>0 && body.maxVisibleCharacters<100,"Ctrl 해제 후 다음 대사 원래 출력 속도 복원");
            yield return Hold(Key.RightCtrl,1f);
            Check(!app.Dialogue.Active,"오른쪽 Ctrl 유지: 대사 자동 넘김 및 종료");
            var state=app.Stages.State;int windows=app.Windows.childCount;
            yield return Hold(Key.LeftCtrl,.25f);
            Check(!app.Dialogue.Active && app.Stages.State==state && app.Windows.childCount==windows,"대화 밖 Ctrl은 상태 변경 없음");
            app.Dialogue.Load(app.Stages.Current.dialogue);Destroy(fixture);
        }
        IEnumerator ClockChecks()
        {
            var clock=app.ScreenRoot.Find("시스템 시계").GetComponent<TMPro.TextMeshProUGUI>();
            var samples=new List<string>();float began=Time.realtimeSinceStartup;bool matches=true;
            float duration=Array.IndexOf(Environment.GetCommandLineArgs(),"-coLongClockQA")>=0?180:2;
            Debug.Log("시스템 시계 검증 시작: "+duration+"초");if(duration==180) Capture("clock-start");
            while(Time.realtimeSinceStartup-began<duration)
            {
                var before=DateTime.Now;yield return new WaitForEndOfFrame();var after=DateTime.Now;
                string a=before.ToString("tt hh:mm",System.Globalization.CultureInfo.InvariantCulture),b=after.ToString("tt hh:mm",System.Globalization.CultureInfo.InvariantCulture);
                matches &= clock.text==a || clock.text==b;
                samples.Add($"{after:O}\t{clock.text}");
                File.WriteAllText(Path.Combine(output,"clock-live.txt"),samples.Last());
                yield return new WaitForSecondsRealtime(.5f);
            }
            if(duration==180) Capture("clock-end");File.WriteAllLines(Path.Combine(output,duration==180?"clock-samples.txt":"clock-regression.txt"),samples);
            Check(matches && samples.Count>=(duration==180?300:3),"시계 시스템 시각 일치 ("+duration+"초)");
        }
        IEnumerator SingleWindowChecks()
        {
            var icon=Icons().First(i=>i.Id=="computer").Rect;
            for(int i=0;i<3;i++) yield return MouseClick(icon);
            yield return new WaitForSeconds(.2f);
            var original=FindWindow("내 컴퓨터 - 메모장");
            Check(original && app.Windows.GetComponentsInChildren<FakeWindow>().Count(w=>w.Key=="note:내 컴퓨터")==1,"같은 아이콘 3회 클릭: 창 1개");
            var drag=original.GetComponentInChildren<WindowDrag>();var old=original.Rect.anchoredPosition;
            var point=RectTransformUtility.WorldToScreenPoint(app.UICamera,drag.transform.TransformPoint(new Vector3(100,-15,0)));
            yield return MouseDrag(point,point+new Vector2(35,25));
            Check(original.Rect.anchoredPosition!=old,"창 드래그 이동");
            int children=original.Content.childCount;
            yield return MouseClick(Buttons().First(b=>b.name=="옵션").GetComponent<RectTransform>());
            yield return new WaitForSeconds(.2f);
            Check(FindWindow("Option") && original,"다른 종류 창은 동시에 열림");
            yield return MouseClick(Buttons().First(b=>b.name=="열기").GetComponent<RectTransform>());
            var selected=EventSystem.current.currentSelectedGameObject;
            Check(FindWindow("내 컴퓨터 - 메모장")==original && app.Windows.GetComponentsInChildren<FakeWindow>().Count(w=>w.Key==original.Key)==1 && original.transform.GetSiblingIndex()==app.Windows.childCount-1 && selected && selected.transform.IsChildOf(original.transform),"아이콘 → 메뉴: 기존 창 1개, 맨 앞 및 포커스");
            Check(original.Content.childCount==children,"재호출 시 창 내용 중복 생성 없음");
            Check(app.Windows.GetComponentsInChildren<WindowDrag>().All(d=>d.GetComponentsInChildren<Button>().Length==1 && d.GetComponentInChildren<Button>().name=="X"),"제목 표시줄은 X 버튼만 표시");
            Capture("single-window-focus");
            yield return MouseClick(original.GetComponentsInChildren<Button>().First(b=>b.name=="X").GetComponent<RectTransform>());
            yield return MouseClick(icon);yield return new WaitForSeconds(.2f);
            var reopened=FindWindow("내 컴퓨터 - 메모장");
            Check(!original && reopened && app.Windows.GetComponentsInChildren<FakeWindow>().Count(w=>w.Key=="note:내 컴퓨터")==1,"X 닫기 → 아이콘 다시 클릭: 새 창 정상 생성");
            reopened.Close();app.OpenComputer();
            Check(FindWindow("내 컴퓨터 - 메모장")!=reopened && app.Windows.GetComponentsInChildren<FakeWindow>().Count(w=>w.Key=="note:내 컴퓨터")==1,"닫은 같은 프레임에도 다시 열기 가능");
            foreach(var window in app.Windows.GetComponentsInChildren<FakeWindow>()) window.Close();
            yield return null;
        }
        void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            int width=Screen.width,height=Screen.height;var rt=new RenderTexture(width,height,24);rt.Create();
            var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt};
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(app.UICamera,request);
            var old=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=old;Destroy(image);rt.Release();Destroy(rt);
        }
        FakeWindow FindWindow(string name)=>app.Windows.GetComponentsInChildren<FakeWindow>().FirstOrDefault(w=>w.name==name);
        FakeIcon[] Icons()=>app.Root.GetComponentsInChildren<FakeIcon>();
        Button[] Buttons()=>app.Root.GetComponentsInChildren<Button>();
        PointerEventData Pointer(Vector2 p)=>new(EventSystem.current){position=p,button=PointerEventData.InputButton.Left};
        IEnumerator Icon(string id)
        {
            var icon=Icons().LastOrDefault(i=>i.Id==id);if(!icon)throw new Exception("아이콘 없음: "+id);
            icon.OnPointerClick(Pointer(Vector2.zero));yield return null;
        }
        IEnumerator ClickButton(string name) {if(name=="설정 · 숨김파일 표시"){yield return MouseClick((RectTransform)app.Windows.GetComponentsInChildren<Button>(true).Last(b=>b.name==name).transform);yield break;}var b=Buttons().LastOrDefault(b=>b.name==name);if(!b)throw new Exception("버튼 없음: "+name);b.onClick.Invoke();yield return null;}
        IEnumerator Press(Key key) {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;}
        IEnumerator MouseClick(RectTransform rect)
        {
            // Old regression callers now exercise the relocated menu item through the real UI.
            if(rect.name=="설정 · 숨김파일 표시"&&rect.GetComponentInParent<FolderView>())
            {
                yield return MouseClick((RectTransform)rect.GetComponentInParent<FakeWindow>().Toolbar.Find("보기"));
                yield return MouseClick((RectTransform)app.Modal.GetComponentsInChildren<Button>().Single(b=>b.name=="숨김파일 표시").transform);yield break;
            }
            Canvas.ForceUpdateCanvases();var point=RectTransformUtility.WorldToScreenPoint(app.UICamera,rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
        }
        IEnumerator Hold(Key key,float time) {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSeconds(time);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
        IEnumerator FinishDialogue() {int limit=0;while(app.Dialogue.Active && limit++<150)yield return Press(Key.Space);yield return null;}
        IEnumerator Drop(string id,string category)
        {
            var icon=Icons().Last(i=>i.Id==id);var target=icon.transform.parent.GetComponentsInChildren<DropTarget>().First(t=>t.Category==category);
            // 이벤트 함수를 직접 호출하지 않고 마우스 이동·버튼 상태를 입력한다.
            yield return new WaitForSeconds(.18f);Canvas.ForceUpdateCanvases();
            var start=RectTransformUtility.WorldToScreenPoint(app.UICamera,icon.Rect.TransformPoint(icon.Rect.rect.center));
            var targetRect=(RectTransform)target.transform;
            var end=RectTransformUtility.WorldToScreenPoint(app.UICamera,targetRect.TransformPoint(targetRect.rect.center));
            yield return MouseDrag(start,end);
        }
        bool dragLayerQA;int dragLayerFrames;
        void VerifyDragLayer()
        {
            if(!dragLayerQA)return;
            var source=Icons().FirstOrDefault(i=>i.DragVisual);if(!source)return;
            var visual=source.DragVisual;
            if(!visual.IsChildOf(app.DragLayer) || app.DragLayer.GetSiblingIndex()<=app.Windows.GetSiblingIndex() || app.DragLayer.GetSiblingIndex()<=app.Modal.GetSiblingIndex())throw new Exception("드래그 아이콘 상위 레이어 실패");
            if(Vector3.Distance(visual.position,source.Rect.position)>.1f || source.GetComponent<CanvasGroup>().alpha!=0)throw new Exception("드래그 표시 위치/원본 숨김 실패");
            if(visual.GetComponentsInChildren<Graphic>().Any(g=>g.raycastTarget) || visual.GetComponent<CanvasGroup>().blocksRaycasts)throw new Exception("드래그 표시가 드롭 판정을 가림");
            if(!app.DragLayer.GetComponent<RectMask2D>() || app.DragLayer.anchoredPosition.y!=-38)throw new Exception("상단 메뉴 제외 마스크 실패");
            if(++dragLayerFrames==24)Capture("drag-layer-over-window");
        }
        IEnumerator MouseDrag(Vector2 start,Vector2 end)
        {
            InputSystem.QueueStateEvent(mouse,new MouseState{position=start});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=start,buttons=1});yield return null;yield return null;
            for(int i=1;i<=12;i++){InputSystem.QueueStateEvent(mouse,new MouseState{position=Vector2.Lerp(start,end,i/12f),buttons=1});yield return null;VerifyDragLayer();}
            InputSystem.QueueStateEvent(mouse,new MouseState{position=end});yield return null;yield return null;
        }
        static void Quit(int code)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
            #else
            Application.Quit(code);
            #endif
        }
        void OnDestroy() {Application.logMessageReceived-=Log;}
    }
}
#endif
