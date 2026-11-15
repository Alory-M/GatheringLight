using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelMoments
{
    public sealed class GameView : MonoBehaviour
    {
        private readonly Campaign campaign=new Campaign();
        private readonly bool[] collected=new bool[3];
        private Puzzle Puzzle => campaign.Current;
        private Image[,] tiles;
        private Text[,] symbols;
        private int[,] revisions;
        private readonly Color cream=Hex("FFF6DE"),ink=Hex("343D46"),gold=Hex("F6BD35"),mint=Hex("A6CF6B");
        private Font font;
        private Canvas canvas;
        private RectTransform root,frame,page,board;
        private Text hearts,status,progress,modeLabel;
        private Button reset;
        private GameObject help,next;
        private AudioSource sound;
        private AudioClip tap,wrong,win;
        private Sprite rounded;
        private int mode,generation;
        private LineStroke stroke;
        private const float Pitch=40,BoardSize=Pitch*15;
        private Rect lastSafe;
        private Vector2 lastScreen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if(FindObjectOfType<GameView>()==null)new GameObject("Pixel Moments").AddComponent<GameView>();
        }
        private void Start()
        {
            Application.targetFrameRate=60;Screen.orientation=ScreenOrientation.Portrait;
#if UNITY_STANDALONE && !UNITY_EDITOR
            // Set the whole player window, including the home and completion screens.
            Screen.SetResolution(900,1600,FullScreenMode.Windowed);
#endif
            font=Resources.Load<Font>("NotoSansSC");
            if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Noto Sans CJK SC","Microsoft YaHei","Arial"},24);
            rounded=MakeRound();
            var cameraObject=new GameObject("Camera");var camera=cameraObject.AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=cream;cameraObject.AddComponent<AudioListener>();
            sound=gameObject.AddComponent<AudioSource>();tap=Tone(660,.055f);wrong=Tone(220,.12f);win=Tone(880,.45f);
            if(FindObjectOfType<EventSystem>()==null)new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var obj=new GameObject("Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas=obj.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=obj.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(900,1600);scaler.matchWidthOrHeight=0f;
            root=obj.GetComponent<RectTransform>();
            var backdrop=Panel(root,"Paper",0,0,900,1600,Color.white);
            backdrop.rectTransform.anchorMin=Vector2.zero;backdrop.rectTransform.anchorMax=Vector2.one;
            backdrop.rectTransform.sizeDelta=Vector2.zero;backdrop.sprite=MakePaper();backdrop.type=Image.Type.Simple;
            frame=Panel(root,"Full screen UI",0,0,900,1600,Color.clear).rectTransform;
            frame.GetComponent<Image>().raycastTarget=false;FitSafeArea();ShowHome();
        }
        private void Update()
        {
            if(lastSafe!=Screen.safeArea || lastScreen!=new Vector2(Screen.width,Screen.height))FitSafeArea();
        }
        private void FitSafeArea()
        {
            if(!frame)return;
            lastSafe=Screen.safeArea;lastScreen=new Vector2(Screen.width,Screen.height);
            // Stretch the UI root to the actual screen; never fit a portrait module
            // inside a separate landscape render surface. Only inset for device cutouts.
            frame.localScale=Vector3.one;
            frame.anchorMin=new Vector2(lastSafe.xMin/Screen.width,lastSafe.yMin/Screen.height);
            frame.anchorMax=new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height);
            frame.offsetMin=frame.offsetMax=Vector2.zero;
        }
        private void NewPage(string title)
        {
            generation++;stroke=null;StopAllCoroutines();
            if(page) {page.gameObject.SetActive(false);Destroy(page.gameObject);}
            page=Panel(frame,title,0,0,900,1600,Color.clear).rectTransform;
            page.anchorMin=Vector2.zero;page.anchorMax=Vector2.one;
            page.offsetMin=page.offsetMax=Vector2.zero;
            page.GetComponent<Image>().raycastTarget=false;
        }
        private void ShowHome()
        {
            campaign.Home();NewPage("Home");
            Label(page,"PIXEL MOMENTS",0,696,800,40,24,ink);
            Label(page,"像素拾光",0,594,820,110,72,ink);
            Label(page,"用一点点确定，拼出完整的小美好。",0,500,800,55,28,ink);
            var art=Panel(page,"Paper print",0,321,690,242,Color.white);
            Miniature(art.transform,Levels.All[0],-205,0,12);
            Label(art.transform,"收集三张日常小画\n15 × 15  ·  横纵推理\n\n已拾光  "+collected.Count(x=>x)+" / 3",94,0,430,190,28,ink);
            for(int i=0;i<Levels.All.Count;i++) {
                int index=i;var level=Levels.All[i];float y=83-i*204;
                var card=Panel(page,"Level "+i,0,y,780,178,Color.white);
                Panel(card.transform,"Accent",-339,0,18,116,Hex(level.Accent));
                Label(card.transform,(i+1).ToString("00"),-270,0,100,85,45,Hex(level.Accent));
                Label(card.transform,level.Title+(collected[i]?"  ✓":""),-10,36,370,56,35,ink);
                Label(card.transform,level.Difficulty+"  ·  15 × 15",-10,-33,370,46,25,ink);
                Action(card.transform,"拾光 →",266,0,170,75,()=>StartLevel(index),Hex(level.Accent));
            }
            Label(page,"自由滑动，斜着也能连续涂色\n没有倒计时，让思考慢一点。",0,-592,780,100,27,ink);
            Label(page,"黄花 · 小屋 · 蝶舞",0,-717,780,42,22,ink);
        }
        private void Miniature(Transform parent,Level level,float x,float y,float pitch)
        {
            var image=Panel(parent,"Reference art",x,y,15*pitch,15*pitch,Color.white);
            image.sprite=ArtSprite(level);image.type=Image.Type.Simple;image.raycastTarget=false;
        }
        private readonly System.Collections.Generic.Dictionary<string,Sprite> artSprites=new System.Collections.Generic.Dictionary<string,Sprite>();
        private Sprite ArtSprite(Level level)
        {
            if(artSprites.TryGetValue(level.Id,out var existing))return existing;
            var texture=new Texture2D(15,15,TextureFormat.RGBA32,false);
            texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;
            for(int r=0;r<15;r++)for(int c=0;c<15;c++)texture.SetPixel(c,14-r,Hex(level.PixelColor(r,c)));
            texture.Apply();var sprite=Sprite.Create(texture,new Rect(0,0,15,15),new Vector2(.5f,.5f),15);
            artSprites.Add(level.Id,sprite);return sprite;
        }
        private void Clue(Transform parent,ColorClue clue,float x,float y,float width,float height)
        {
            // Dark text on the exact color is legible even for cream-colored runs.
            var chip=Panel(parent,"Clue "+clue.Key+" "+clue.Length,x,y,width,height,Hex(clue.Color));
            chip.raycastTarget=false;
            var text=Label(chip.transform,clue.Length.ToString(),0,0,width,height,20,clue.Key=='D'?Color.white:Hex("252B30"));
            text.horizontalOverflow=HorizontalWrapMode.Overflow;
        }
        private void StartLevel(int index) {campaign.Start(index);BuildGame();}
        private void BuildGame()
        {
            NewPage("Puzzle");
            var level=Puzzle.Level;
            Action(page,"‹ 主页",-304,711,172,66,ShowHome,Hex("F2D38A"));
            Label(page,"PIXEL MOMENTS",0,714,385,48,22,ink);
            Action(page,"玩法 ?",315,711,150,66,()=>{stroke=null;help.SetActive(true);},mint);
            Label(page,(campaign.Index+1).ToString("00")+" / 03    "+level.Title,0,614,800,70,43,ink);
            Label(page,level.Difficulty+"  ·  "+level.Description,0,550,820,64,24,ink);
            hearts=Label(page,"",-150,467,490,52,27,ink);
            progress=Label(page,"",285,467,240,52,25,ink);
            // Leave room for nine colored clues on either axis.
            Panel(page,"Puzzle paper",0,-24,850,920,Color.white);
            board=Panel(page,"Board",102,-155,BoardSize,BoardSize,Hex("DAD3BB")).rectTransform;
            tiles=new Image[15,15];symbols=new Text[15,15];revisions=new int[15,15];
            for(int r=0;r<15;r++)for(int c=0;c<15;c++) {
                float x=(c-7)*Pitch,y=(7-r)*Pitch;
                var image=Panel(board,"Cell "+r+" "+c,x,y,Pitch-3,Pitch-3,cream);
                image.sprite=null;image.type=Image.Type.Simple;
                image.raycastTarget=false;tiles[r,c]=image;
                symbols[r,c]=Label(image.transform,"",0,0,Pitch-3,Pitch-3,26,ink);
            }
            // Faint guides delineate 5×5 blocks without stealing touch targets.
            for(int i=1;i<3;i++) {
                Panel(board,"Column guide",-BoardSize/2+i*5*Pitch,0,2,BoardSize,ink).raycastTarget=false;
                Panel(board,"Row guide",0,BoardSize/2-i*5*Pitch,BoardSize,2,ink).raycastTarget=false;
            }
            for(int i=0;i<15;i++) {
                var rows=level.RowColorClues(i);var cols=level.ColumnColorClues(i);
                if(rows.Length==0)Label(page,"0",-218,125-i*Pitch,24,30,20,ink);
                for(int j=0;j<rows.Length;j++)
                    Clue(page,rows[j],-218-(rows.Length-1-j)*23,125-i*Pitch,22,30);
                if(cols.Length==0)Label(page,"0",-178+i*Pitch,172,32,27,20,ink);
                for(int j=0;j<cols.Length;j++)
                    Clue(page,cols[j],-178+i*Pitch,172+(cols.Length-1-j)*28,32,26);
            }
            var input=board.gameObject.AddComponent<BoardInput>();
            input.Begin=BeginStroke;input.Move=MoveStroke;input.End=()=>stroke=null;
            status=Label(page,"",0,-534,800,80,26,ink);
            modeLabel=Label(page,"",0,-603,820,46,25,ink);
            Action(page,"● 涂色",-284,-677,186,78,()=>SetMode(0),gold);
            Action(page,"× 叉号",-82,-677,186,78,()=>SetMode(1),mint);
            Action(page,"擦除",120,-677,186,78,()=>SetMode(2),Hex("9FC9E3"));
            reset=Action(page,"重置",322,-677,174,78,Reset,Hex("F28B64"));
            next=Action(page,campaign.IsLast?"收集完成 · 返回主页":"画作已收好 · 下一关 →",0,-677,780,90,Advance,Hex(level.Accent)).gameObject;
            next.SetActive(false);
            Label(page,"自由滑动 · 支持斜线和转弯 · 自动使用图案颜色",0,-754,860,38,20,ink);
            BuildHelp();SetMode(0);Refresh();
        }
        private void BuildHelp()
        {
            help=Panel(page,"Help",0,0,900,1600,new Color(.20f,.23f,.27f,.65f)).gameObject;
            var card=Panel(help.transform,"Help card",0,0,820,990,cream);
            Label(card.transform,"一点点，拼出整幅画",0,385,750,80,40,ink);
            Label(card.transform,"左右、上下都看彩色数字线索。\n数字是该颜色连续出现的格数。\n同色两段之间要留空，不同颜色可以相邻。\n\n选「涂色」，手指滑过的格子都会涂色。\n支持斜线和转弯，颜色由图案自动填入。\n快速滑动会补齐沿途格子，不会重复扣值。\n\n图案格全部涂对即可通关，叉号可选。\n多涂的空白格需要擦除。\n标错扣 1 点，归零后点重置重新开始。\n\n前两关点下一关，最后一关返回主页。",0,0,740,650,27,ink);
            Action(card.transform,"明白了，慢慢画",0,-389,540,88,()=>help.SetActive(false),gold);
            help.SetActive(false);
        }
        private void SetMode(int value)
        {
            stroke=null;mode=value;
            modeLabel.text=new[]{"● 自由涂色 · 斜线 / 转弯皆可，颜色自动填入","× 叉号模式  ·  连续确认空白格","擦除模式  ·  滑动清除标记，不扣推理值"}[mode];
        }
        private void BeginStroke(float row,float col)
        {
            if(Puzzle==null || Puzzle.State!=Phase.Playing || help.activeSelf)return;
            stroke=new LineStroke(row,col,mode==0?Mark.Filled:mode==1?Mark.Empty:Mark.Unknown);
            stroke.Visit(row,col,ApplyMark);
        }
        private void MoveStroke(float row,float col)
        {
            if(Puzzle==null || Puzzle.State!=Phase.Playing || help.activeSelf) {stroke=null;return;}
            stroke?.Visit(row,col,ApplyMark);
        }
        private void ApplyMark(int row,int col,Mark mark)
        {
            if(!Puzzle.Submit(row,col,mark,out bool error))return;
            revisions[row,col]++;sound.PlayOneShot(error?wrong:tap,.15f);Refresh();
            if(Puzzle.State==Phase.Completed) {
                collected[campaign.Index]=true;stroke=null;sound.PlayOneShot(win,.25f);StartCoroutine(Reveal(generation));
            } else StartCoroutine(Feedback(row,col,error,generation,revisions[row,col]));
        }
        private Color CellColor(int r,int c)
        {
            var color=Puzzle.Cell(r,c)==Mark.Filled?Hex(Puzzle.Answer(r,c)?Puzzle.Level.PixelColor(r,c):"E67762"):cream;
            return Puzzle.State==Phase.Locked?Color.Lerp(color,Hex("8C8590"),.4f):color;
        }
        private IEnumerator Feedback(int r,int c,bool error,int epoch,int revision)
        {
            for(float elapsed=0;elapsed<.18f && epoch==generation && revision==revisions[r,c] && Puzzle.State!=Phase.Completed;elapsed+=Time.unscaledDeltaTime) {
                tiles[r,c].color=Color.Lerp(error?Hex("EB7868"):Color.Lerp(CellColor(r,c),Color.white,.18f),CellColor(r,c),elapsed/.18f);yield return null;
            }
            if(epoch==generation && revision==revisions[r,c] && Puzzle.State!=Phase.Completed)tiles[r,c].color=CellColor(r,c);
        }
        private void Reset()
        {
            if(!Puzzle.Reset())return;
            stroke=null;generation++;StopAllCoroutines();sound.PlayOneShot(tap,.2f);Refresh();
        }
        private void Refresh()
        {
            hearts.text="推理值  "+new string('●',Puzzle.Reasoning)+new string('○',5-Puzzle.Reasoning);
            progress.text=Puzzle.FilledCount+" / "+Puzzle.Level.FilledCount;
            status.text=Puzzle.State==Phase.Locked?"推理值耗尽 · 点重置，重新拾起思路":Puzzle.State==Phase.Completed?
                "画好啦！正在收好你的作品…":"涂完全部图案格即可通关 · 空白不必打叉";
            reset.interactable=Puzzle.State!=Phase.Completed;
            for(int r=0;r<15;r++)for(int c=0;c<15;c++) {
                tiles[r,c].color=CellColor(r,c);symbols[r,c].text=Puzzle.Cell(r,c)==Mark.Empty?"×":"";
            }
        }
        private IEnumerator Reveal(int epoch)
        {
            for(float t=0;t<1.2f && epoch==generation;t+=Time.unscaledDeltaTime) {
                for(int r=0;r<15;r++)for(int c=0;c<15;c++) {
                    float reveal=Mathf.Clamp01((t-r*.026f)/.65f);
                    symbols[r,c].color=new Color(ink.r,ink.g,ink.b,1-reveal);
                    tiles[r,c].color=Color.Lerp(CellColor(r,c),Hex(Puzzle.Level.PixelColor(r,c)),reveal);
                }
                board.GetComponent<Image>().color=Color.Lerp(Hex("DAD3BB"),gold,Mathf.Sin(t/1.2f*Mathf.PI));yield return null;
            }
            foreach(Transform child in board)child.gameObject.SetActive(false);
            var print=Panel(board,"Completed reference print",0,0,BoardSize,BoardSize,Color.white);
            print.sprite=ArtSprite(Puzzle.Level);print.type=Image.Type.Simple;print.raycastTarget=false;
            status.text=campaign.IsLast?"三幅画都拾到了 · 带着好心情回家吧！":"《"+Puzzle.Level.Title+"》已收好 · 继续下一幅吧！";
            next.SetActive(true);
        }
        private void Advance()
        {
            if(!campaign.Advance())return;
            if(campaign.IsHome)ShowHome();else BuildGame();
        }
        private Image Panel(Transform parent,string title,float x,float y,float width,float height,Color color)
        {
            var obj=new GameObject(title,typeof(RectTransform),typeof(Image)); obj.transform.SetParent(parent,false);
            var rt=obj.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);
            rt.anchoredPosition=new Vector2(x,y); rt.sizeDelta=new Vector2(width,height);
            var img=obj.GetComponent<Image>(); img.color=color; img.sprite=rounded; img.type=Image.Type.Sliced;
            return img;
        }
        private Text Label(Transform parent,string value,float x,float y,float width,float height,int size,Color color)
        {
            var obj=new GameObject("Label",typeof(RectTransform),typeof(Text)); obj.transform.SetParent(parent,false);
            var rt=obj.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(width,height);
            var text=obj.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=color;
            text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.supportRichText=false;return text;
        }
        private Button Action(Transform parent,string text,float x,float y,float width,float height,UnityEngine.Events.UnityAction action,Color color)
        {
            var image=Panel(parent,text,x,y,width,height,color);var b=image.gameObject.AddComponent<Button>();
            b.targetGraphic=image;b.onClick.AddListener(action);Label(image.transform,text,0,0,width,height,28,ink);return b;
        }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#"+value,out var color); return color; }
        private Sprite MakePaper()
        {
            // Subtle deterministic grain rather than shiny material highlights.
            const int width=320,height=180;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
            var random=new System.Random(17);
            for(int y=0;y<height;y++) for(int x=0;x<width;x++) {
                float grain=(float)random.NextDouble()*.025f;
                texture.SetPixel(x,y,new Color(cream.r-grain,cream.g-grain,cream.b-grain,1));
            }
            texture.filterMode=FilterMode.Point;texture.Apply();
            return Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,.5f),100);
        }
        private Sprite MakeRound()
        {
            const int size=64;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Bilinear;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float dx=Mathf.Max(12-x,x-51,0),dy=Mathf.Max(12-y,y-51,0);
                tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(12-Mathf.Sqrt(dx*dx+dy*dy))));
            }
            tex.Apply();return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(14,14,14,14));
        }
        private AudioClip Tone(float frequency,float duration)
        {
            const int rate=44100;var samples=new float[(int)(rate*duration)];
            for(int i=0;i<samples.Length;i++) {float t=(float)i/rate; samples[i]=Mathf.Sin(2*Mathf.PI*frequency*t)*Mathf.Sin(Mathf.PI*i/samples.Length)*.3f;}
            var clip=AudioClip.Create("Soft chime",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
    }
}
