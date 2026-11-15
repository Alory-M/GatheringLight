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
        private const float Pitch=42,BoardSize=Pitch*15;
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
            Label(page,"按住滑动，整行或整列连续标记\n没有倒计时，让思考慢一点。",0,-592,780,100,27,ink);
            Label(page,"黄花 · 小屋 · 蝶舞",0,-717,780,42,22,ink);
        }
        private void Miniature(Transform parent,Level level,float x,float y,float pitch)
        {
            for(int r=0;r<15;r++)for(int c=0;c<15;c++)if(level.Answer(r,c)) {
                var image=Panel(parent,"Pixel",x+(c-7)*pitch,y+(7-r)*pitch,pitch,pitch,Hex(level.PixelColor(r,c)));
                image.raycastTarget=false;
            }
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
            // 630px board + 168px row clues. Column clues stack in 8 fixed slots.
            Panel(page,"Puzzle paper",0,-24,850,920,Color.white);
            board=Panel(page,"Board",84,-133,BoardSize,BoardSize,Hex("DAD3BB")).rectTransform;
            tiles=new Image[15,15];symbols=new Text[15,15];revisions=new int[15,15];
            for(int r=0;r<15;r++)for(int c=0;c<15;c++) {
                float x=(c-7)*Pitch,y=(7-r)*Pitch;
                var image=Panel(board,"Cell "+r+" "+c,x,y,Pitch-3,Pitch-3,cream);
                image.raycastTarget=false;tiles[r,c]=image;
                symbols[r,c]=Label(image.transform,"",0,0,Pitch-3,Pitch-3,26,ink);
            }
            // Faint guides delineate 5×5 blocks without stealing touch targets.
            for(int i=1;i<3;i++) {
                Panel(board,"Column guide",-BoardSize/2+i*5*Pitch,0,2,BoardSize,ink).raycastTarget=false;
                Panel(board,"Row guide",0,BoardSize/2-i*5*Pitch,BoardSize,2,ink).raycastTarget=false;
            }
            for(int i=0;i<15;i++) {
                var rows=level.RowClues(i);var cols=level.ColumnClues(i);
                // Row clues use fixed slots as well, so 8-segment clues never overflow.
                int slots=Mathf.Max(1,rows.Length);
                for(int j=0;j<slots;j++)Label(page,rows.Length==0?"0":rows[j].ToString(),-245-(slots-1-j)*23,161-i*Pitch,25,38,23,ink);
                for(int j=0;j<Mathf.Max(1,cols.Length);j++)Label(page,cols.Length==0?"0":cols[j].ToString(),-210+i*Pitch,207+(Mathf.Max(1,cols.Length)-1-j)*27,Pitch,29,23,ink);
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
            Label(page,"长按滑动  ·  首次移动锁定行 / 列  ·  每格每次只标记一次",0,-754,860,38,20,ink);
            BuildHelp();SetMode(0);Refresh();
        }
        private void BuildHelp()
        {
            help=Panel(page,"Help",0,0,900,1600,new Color(.20f,.23f,.27f,.65f)).gameObject;
            var card=Panel(help.transform,"Help card",0,0,820,990,cream);
            Label(card.transform,"一点点，拼出整幅画",0,385,750,80,40,ink);
            Label(card.transform,"数字表示连续色块的长度。\n多段数字之间，至少留一个空白。\n\n选「涂色」或「叉号」，按住棋盘滑动。\n第一次移动决定横向或纵向，\n快速划过的格子也会连续标记。\n\n全部 225 格都要正确标记，才能揭图。\n每次错误标记扣 1 点，归零只能重置。\n擦除不扣分，同一次滑动不重复标记。\n\n完成后点下一关，第三关完成后返回主页。",0,0,740,650,29,ink);
            Action(card.transform,"明白了，慢慢画",0,-389,540,88,()=>help.SetActive(false),gold);
            help.SetActive(false);
        }
        private void SetMode(int value)
        {
            stroke=null;mode=value;
            modeLabel.text=new[]{"● 涂色模式  ·  按住并横向 / 纵向滑动","× 叉号模式  ·  连续确认空白格","擦除模式  ·  滑动清除标记，不扣推理值"}[mode];
        }
        private void BeginStroke(int row,int col)
        {
            if(Puzzle==null || Puzzle.State!=Phase.Playing || help.activeSelf)return;
            stroke=new LineStroke(row,col,mode==0?Mark.Filled:mode==1?Mark.Empty:Mark.Unknown);
            stroke.Visit(row,col,ApplyMark);
        }
        private void MoveStroke(int row,int col)
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
            var color=Puzzle.Cell(r,c)==Mark.Filled?gold:cream;
            return Puzzle.State==Phase.Locked?Color.Lerp(color,Hex("8C8590"),.4f):color;
        }
        private IEnumerator Feedback(int r,int c,bool error,int epoch,int revision)
        {
            for(float elapsed=0;elapsed<.18f && epoch==generation && revision==revisions[r,c] && Puzzle.State!=Phase.Completed;elapsed+=Time.unscaledDeltaTime) {
                tiles[r,c].color=Color.Lerp(error?Hex("EB7868"):Color.white,CellColor(r,c),elapsed/.18f);yield return null;
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
            progress.text=Puzzle.MarkedCount+" / 225";
            status.text=Puzzle.State==Phase.Locked?"推理值耗尽 · 点重置，重新拾起思路":Puzzle.State==Phase.Completed?
                "推理成功！属于你的色彩正在显现…":"数字之间的留白，也是答案的一部分。";
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
            // Make the completed print seamless, with no crossword marks or guide lines.
            foreach(Transform child in board)if(child.name.Contains("guide"))child.gameObject.SetActive(false);
            for(int r=0;r<15;r++)for(int c=0;c<15;c++) {
                symbols[r,c].text="";tiles[r,c].color=Hex(Puzzle.Level.PixelColor(r,c));
                tiles[r,c].rectTransform.sizeDelta=new Vector2(Pitch,Pitch);
            }
            board.GetComponent<Image>().color=gold;
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
