using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed class DialogueSystem : MonoBehaviour
    {
        public bool Active { get; private set; }
        // Special-route presentation is scoped to a sequence, never stored in the shared expression catalog.
        public string SpecialPresentation;
        public bool Paused;
        public Action<string> LineRevealed;
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        public enum SkipOverride { Default,ForceOn,ForceOff }
        [Tooltip("개발 전용: 저장된 해금 상태를 바꾸지 않고 Ctrl 스킵을 강제 테스트")]
        public SkipOverride debugSkipOverride;
        #endif
        public bool CanSkipDialogue()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(debugSkipOverride!=SkipOverride.Default)return debugSkipOverride==SkipOverride.ForceOn;
            #endif
            return GameApp.Instance.Save.CanSkipDialogue();
        }
        bool Skipping => Active && !Paused && CanSkipDialogue() && Keyboard.current!=null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
        DialogueDocument document;bool dialogueMusicPaused;
        CanvasGroup preservedWindows;bool previousInteractable,previousRaycasts;
        void RestoreWindowInput(){if(!preservedWindows)return;preservedWindows.interactable=previousInteractable;preservedWindows.blocksRaycasts=previousRaycasts;preservedWindows=null;}
        public Action<string> FlagCommand;
        public string CurrentSequence {get;private set;}
        void ResumeDialogueMusic(){if(!dialogueMusicPaused)return;dialogueMusicPaused=false;GameApp.Instance.Audio.ResumeEventMusic();}
        ExpressionCatalog expressions;
        Image leftPortrait,rightPortrait;string leftCharacter,rightCharacter;
        readonly Dictionary<string,Sprite> currentPortraits=new();
        readonly Dictionary<string,Image> cast=new();
        void Awake() { LoadExpressions(Resources.Load<TextAsset>("CharacterExpressions")); }
        public void LoadExpressions(TextAsset json)
        {
            expressions=json?JsonUtility.FromJson<ExpressionCatalog>(json.text):new ExpressionCatalog();
            currentPortraits.Clear();
        }
        // 정확한 매핑을 찾으면 true. 미등록 표정은 해당 캐릭터 기본 표정으로 복원한다.
        public bool SetExpression(string character,string expressionId)
        {
            var definition=expressions.Find(character);if(definition==null)return false;
            var sprite=expressions.Resolve(definition,expressionId,out bool exact);
            if(!sprite)return false;currentPortraits[definition.id]=sprite;
            if(cast.TryGetValue(definition.id,out var castImage)&&castImage)castImage.sprite=sprite;
            if(leftPortrait && leftCharacter==definition.id)leftPortrait.sprite=sprite;
            if(rightPortrait && rightCharacter==definition.id)rightPortrait.sprite=sprite;
            return exact;
        }
        void BindCharacter(CharacterExpressions definition)
        {
            if(definition==null)return;
            if(cast.TryGetValue(definition.id,out var castImage)){if(currentPortraits.TryGetValue(definition.id,out var saved))castImage.sprite=saved;return;}
            var image=definition.side=="right"?rightPortrait:leftPortrait;
            if(definition.side=="right")rightCharacter=definition.id;else leftCharacter=definition.id;
            if(!currentPortraits.TryGetValue(definition.id,out var sprite))
                sprite=expressions.Resolve(definition,definition.defaultExpression,out _);
            if(sprite)image.sprite=sprite;
        }
        // 화면 교체 시 이전 대화의 완료 콜백을 실행하지 않는다.
        public void Cancel()
        {
            StopAllCoroutines();Paused=false;SpecialPresentation=null;LineRevealed=null;RestoreWindowInput();ResumeDialogueMusic();cast.Clear();leftPortrait=null;rightPortrait=null;leftCharacter=null;rightCharacter=null;currentPortraits.Clear();Active=false;
        }
        // 외부 상태 변화로 현재 대화만 종료할 때 완료 명령은 실행하지 않는다.
        public void CloseCurrent(){Cancel();var panel=GameApp.Instance.Modal.Find("대화");if(panel){panel.gameObject.SetActive(false);Destroy(panel.gameObject);}}
        public void Load(TextAsset json) { FlagCommand=null;document=JsonUtility.FromJson<DialogueDocument>(json.text); }
        public void Play(Action finished,params string[] sequenceIds)
        {
            if(Active || GameApp.Instance.CinematicActive) return; var lines=new List<DialogueLine>();
            foreach(var id in sequenceIds) { var seq=Array.Find(document.sequences,s=>s.id==id); if(seq!=null) lines.AddRange(seq.lines); }
            CurrentSequence=sequenceIds.Length>0?sequenceIds[0]:"";StartCoroutine(Run(lines,finished));
        }
        // 긴 대사만 실제 본문 폭/높이로 나눈다. 원본 데이터와 기존 줄은 보존한다.
        static List<DialogueLine> Paginate(List<DialogueLine> lines,TMP_Text text)
        {
            var pages=new List<DialogueLine>();
            foreach(var line in lines)
            {
                if(!line.paginate||string.IsNullOrEmpty(line.text)){pages.Add(line);continue;}
                string remaining=line.text;
                while(remaining.Length>0)
                {
                    int end=1;while(end<remaining.Length&&text.GetPreferredValues(remaining.Substring(0,end+1),text.rectTransform.rect.width,10000).y<=text.rectTransform.rect.height)end++;
                    var page=JsonUtility.FromJson<DialogueLine>(JsonUtility.ToJson(line));page.text=remaining.Substring(0,end);pages.Add(page);remaining=remaining.Substring(end);
                }
            }
            return pages;
        }
        IEnumerator Run(List<DialogueLine> lines,Action finished)
        {
            Active=true; var app=GameApp.Instance;if(!app.Stages.Current.preserveWindowsDuringDialogue)app.CloseAllWindows();
            else{preservedWindows=app.Windows.GetComponent<CanvasGroup>()??app.Windows.gameObject.AddComponent<CanvasGroup>();previousInteractable=preservedWindows.interactable;previousRaycasts=preservedWindows.blocksRaycasts;preservedWindows.interactable=false;preservedWindows.blocksRaycasts=false;UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);}
            var blocker=UIFactory.Panel(app.Modal,"대화",0,0,1024,768,new Color(0,0,0,.08f));
            var npc=UIFactory.Sprite(blocker.transform,app.Catalog.sprites.Get("portrait_folder"),32,190,335,390);
            var girl=UIFactory.Sprite(blocker.transform,app.Catalog.sprites.Get("portrait_v"),667,180,325,400);
            npc.name="왼쪽 NPC";girl.name="오른쪽 여자아이";
            leftPortrait=npc;rightPortrait=girl;leftCharacter=null;rightCharacter=null;
            var initialNpc=expressions.Find("folder");
            foreach(var firstLine in lines){var candidate=expressions.Find(string.IsNullOrEmpty(firstLine.characterId)?firstLine.speaker:firstLine.characterId);if(candidate!=null&&candidate.side=="left"){initialNpc=candidate;break;}}
            BindCharacter(initialNpc);BindCharacter(expressions.Find("girl"));
            if(app.Stages.Current.stageId=="Stage07"&&npc.sprite==app.Catalog.sprites.Get("portrait_folder"))npc.gameObject.AddComponent<FloppyPortrait>().Setup(npc);
            bool twins=lines.Exists(l=>l.speaker=="흑"||l.speaker=="백");
            if(twins)
            {
                npc.gameObject.SetActive(false);
                var group=UIFactory.Rect(blocker.transform,"쌍둥이 CG",15,175,560,390);
                cast["twin_black"]=UIFactory.Sprite(group,app.Catalog.sprites.Get("portrait_twin_black"),0,0,330,390);
                cast["twin_white"]=UIFactory.Sprite(group,app.Catalog.sprites.Get("portrait_twin_white"),190,0,330,390);
                cast["girl"]=girl;
            }
            if(app.Stages.Current.stageId=="Stage06")
            {
                npc.sprite=app.Catalog.sprites.Get("portrait_joystick");cast["girl"]=girl;cast["joystick"]=npc;
            }
            Image internet=null;
            bool reunion=SpecialPresentation=="reunion";
            if(reunion)
            {
                girl.rectTransform.anchoredPosition=new Vector2(18,-190);
                girl.rectTransform.sizeDelta=new Vector2(270,365);
                internet=UIFactory.Sprite(blocker.transform,app.Catalog.sprites.Get("portrait_npc_stage8"),260,190,270,365);
                internet.name="왼쪽 인터넷 NPC";
                npc.rectTransform.anchoredPosition=new Vector2(665,-190);
                npc.name="오른쪽 폴더 NPC";
                cast["girl"]=girl;cast["folder"]=npc;
            }
            if(SpecialPresentation=="hidden"){npc.gameObject.SetActive(false);girl.gameObject.SetActive(false);}
            UIFactory.Panel(blocker.transform,"대화 테두리",30,558,964,183,new Color(.32f,.32f,.36f));
            UIFactory.Panel(blocker.transform,"화자 표시줄",35,563,954,33,app.Catalog.theme.navy);
            UIFactory.Panel(blocker.transform,"대화 본문",35,600,954,135,app.Catalog.theme.paper);
            var name=UIFactory.Text(blocker.transform,"",47,563,850,34,22,Color.white);
            var text=UIFactory.Text(blocker.transform,"",53,615,916,102,25);
            var overlap=UIFactory.Text(blocker.transform,"",55,617,916,102,25,Color.white);overlap.name="흑백 동시 대사";overlap.raycastTarget=false;
            var echo=overlap.gameObject.AddComponent<DialogueTextEcho>();echo.Source=text;
            foreach(var line in Paginate(lines,text))
            {
                if(!string.IsNullOrEmpty(line.cmd))
                {
                    if(line.cmd=="musicPause"){app.Audio.PauseEventMusic();dialogueMusicPaused=true;}
                    else if(line.cmd=="musicResume")ResumeDialogueMusic();
                    else if(line.cmd=="glitch")StoryEffects.Get().Glitch(line.seconds);
                    else if(line.cmd=="sfx")app.Audio.Play(line.soundKey);
                    else if(line.cmd=="setFlag")FlagCommand?.Invoke(line.flag);
                    if(line.cmd=="wait"||line.cmd=="glitch")yield return new WaitForSecondsRealtime(line.seconds);
                    continue;
                }
                bool narration=line.speaker=="나레이션";
                bool simultaneous=line.style=="twinsOverlap",mutter=line.style=="mutter";
                overlap.gameObject.SetActive(simultaneous);text.fontSize=mutter?20:25;
                text.alignment=narration?TextAlignmentOptions.Center:TextAlignmentOptions.TopLeft;
                text.fontStyle=narration?FontStyles.Italic:FontStyles.Normal;text.color=narration?Color.gray:app.Catalog.theme.navy;
                if(simultaneous)text.color=Color.black;else if(mutter)text.color=new Color(.35f,.35f,.42f,.75f);
                name.text=line.speaker=="폴더"?"폴더군":line.speaker;
                var character=expressions.Find(string.IsNullOrEmpty(line.characterId)?line.speaker:line.characterId);
                if(!reunion)BindCharacter(character);
                bool girlSpeaking=character!=null?character.side=="right":line.speaker=="V양" || line.speaker=="독백";
                npc.color=girlSpeaking?new Color(.42f,.42f,.46f):Color.white;
                girl.color=girlSpeaking?Color.white:new Color(.42f,.42f,.46f);
                foreach(var member in cast){member.Value.color=!narration&&member.Key==character?.id?Color.white:new Color(.42f,.42f,.46f);}
                if(simultaneous)foreach(var member in cast)if(member.Key.StartsWith("twin_"))member.Value.color=Color.white;
                if(character!=null&&character.id.StartsWith("twin_")&&cast.TryGetValue(character.id,out var speaking))speaking.transform.SetAsLastSibling();
                if(narration){npc.color=girl.color=new Color(.42f,.42f,.46f);}
                if(!string.IsNullOrEmpty(line.expressionId)) SetExpression(character?.id,line.expressionId);
                else if(!string.IsNullOrEmpty(line.portraitKey)) { var portrait=app.Catalog.sprites.Get(line.portraitKey); if(portrait) { (girlSpeaking?girl:npc).sprite=portrait;if(character!=null)currentPortraits[character.id]=portrait; } }
                else if(character!=null && !string.IsNullOrEmpty(line.emotion)) SetExpression(character.id,line.emotion);
                if(reunion)
                {
                    // All three remain visible; the speaking character alone is highlighted.
                    var dim=new Color(.42f,.42f,.46f);
                    bool v=line.speaker=="V양"||line.speaker=="독백";
                    bool net=line.speaker=="인터넷";
                    girl.sprite=app.Catalog.sprites.Get("portrait_v");
                    npc.sprite=app.Catalog.sprites.Get("portrait_folder");
                    girl.color=v?Color.white:dim;internet.color=net?Color.white:dim;npc.color=!v&&!net&&!narration?Color.white:dim;
                }
                string content=line.text;
                if(line.highlightWords!=null) foreach(var h in line.highlightWords) content=content.Replace(h.word,$"<color={h.color}>{h.word}</color>");
                text.text=content; text.maxVisibleCharacters=0; text.ForceMeshUpdate();
                var glitch=text.GetComponent<DialogueGlyphGlitch>()??text.gameObject.AddComponent<DialogueGlyphGlitch>();glitch.Words=line.glitchWords;
                int count=text.textInfo.characterCount; float elapsed=0; int previous=-1;
                if(Skipping)
                {
                    text.maxVisibleCharacters=count;
                    float fast=0;while(Skipping&&fast<app.Catalog.theme.dialogueSkipSeconds){fast+=Time.unscaledDeltaTime;yield return null;}
                    if(Skipping){LineRevealed?.Invoke(line.text);while(Paused)yield return null;continue;}
                }
                yield return null;
                while(text.maxVisibleCharacters<count)
                {
                    if(Paused){yield return null;continue;}
                    if(GameApp.Advance || Skipping) { text.maxVisibleCharacters=count; yield return null; break; }
                    elapsed+=Time.unscaledDeltaTime; int visible=Mathf.Min(count,(int)(elapsed*(line.characterSeconds>0?1/line.characterSeconds:app.Catalog.theme.charactersPerSecond)));
                    text.maxVisibleCharacters=visible;
                    if(visible!=previous && visible%3==0) app.Audio.Play("sfx_text_tick"); previous=visible; yield return null;
                }
                yield return null;
                LineRevealed?.Invoke(line.text);
                float skipHeld=0;
                while(Paused||!GameApp.Advance)
                {
                    if(Paused){yield return null;continue;}
                    skipHeld=Skipping ? skipHeld+Time.unscaledDeltaTime : 0;
                    if(skipHeld>=app.Catalog.theme.dialogueSkipSeconds) break;
                    yield return null;
                }
                yield return null;
            }
            RestoreWindowInput();ResumeDialogueMusic();Destroy(blocker.gameObject);cast.Clear();leftPortrait=null;rightPortrait=null;leftCharacter=null;rightCharacter=null;currentPortraits.Clear(); Active=false; finished?.Invoke();
        }
    }
}
