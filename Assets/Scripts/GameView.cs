using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelMoments
{
    public sealed class GameView : MonoBehaviour
    {
        private readonly Puzzle puzzle = new Puzzle();
        private readonly Image[,] tiles = new Image[5,5];
        private readonly Text[,] symbols = new Text[5,5];
        private readonly Button[,] buttons = new Button[5,5];
        private readonly int[,] revisions = new int[5,5];
        private readonly Color cream = Hex("FFF6DE"), ink = Hex("343D46"), gold = Hex("F6BD35"), mint = Hex("A6CF6B");
        private Font font;
        private RectTransform root, board;
        private Text hearts, status, progress, modeLabel;
        private Button reset;
        private GameObject help;
        private AudioSource sound;
        private AudioClip tap, wrong, win;
        private int mode; // paint, cross, erase, cycle
        private int generation;
        private Sprite rounded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindObjectOfType<GameView>() == null) new GameObject("Pixel Moments").AddComponent<GameView>();
        }
        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            font = Resources.Load<Font>("NotoSansSC");
            if (!font) font = Font.CreateDynamicFontFromOSFont(new[]{"Noto Sans CJK SC", "Microsoft YaHei", "Arial"}, 24);
            rounded = MakeRound();
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cream; cameraObject.AddComponent<AudioListener>();
            sound = gameObject.AddComponent<AudioSource>();
            tap = Tone(660, .055f); wrong = Tone(220, .12f); win = Tone(880, .45f);
            if (FindObjectOfType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = .5f;
            root = canvas.GetComponent<RectTransform>();
            var paper = Panel(root, "Backdrop", 0,0,1600,900,cream);
            paper.sprite = MakePaper(); paper.type = Image.Type.Simple; paper.color = Color.white;
            Panel(root, "Left decoration", -680,-340,260,260,Hex("9FC9E3"));
            Panel(root, "Right decoration", 690,340,240,240,Hex("A6CF6B"));
            Label(root,"PIXEL MOMENTS  /  慢慢来，光会亮", -510,369,520,38,20,ink);
            Label(root,"像素拾光",-540,285,460,90,62,ink);
            Label(root,"把每一个小小的确定，\n拼成闪闪发光的日常。",-530,182,480,90,25,ink);
            Panel(root,"Chapter",-528,57,450,74,Hex("F2D38A"));
            Label(root,"01   第一束星光     /     5 × 5",-528,57,430,50,23,ink);
            hearts = Label(root,"",-530,-39,470,55,28,ink);
            progress = Label(root,"",-530,-102,470,40,20,ink);
            status = Label(root,"",-528,-202,470,115,23,ink);
            Label(root,"没有倒计时 · 每一次思考都值得",-525,-353,470,36,19,ink);
            Panel(root,"Puzzle card",267,31,910,694,Color.white);
            Label(root,"星光小笺",267,331,700,44,28,ink);
            Label(root,"数字是连续色块的长度，多段之间至少空一格",267,282,780,40,19,ink);
            board = Panel(root,"Board",320,-11,445,445,Hex("DAD3BB")).rectTransform;
            for(int r=0;r<5;r++) for(int c=0;c<5;c++) {
                int row=r,col=c; float x=(c-2)*86, y=(2-r)*86;
                var image=Panel(board,"Cell "+r+" "+c,x,y,78,78,cream); tiles[r,c]=image;
                var b=image.gameObject.AddComponent<Button>(); b.targetGraphic=image;
                b.onClick.AddListener(()=>Click(row,col)); buttons[r,c]=b;
                symbols[r,c]=Label(image.rectTransform,"",0,0,72,72,36,ink);
            }
            for(int i=0;i<5;i++) {
                Label(root,string.Join("  ",puzzle.RowClues(i).Select(n=>n.ToString())),33,161-i*86,100,60,26,ink);
                Label(root,string.Join("\n",puzzle.ColumnClues(i).Select(n=>n.ToString())),148+i*86,237,78,68,24,ink);
            }
            modeLabel=Label(root,"",270,-275,800,35,18,ink);
            Action(root,"涂色",-5,-359,130,54,()=>SetMode(0),gold);
            Action(root,"叉号",141,-359,130,54,()=>SetMode(1),mint);
            Action(root,"擦除",287,-359,130,54,()=>SetMode(2),Hex("9FC9E3"));
            Action(root,"循环",433,-359,130,54,()=>SetMode(3),Hex("F2D38A"));
            reset=Action(root,"重置",656,-359,176,54,Reset,Hex("F28B64"));
            Action(root,"玩法 ?",659,369,170,48,()=>help.SetActive(true),mint);
            BuildHelp(); SetMode(0); Refresh();
        }
        private void BuildHelp()
        {
            help=Panel(root,"How to play",0,0,1600,900,new Color(.30f,.27f,.32f,.55f)).gameObject;
            var card=Panel(help.transform,"Help card",0,0,940,660,cream);
            Label(card.transform,"欢迎拾起第一束光",0,234,820,65,40,ink);
            Label(card.transform,"看数字，慢慢推理",0,157,800,45,27,ink);
            Label(card.transform,"[3] 是连续 3 格色块；[1, 3] 两段之间至少空 1 格。\n\n涂色：确定是图案。叉号：确定是空白。\n全部 25 格都要标记正确，才能点亮星星。\n\n标错一次扣 1 点推理值，归零后只能重置。\n擦除不扣分；循环模式：未标记 → 涂色 → 叉号。\n循环中的每次错误标记都会扣分，请谨慎选择。",0,-40,800,330,25,ink);
            Action(card.transform,"开始拾光",0,-248,280,64,()=>help.SetActive(false),gold);
            help.SetActive(false);
        }
        private void SetMode(int value)
        {
            mode=value;
            modeLabel.text=new[]{"● 涂色模式  ·  点击格子填入暖黄色", "× 叉号模式  ·  点击格子确认空白", "擦除模式  ·  清除标记，不消耗推理值", "循环模式  ·  未标记 → 涂色 → 叉号 → 未标记"}[mode];
        }
        private void Click(int r,int c)
        {
            var mark=mode==0?Mark.Filled:mode==1?Mark.Empty:mode==2?Mark.Unknown:(Mark)(((int)puzzle.Cells[r,c]+1)%3);
            if(!puzzle.Submit(r,c,mark,out bool error)) return;
            revisions[r,c]++;
            sound.PlayOneShot(error?wrong:tap,.22f); Refresh();
            StartCoroutine(Feedback(r,c,error,generation,revisions[r,c]));
            if(puzzle.State==Phase.Completed) { sound.PlayOneShot(win,.25f); StartCoroutine(Reveal(generation)); }
        }
        private Color CellColor(int r,int c)
        {
            var color=puzzle.Cells[r,c]==Mark.Filled?gold:cream;
            if(puzzle.State==Phase.Locked) color=Color.Lerp(color,Hex("8C8590"),.4f);
            return color;
        }
        private IEnumerator Feedback(int r,int c,bool error,int epoch,int revision)
        {
            float elapsed=0;
            while(elapsed<.23f && epoch==generation && revision==revisions[r,c] && puzzle.State!=Phase.Completed) {
                elapsed+=Time.unscaledDeltaTime;
                tiles[r,c].color=Color.Lerp(error?Hex("EB7868"):Color.white,CellColor(r,c),elapsed/.23f);
                tiles[r,c].transform.localScale=Vector3.one*(1+.05f*Mathf.Sin(elapsed/.23f*Mathf.PI));
                yield return null;
            }
            if(epoch==generation && revision==revisions[r,c]) { tiles[r,c].transform.localScale=Vector3.one; tiles[r,c].color=CellColor(r,c); }
        }
        private void Reset()
        {
            if(!puzzle.Reset()) return;
            generation++; StopAllCoroutines();
            foreach(var t in tiles) t.transform.localScale=Vector3.one;
            sound.PlayOneShot(tap,.2f); Refresh();
        }
        private void Refresh()
        {
            hearts.text="推理值   "+new string('●',puzzle.Reasoning)+new string('○',5-puzzle.Reasoning);
            progress.text="已标记  "+puzzle.MarkedCount+" / 25     ·     入门 / 五角星";
            status.text=puzzle.State==Phase.Locked?"推理值耗尽，无法继续试错。\n点「重置」，重新拾起思路。":puzzle.State==Phase.Completed?"关卡完成！推理成功。\n你拾到了一颗小星星。":"让横与纵的线索相遇，\n答案就藏在交叉的地方。";
            reset.interactable=puzzle.State!=Phase.Completed;
            for(int r=0;r<5;r++) for(int c=0;c<5;c++) {
                tiles[r,c].color=CellColor(r,c);
                symbols[r,c].text=puzzle.Cells[r,c]==Mark.Empty?"×":"";
                buttons[r,c].interactable=puzzle.State==Phase.Playing;
            }
        }
        private IEnumerator Reveal(int epoch)
        {
            for(float t=0;t<1.5f && epoch==generation;t+=Time.unscaledDeltaTime) {
                float reveal=Mathf.Clamp01(t/.9f);
                for(int r=0;r<5;r++) for(int c=0;c<5;c++) {
                    symbols[r,c].color=new Color(ink.r,ink.g,ink.b,1-reveal);
                    tiles[r,c].color=Color.Lerp(CellColor(r,c),puzzle.Answer(r,c)?Hex("FFC637"):Color.white,reveal);
                }
                board.localScale=Vector3.one*(1+.025f*Mathf.Sin(t*Mathf.PI/1.5f));
                board.GetComponent<Image>().color=Color.Lerp(Hex("DAD3BB"),gold,Mathf.Sin(t*Mathf.PI/1.5f));
                yield return null;
            }
            board.localScale=Vector3.one;
            board.GetComponent<Image>().color=gold;
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
            b.targetGraphic=image;b.onClick.AddListener(action);Label(image.transform,text,0,0,width,height,22,ink);return b;
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
