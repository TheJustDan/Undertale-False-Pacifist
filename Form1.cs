using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace Undertale_False_Pacifist
{
    public partial class Undertale : Form
    {
        private System.Windows.Forms.Timer gameTimer;
        private Player player;
        private Bitmap friskSprite;

        private HashSet<Keys> pressedKeys = new HashSet<Keys>();

        public enum InventoryState { SelectingItem, SelectingAction, ShowingInfo }

        private Bitmap sansSprite;

        private Bitmap sansFaceNormal;
        private Bitmap sansFaceWink;
        private Bitmap sansFaceClosed;
        private Bitmap sansFaceEmpty;
        private Bitmap sansFaceSerious;
        private Bitmap sansFaceSide;

        private struct DialogueLine
        {
            public string Text;
            public string Expression;
            public bool IsSansFont;

            public DialogueLine(string text, string expression, bool isSansFont)
            {
                Text = text;
                Expression = expression;
                IsSansFont = isSansFont;
            }

            public DialogueLine(string text, string expression)
            {
                Text = text;
                Expression = expression;
                IsSansFont = true;
            }
        }

        private bool isDialogueActive = false;
        private List<DialogueLine> dialogueQueue = new List<DialogueLine>();
        private int dialogueIndex = 0;
        private float dialogueCharProgress = 0f;
        private const float DialogueCharsPerTick = 0.6f;

        private bool hasSansTrigger = false;
        private bool sansTriggerConsumed = false;
        private PointF sansTriggerPos;
        private const float TriggerRadius = 8f;
        private string sansTriggerFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sans_trigger_data.txt");

        private struct RoomTriggerPoint
        {
            public PointF Pos;
            public string TargetRoom;
            public float? SpawnX;
            public float? SpawnY;
        }

        private List<RoomTriggerPoint> roomTriggers = new List<RoomTriggerPoint>();
        private bool wasInCastleTriggerZone = false;

        private bool isRoomTransitioning = false;
        private bool roomTransitionFadingOut = true;
        private float roomTransitionAlpha = 0f;
        private const float RoomTransitionSpeed = 12f;
        private string pendingRoomName = null;
        private PointF? pendingRoomSpawn = null;

        private Dictionary<string, Bitmap> roomPlayerSprites = new Dictionary<string, Bitmap>();

        private const float CastleTriggerRadius = 10f;
        private const float CastleTriggerRemoveRadius = 12f;
        private string castleTriggersFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "castle_trigger_data.txt");

        private Dictionary<string, Bitmap> roomBackgrounds = new Dictionary<string, Bitmap>();
        private Dictionary<string, PointF> roomSpawns = new Dictionary<string, PointF>();

        private enum SansCutsceneStage
        {
            None,
            WaitingBeforePan,
            PanningCameraForward,
            WaitingBeforeBells,
            BellsPlaying,
            WaitingAfterBells,
            DialogueActive,
            PanningCameraBack
        }
        private SansCutsceneStage cutsceneStage = SansCutsceneStage.None;
        private bool IsSansCutsceneActive => cutsceneStage != SansCutsceneStage.None;

        private bool sansEncounterCameraLocked = false;
        private float cutsceneCameraX = 0f;
        private float cutsceneCameraTargetX = 0f;
        private const float CutscenePanDistance = 125f;
        private const float CutsceneCameraSpeed = 1.3f;
        private int cutsceneTimer = 0;
        private const int OneSecondTicks = 63;

        private bool sansVisibleInWorld = false;
        private float sansWorldX = 0f;
        private float sansWorldY = 0f;
        private const float SansWorldScale = 3f;

        private const float SansAheadOffsetX = 0.67f;
        private const float SansVerticalOffset = 4f;

        private const float VirtualCanvasWidth = 800f;

        private float lastCameraX = 0f;
        private float lastCameraY = 0f;
        private float lastScreenScale = 1f;
        private float lastScreenOffsetX = 0f;
        private float lastScreenOffsetY = 0f;

        private Bitmap inventoryBox;
        private Bitmap heartTexture;
        private Bitmap locationBackground;
        private Bitmap castleBackground;
        private float cameraZoom = 2.5f;
        private float cutsceneCameraZoom = 2.5f;
        private Dictionary<string, float> roomCameraZoom = new Dictionary<string, float>
        {
            { "castle_finalshoehorn", 2.5f }
        };
        private Dictionary<string, PointF> roomCameraOffset = new Dictionary<string, PointF>
        {
            { "castle_finalshoehorn", new PointF(20f, 20f) }
        };
        private float BaseCameraZoom => roomCameraZoom.TryGetValue(currentLocationName, out float z) ? z : cameraZoom;
        private float CurrentCameraZoom => sansEncounterCameraLocked ? cutsceneCameraZoom : BaseCameraZoom;
        private float LocationWorldWidth => locationBackground != null ? locationBackground.Width : 0f;
        private float LocationWorldHeight => locationBackground != null ? locationBackground.Height : 0f;

        private PrivateFontCollection pfc = new PrivateFontCollection();
        private Font pixelFont;
        private Font pixelFontSmall;

        private PrivateFontCollection sansPfc = new PrivateFontCollection();
        private Font sansFont;

        private bool isMenuOpen = false;
        private bool isInSubMenu = false;
        private int selectedMenuIndex = 0;
        private int selectedItemIndex = 0;
        private int selectedActionIndex = 0;
        private InventoryState currentInvState = InventoryState.SelectingItem;

        private string PlayerName = "Chara";
        private int CurrentHP = 10;
        private int MaxHP = 20;
        private int Attack = 10;
        private int WeaponAttack = 7;
        private int Defense = 10;
        private int ArmorDefense = 5;
        private string EquippedWeapon = "None";
        private string EquippedArmor = "None";
        private int Gold = 0;

        public struct CollisionBox
        {
            public RectangleF Rect;
            public CollisionBox(float x, float y, float width, float height)
            {
                Rect = new RectangleF(x, y, width, height);
            }
        }

        private List<CollisionBox> collisionBoxes = new List<CollisionBox>();
        private string collisionsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "collisions_data.txt");

        private bool showCollisions = false;
        private PointF collisionDragStart;
        private PointF collisionDragCurrent;
        private bool isCreatingCollision = false;

        private Bitmap columnTexture;
        private List<float> foregroundColumns = new List<float>();

        private float parallaxFactor = 2f;
        private string columnsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "columns_data.txt");

        private struct WallObject
        {
            public PointF Pos; // точка "основания" стены (низ текстуры) - используется для сортировки по глубине
        }

        private Bitmap wallTexture;
        private List<WallObject> worldWalls = new List<WallObject>();
        private string wallsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "walls_data.txt");
        private const float WallRemoveRadius = 12f;

        private string RoomCollisionsPath(string roomName)
        {
            if (roomName == "last_corridor") return collisionsFilePath;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "collisions_data_" + roomName + ".txt");
        }

        private string RoomColumnsPath(string roomName)
        {
            if (roomName == "last_corridor") return columnsFilePath;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "columns_data_" + roomName + ".txt");
        }

        private string RoomWallsPath(string roomName)
        {
            if (roomName == "last_corridor") return wallsFilePath;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "walls_data_" + roomName + ".txt");
        }

        private string RoomTriggersPath(string roomName)
        {
            if (roomName == "last_corridor") return castleTriggersFilePath;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trigger_data_" + roomName + ".txt");
        }

        private List<Item> inventory = new List<Item>();

        private bool isFullscreen = false;
        private Rectangle windowedBounds;

        private FMOD.System fmodSystem;
        private FMOD.Sound bgmSound;
        private FMOD.Channel bgmChannel;
        private FMOD.Sound voiceSound;
        private FMOD.Channel voiceChannel;
        private FMOD.Sound lastSfxSound;
        private FMOD.Channel sfxChannel;

        private bool isFmodInitialized = false;
        private bool isBgmLoaded = false;
        private bool isVoiceLoaded = false;
        private bool isSfxLoaded = false;

        private string currentMusic = "";
        private string currentLocationName = "last_corridor";

        public Undertale()
        {
            InitializeComponent();

            this.Text = "Undertale False Pacifist";
            this.ClientSize = new Size(800, 600);
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            InitFMOD();

            string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fonts", "undertale_battle_font.ttf");
            if (File.Exists(fontPath))
            {
                pfc.AddFontFile(fontPath);
                pixelFont = new Font(pfc.Families[0], 18, FontStyle.Regular);
                pixelFontSmall = new Font(pfc.Families[0], 14, FontStyle.Regular);
            }
            else
            {
                pixelFont = new Font("Courier New", 18, FontStyle.Bold);
                pixelFontSmall = new Font("Courier New", 14, FontStyle.Bold);
            }

            string sansFontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fonts", "comic_sans.ttf");

            if (File.Exists(sansFontPath))
            {
                sansPfc.AddFontFile(sansFontPath);
                if (sansPfc.Families.Length > 0)
                {
                    sansFont = new Font(sansPfc.Families[0], 16, FontStyle.Regular);
                }
            }

            if (sansFont == null)
            {
                sansFont = new Font("Comic Sans MS", 14, FontStyle.Regular);
            }

            if (sansFont == null)
            {
                sansFont = new Font("Comic Sans MS", 14, FontStyle.Regular);
            }

            string columnPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "column.bmp");
            if (File.Exists(columnPath))
            {
                columnTexture = new Bitmap(columnPath);
                columnTexture.MakeTransparent(Color.White);
            }

            string wallPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "wall.bmp");
            if (File.Exists(wallPath))
            {
                wallTexture = new Bitmap(wallPath);
                wallTexture.MakeTransparent(Color.White);
            }

            string spritePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "dark_frisk.bmp");

            if (File.Exists(spritePath))
            {
                friskSprite = new Bitmap(spritePath);
                friskSprite.MakeTransparent(Color.White);
            }

            string friskAltPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "frisk.bmp");
            Bitmap friskAltSprite = null;
            if (File.Exists(friskAltPath))
            {
                friskAltSprite = new Bitmap(friskAltPath);
                friskAltSprite.MakeTransparent(Color.White);
            }

            roomPlayerSprites["last_corridor"] = friskSprite;
            roomPlayerSprites["castle_finalshoehorn"] = friskAltSprite ?? friskSprite;

            string sansPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "sans.bmp");
            if (File.Exists(sansPath))
            {
                sansSprite = new Bitmap(sansPath);
                sansSprite.MakeTransparent(Color.White);
            }

            sansFaceNormal = LoadSansFace("sans_normal.bmp");
            sansFaceWink = LoadSansFace("sans_wink.bmp");
            sansFaceClosed = LoadSansFace("sans_closed.bmp");
            sansFaceEmpty = LoadSansFace("sans_empty.bmp");
            sansFaceSerious = LoadSansFace("sans_serious.bmp");
            sansFaceSide = LoadSansFace("sans_side.bmp");

            string invPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "inventory.bmp");
            if (File.Exists(invPath))
            {
                inventoryBox = new Bitmap(invPath);
                inventoryBox.MakeTransparent(Color.White);
            }

            string heartPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "heart.bmp");
            if (File.Exists(heartPath))
            {
                heartTexture = new Bitmap(heartPath);
                heartTexture.MakeTransparent(Color.White);
            }

            string locationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "last_corridor.bmp");
            if (File.Exists(locationPath))
            {
                locationBackground = new Bitmap(locationPath);
            }

            string castleRoomPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "room_castle_finalshoehorn.bmp");
            if (File.Exists(castleRoomPath))
            {
                castleBackground = new Bitmap(castleRoomPath);
            }

            roomBackgrounds["last_corridor"] = locationBackground;
            roomBackgrounds["castle_finalshoehorn"] = castleBackground;

            roomSpawns["last_corridor"] = new PointF(105f, 100f);
            roomSpawns["castle_finalshoehorn"] = new PointF(150f, 150f);

            inventory.Add(new Item("Stick", "A standard stick."));
            inventory.Add(new Item("Bandage", "Heals 10 HP."));

            this.KeyDown += Form1_KeyDown;

            player = new Player { X = 105f, Y = 100f };

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            StartIntro();
            LoadColumns();
            LoadWalls();
            LoadSansTrigger();
            LoadCollisions();
            LoadCastleTriggers();


            this.MouseDown += Form1_MouseDown;
            this.MouseMove += Form1_MouseMove;
            this.MouseUp += Form1_MouseUp;

            this.KeyDown += (s, e) => pressedKeys.Add(e.KeyCode);
            this.KeyUp += (s, e) => pressedKeys.Remove(e.KeyCode);
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isCreatingCollision)
            {
                collisionDragCurrent = ScreenToWorld(e.Location);
                this.Invalidate();
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            // Работает только если включен режим разработчика (F3)
            if (!showCollisions) return;
            if (introActive || isMenuOpen || isDialogueActive || IsSansCutsceneActive) return;

            PointF worldPos = ScreenToWorld(e.Location);

            // 1. УПРАВЛЕНИЕ ТРИГГЕРАМИ (Зажат SHIFT)
            if (Control.ModifierKeys == Keys.Shift)
            {
                if (e.Button == MouseButtons.Left)
                {
                    // Установить триггер
                    roomTriggers.Add(new RoomTriggerPoint
                    {
                        Pos = worldPos,
                        TargetRoom = GetOtherRoom(currentLocationName)
                    });
                    SaveCastleTriggers();
                    this.Invalidate();
                }
                else if (e.Button == MouseButtons.Right)
                {
                    // Удалить триггер
                    int closestIndex = -1;
                    float closestDist = float.MaxValue;

                    for (int i = 0; i < roomTriggers.Count; i++)
                    {
                        float dx = roomTriggers[i].Pos.X - worldPos.X;
                        float dy = roomTriggers[i].Pos.Y - worldPos.Y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestIndex = i;
                        }
                    }

                    if (closestIndex >= 0 && closestDist <= CastleTriggerRemoveRadius)
                    {
                        roomTriggers.RemoveAt(closestIndex);
                        SaveCastleTriggers();
                        this.Invalidate();
                    }
                }
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                isCreatingCollision = true;
                collisionDragStart = worldPos;
                collisionDragCurrent = worldPos;
            }
            else if (e.Button == MouseButtons.Right)
            {
                for (int i = collisionBoxes.Count - 1; i >= 0; i--)
                {
                    if (collisionBoxes[i].Rect.Contains(worldPos))
                    {
                        collisionBoxes.RemoveAt(i);
                        SaveCollisions();
                        PlaySoundEffect("move.mp3");
                        this.Invalidate();
                        break;
                    }
                }
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isCreatingCollision)
            {
                isCreatingCollision = false;

                float x = Math.Min(collisionDragStart.X, collisionDragCurrent.X);
                float y = Math.Min(collisionDragStart.Y, collisionDragCurrent.Y);
                float w = Math.Abs(collisionDragCurrent.X - collisionDragStart.X);
                float h = Math.Abs(collisionDragCurrent.Y - collisionDragStart.Y);

                if (w >= 2f && h >= 2f)
                {
                    collisionBoxes.Add(new CollisionBox(x, y, w, h));
                    SaveCollisions();
                    PlaySoundEffect("move.mp3");
                }
                this.Invalidate();
            }
        }

        private void LoadCollisions()
        {
            collisionBoxes.Clear();
            string path = RoomCollisionsPath(currentLocationName);
            if (!File.Exists(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    string[] p = line.Split(',');
                    if (p.Length == 4 &&
                        float.TryParse(p[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                        float.TryParse(p[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float w) &&
                        float.TryParse(p[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float h))
                    {
                        collisionBoxes.Add(new CollisionBox(x, y, w, h));
                    }
                }
            }
            catch { }
        }

        private void SaveCollisions()
        {
            try
            {
                string path = RoomCollisionsPath(currentLocationName);
                List<string> lines = new List<string>();
                foreach (var box in collisionBoxes)
                {
                    string line = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3}",
                        box.Rect.X, box.Rect.Y, box.Rect.Width, box.Rect.Height);
                    lines.Add(line);
                }
                File.WriteAllLines(path, lines);
            }
            catch { }
        }

        private List<RoomTriggerPoint> ReadRoomTriggersFile(string roomName)
        {
            List<RoomTriggerPoint> result = new List<RoomTriggerPoint>();
            string path = RoomTriggersPath(roomName);
            if (!File.Exists(path)) return result;

            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    string[] p = line.Split(',');
                    if (p.Length >= 2 &&
                        float.TryParse(p[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                    {
                        string target = (p.Length >= 3 && !string.IsNullOrWhiteSpace(p[2]))
                            ? p[2].Trim()
                            : GetOtherRoom(roomName);

                        float? spawnX = null;
                        float? spawnY = null;
                        if (p.Length >= 5 &&
                            float.TryParse(p[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sx) &&
                            float.TryParse(p[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sy))
                        {
                            spawnX = sx;
                            spawnY = sy;
                        }

                        result.Add(new RoomTriggerPoint { Pos = new PointF(x, y), TargetRoom = target, SpawnX = spawnX, SpawnY = spawnY });
                    }
                }
            }
            catch { }

            return result;
        }

        private void LoadCastleTriggers()
        {
            roomTriggers = ReadRoomTriggersFile(currentLocationName);
        }

        private void SaveCastleTriggers()
        {
            try
            {
                string path = RoomTriggersPath(currentLocationName);
                List<string> lines = new List<string>();
                foreach (var t in roomTriggers)
                {
                    if (t.SpawnX.HasValue && t.SpawnY.HasValue)
                    {
                        lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4}",
                            t.Pos.X, t.Pos.Y, t.TargetRoom, t.SpawnX.Value, t.SpawnY.Value));
                    }
                    else
                    {
                        lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2}", t.Pos.X, t.Pos.Y, t.TargetRoom));
                    }
                }
                File.WriteAllLines(path, lines);
            }
            catch { }
        }

        private PointF ScreenToWorld(Point screenPoint)
        {
            float virtualWidth = 800f;
            float virtualHeight = 600f;

            float screenScale = Math.Min(this.ClientSize.Width / virtualWidth, this.ClientSize.Height / virtualHeight);
            float screenOffsetX = (this.ClientSize.Width - (virtualWidth * screenScale)) / 2f;
            float screenOffsetY = (this.ClientSize.Height - (virtualHeight * screenScale)) / 2f;

            float virtX = (screenPoint.X - screenOffsetX) / screenScale;
            float virtY = (screenPoint.Y - screenOffsetY) / screenScale;

            float cameraX = lastCameraX;
            float cameraY = lastCameraY;

            float worldX = (virtX / CurrentCameraZoom) + cameraX;
            float worldY = (virtY / CurrentCameraZoom) + cameraY;

            return new PointF(worldX, worldY);
        }

        private bool CheckCollision(float nextX, float nextY, float width, float height)
        {
            RectangleF futureHitbox = new RectangleF(
                nextX + player.HitboxOffsetX,
                nextY + player.HitboxOffsetY,
                width,
                height
            );

            foreach (var box in collisionBoxes)
            {
                if (futureHitbox.IntersectsWith(box.Rect))
                    return true;
            }
            return false;
        }

        private void InitFMOD()
        {
            FMOD.RESULT result = FMOD.Factory.System_Create(out fmodSystem);
            if (result != FMOD.RESULT.OK)
            {
                MessageBox.Show($"FMOD Error: {result}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            result = fmodSystem.init(32, FMOD.INITFLAGS.NORMAL, IntPtr.Zero);
            if (result != FMOD.RESULT.OK)
            {
                MessageBox.Show($"FMOD Init Error: {result}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            isFmodInitialized = true;
        }

        private void PlaySoundEffect(string soundFileName)
        {
            string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", soundFileName);
            if (!File.Exists(soundPath)) return;

            if (isSfxLoaded)
            {
                lastSfxSound.release();
                isSfxLoaded = false;
            }

            if (fmodSystem.createSound(soundPath, FMOD.MODE.DEFAULT, out lastSfxSound) == FMOD.RESULT.OK)
            {
                isSfxLoaded = true;
                fmodSystem.playSound(lastSfxSound, new FMOD.ChannelGroup(), false, out sfxChannel);
            }
        }

        private void OpenVoice(string soundFileName)
        {
            string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", soundFileName);
            if (!File.Exists(soundPath)) return;

            CloseVoice();
            if (fmodSystem.createSound(soundPath, FMOD.MODE.DEFAULT, out voiceSound) == FMOD.RESULT.OK)
            {
                isVoiceLoaded = true;
            }
        }

        private void PlayVoiceBlip()
        {
            if (isVoiceLoaded)
            {
                fmodSystem.playSound(voiceSound, new FMOD.ChannelGroup(), false, out voiceChannel);
            }
        }

        private void CloseVoice()
        {
            if (isVoiceLoaded)
            {
                voiceSound.release();
                isVoiceLoaded = false;
            }
        }

        private int GetLastSoundEffectLengthMs()
        {
            if (isSfxLoaded)
            {
                lastSfxSound.getLength(out uint length, FMOD.TIMEUNIT.MS);
                return (int)length;
            }
            return 0;
        }

        private void PlayLocationMusic()
        {
            string targetMusic = "";

            if (currentLocationName == "last_corridor")
            {
                targetMusic = "background.mp3";
            }
            else if (currentLocationName == "castle_finalshoehorn")
            {
                targetMusic = "castle.mp3";
            }

            if (currentMusic == targetMusic) return;

            StopMusic();

            if (!string.IsNullOrEmpty(targetMusic))
            {
                string musicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", targetMusic);

                if (File.Exists(musicPath))
                {
                    if (fmodSystem.createSound(musicPath, FMOD.MODE.LOOP_NORMAL, out bgmSound) == FMOD.RESULT.OK)
                    {
                        isBgmLoaded = true;
                        fmodSystem.playSound(bgmSound, new FMOD.ChannelGroup(), false, out bgmChannel);
                        currentMusic = targetMusic;
                    }
                }
            }
        }

        private string GetOtherRoom(string currentRoom)
        {
            return currentRoom == "last_corridor" ? "castle_finalshoehorn" : "last_corridor";
        }

        private void EnterRoom(string roomName, PointF? spawnOverride = null)
        {
            if (!roomBackgrounds.TryGetValue(roomName, out Bitmap bg) || bg == null) return;
            if (isRoomTransitioning) return;

            pendingRoomName = roomName;
            pendingRoomSpawn = spawnOverride;
            isRoomTransitioning = true;
            roomTransitionFadingOut = true;
            roomTransitionAlpha = 0f;
        }

        private void PerformRoomSwitch(string roomName, PointF? spawnOverride)
        {
            if (!roomBackgrounds.TryGetValue(roomName, out Bitmap bg) || bg == null) return;

            string fromRoom = currentLocationName;

            locationBackground = bg;
            currentLocationName = roomName;

            if (roomPlayerSprites.TryGetValue(roomName, out Bitmap sprite) && sprite != null)
            {
                friskSprite = sprite;
            }

            PointF spawn;
            bool spawnedAtDoor;

            if (spawnOverride.HasValue)
            {
                spawn = spawnOverride.Value;
                spawnedAtDoor = true;
            }
            else if (roomSpawns.TryGetValue(roomName, out PointF s))
            {
                spawn = s;
                spawnedAtDoor = false;
            }
            else
            {
                RoomTriggerPoint? pairedDoor = null;
                foreach (var t in ReadRoomTriggersFile(roomName))
                {
                    if (t.TargetRoom == fromRoom)
                    {
                        pairedDoor = t;
                        break;
                    }
                }

                if (pairedDoor.HasValue)
                {
                    spawn = pairedDoor.Value.Pos;
                    spawnedAtDoor = true;
                }
                else
                {
                    spawn = new PointF(105f, 100f);
                    spawnedAtDoor = false;
                }
            }

            player.X = spawn.X;
            player.Y = spawn.Y;

            lastCameraX = 0f;
            lastCameraY = 0f;

            LoadCollisions();
            LoadColumns();
            LoadWalls();
            LoadCastleTriggers();

            // если заспавнились прямо на парной двери - считаем, что уже "внутри" её зоны,
            // иначе она сразу сработает обратно и телепортирует туда-обратно по кругу
            wasInCastleTriggerZone = spawnedAtDoor;

            PlayLocationMusic();
            this.Invalidate();
        }

        private void StopMusic()
        {
            if (isBgmLoaded)
            {
                bgmChannel.stop();
                bgmSound.release();
                isBgmLoaded = false;
            }
            currentMusic = "";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopMusic();
            CloseVoice();

            if (isSfxLoaded)
            {
                lastSfxSound.release();
                isSfxLoaded = false;
            }

            if (isFmodInitialized)
            {
                fmodSystem.release();
                isFmodInitialized = false;
            }

            base.OnFormClosing(e);
        }

        private void LoadColumns()
        {
            foregroundColumns.Clear();
            string path = RoomColumnsPath(currentLocationName);

            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    if (float.TryParse(line, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x))
                    {
                        foregroundColumns.Add(x);
                    }
                }
            }
        }

        private void LoadWalls()
        {
            worldWalls.Clear();
            string path = RoomWallsPath(currentLocationName);
            if (!File.Exists(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    string[] p = line.Split(',');
                    if (p.Length == 2 &&
                        float.TryParse(p[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                    {
                        worldWalls.Add(new WallObject { Pos = new PointF(x, y) });
                    }
                }
            }
            catch { }
        }

        private void SaveWalls()
        {
            try
            {
                string path = RoomWallsPath(currentLocationName);
                List<string> lines = new List<string>();
                foreach (var w in worldWalls)
                {
                    lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", w.Pos.X, w.Pos.Y));
                }
                File.WriteAllLines(path, lines);
            }
            catch { }
        }

        private void LoadSansTrigger()
        {
            if (!File.Exists(sansTriggerFilePath)) return;

            try
            {
                string content = File.ReadAllText(sansTriggerFilePath).Trim();
                string[] parts = content.Split(',');

                if (parts.Length == 2 &&
                    float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                {
                    sansTriggerPos = new PointF(x, y);
                    hasSansTrigger = true;
                    sansTriggerConsumed = false;
                }
            }
            catch
            {
            }
        }

        private void ToggleFullscreen()
        {
            if (!isFullscreen)
            {
                windowedBounds = this.Bounds;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                isFullscreen = true;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.Bounds = windowedBounds;
                isFullscreen = false;
            }
            this.Invalidate();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.F4)
            {
                ToggleFullscreen();
                return;
            }

            if (e.KeyCode == Keys.F3)
            {
                showCollisions = !showCollisions;
                this.Invalidate();
                return;
            }

            if (introActive)
            {
                HandleIntroKeyDown(e.KeyCode);
                return;
            }

            if (isDialogueActive)
            {
                if (e.KeyCode == Keys.Z)
                {
                    AdvanceDialogue();
                }
                return;
            }

            if (IsSansCutsceneActive)
            {
                return;
            }

            if (e.KeyCode == Keys.C)
            {
                isMenuOpen = !isMenuOpen;
                isInSubMenu = false;
                currentInvState = InventoryState.SelectingItem;
                pressedKeys.Clear();
                this.Invalidate();
                return;
            }

            if (isMenuOpen)
            {
                if (!isInSubMenu)
                {
                    if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down)
                    {
                        selectedMenuIndex = (selectedMenuIndex + 1) % 3;
                        PlaySoundEffect("move.mp3");
                    }

                    if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
                    {
                        selectedMenuIndex = (selectedMenuIndex - 1 + 3) % 3;
                        PlaySoundEffect("move.mp3");
                    }

                    if (e.KeyCode == Keys.Z)
                    {
                        if ((selectedMenuIndex == 0 && inventory.Count > 0) || selectedMenuIndex == 1 || selectedMenuIndex == 2)
                        {
                            isInSubMenu = true;
                            selectedItemIndex = 0;
                            currentInvState = InventoryState.SelectingItem;
                            PlaySoundEffect("move.mp3");
                        }
                    }
                }
                else
                {
                    if (selectedMenuIndex == 0)
                    {
                        if (currentInvState == InventoryState.SelectingItem)
                        {
                            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down)
                            {
                                selectedItemIndex = (selectedItemIndex + 1) % inventory.Count;
                                PlaySoundEffect("move.mp3");
                            }

                            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
                            {
                                selectedItemIndex = (selectedItemIndex - 1 + inventory.Count) % inventory.Count;
                                PlaySoundEffect("move.mp3");
                            }

                            if (e.KeyCode == Keys.X)
                            {
                                isInSubMenu = false;
                                PlaySoundEffect("move.mp3");
                            }

                            if (e.KeyCode == Keys.Z && inventory.Count > 0)
                            {
                                currentInvState = InventoryState.SelectingAction;
                                selectedActionIndex = 0;
                                PlaySoundEffect("move.mp3");
                            }
                        }
                        else if (currentInvState == InventoryState.SelectingAction)
                        {
                            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right)
                            {
                                selectedActionIndex = (selectedActionIndex + 1) % 3;
                                PlaySoundEffect("move.mp3");
                            }

                            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left)
                            {
                                selectedActionIndex = (selectedActionIndex - 1 + 3) % 3;
                                PlaySoundEffect("move.mp3");
                            }

                            if (e.KeyCode == Keys.X)
                            {
                                currentInvState = InventoryState.SelectingItem;
                                PlaySoundEffect("move.mp3");
                            }

                            if (e.KeyCode == Keys.Z)
                            {
                                PlaySoundEffect("move.mp3");
                                if (selectedActionIndex == 0)
                                {
                                    inventory.RemoveAt(selectedItemIndex);
                                    selectedItemIndex = 0;
                                    currentInvState = InventoryState.SelectingItem;
                                    if (inventory.Count == 0) isInSubMenu = false;
                                }
                            }
                        }
                    }
                    else if (selectedMenuIndex == 1 || selectedMenuIndex == 2)
                    {
                        if (e.KeyCode == Keys.X)
                        {
                            isInSubMenu = false;
                            PlaySoundEffect("move.mp3");
                        }
                    }
                }
                this.Invalidate();
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (isFmodInitialized)
            {
                fmodSystem.update();
            }

            if (introActive)
            {
                UpdateIntro();
                this.Invalidate();
                return;
            }

            if (gameFadeInActive)
            {
                gameFadeInAlpha -= GameFadeInSpeed;
                if (gameFadeInAlpha <= 0f)
                {
                    gameFadeInAlpha = 0f;
                    gameFadeInActive = false;
                }
            }

            if (isRoomTransitioning)
            {
                if (roomTransitionFadingOut)
                {
                    roomTransitionAlpha += RoomTransitionSpeed;
                    if (roomTransitionAlpha >= 255f)
                    {
                        roomTransitionAlpha = 255f;
                        PerformRoomSwitch(pendingRoomName, pendingRoomSpawn);
                        pendingRoomName = null;
                        pendingRoomSpawn = null;
                        roomTransitionFadingOut = false;
                    }
                }
                else
                {
                    roomTransitionAlpha -= RoomTransitionSpeed;
                    if (roomTransitionAlpha <= 0f)
                    {
                        roomTransitionAlpha = 0f;
                        isRoomTransitioning = false;
                    }
                }

                this.Invalidate();
                return;
            }

            HandleInput();
            player.UpdateAnimation();

            if (currentLocationName == "last_corridor" && hasSansTrigger && !sansTriggerConsumed && !isDialogueActive && !IsSansCutsceneActive && !isMenuOpen)
            {
                float playerCenterXForTrigger = player.X + (player.Width / 2f);
                float triggerDistanceX = Math.Abs(playerCenterXForTrigger - sansTriggerPos.X);

                if (triggerDistanceX <= TriggerRadius)
                {
                    sansTriggerConsumed = true;
                    StartSansCutscene();
                }
            }

            if (!isDialogueActive && !IsSansCutsceneActive && !isMenuOpen)
            {
                float playerCenterXForCastle = player.X + (player.Width / 2f);
                float playerCenterYForCastle = player.Y + (player.Height / 2f);

                bool isInCastleZoneNow = false;
                string targetRoomNow = null;
                PointF? targetSpawnNow = null;

                for (int i = 0; i < roomTriggers.Count; i++)
                {
                    float dx = playerCenterXForCastle - roomTriggers[i].Pos.X;
                    float dy = playerCenterYForCastle - roomTriggers[i].Pos.Y;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                    if (dist <= CastleTriggerRadius)
                    {
                        isInCastleZoneNow = true;
                        targetRoomNow = roomTriggers[i].TargetRoom;

                        if (roomTriggers[i].SpawnX.HasValue && roomTriggers[i].SpawnY.HasValue)
                        {
                            targetSpawnNow = new PointF(roomTriggers[i].SpawnX.Value, roomTriggers[i].SpawnY.Value);
                        }

                        break;
                    }
                }

                if (isInCastleZoneNow && !wasInCastleTriggerZone && !string.IsNullOrEmpty(targetRoomNow))
                {
                    EnterRoom(targetRoomNow, targetSpawnNow);
                }

                wasInCastleTriggerZone = isInCastleZoneNow;
            }

            if (cutsceneStage == SansCutsceneStage.WaitingBeforePan)
            {
                cutsceneTimer--;
                if (cutsceneTimer <= 0)
                {
                    cutsceneStage = SansCutsceneStage.PanningCameraForward;
                }
            }
            else if (cutsceneStage == SansCutsceneStage.PanningCameraForward)
            {
                cutsceneCameraX += CutsceneCameraSpeed;
                if (cutsceneCameraX >= cutsceneCameraTargetX)
                {
                    cutsceneCameraX = cutsceneCameraTargetX;
                    cutsceneStage = SansCutsceneStage.WaitingBeforeBells;
                    cutsceneTimer = 63;
                }
            }
            else if (cutsceneStage == SansCutsceneStage.WaitingBeforeBells)
            {
                cutsceneTimer--;
                if (cutsceneTimer <= 0)
                {
                    PlaySoundEffect("bells.mp3");

                    int bellsLengthMs = GetLastSoundEffectLengthMs();
                    int bellsTicks = bellsLengthMs > 0
                        ? (int)Math.Ceiling(bellsLengthMs / (double)gameTimer.Interval)
                        : 10;

                    cutsceneStage = SansCutsceneStage.BellsPlaying;
                    cutsceneTimer = bellsTicks;
                }
            }
            else if (cutsceneStage == SansCutsceneStage.BellsPlaying)
            {
                cutsceneTimer--;
                if (cutsceneTimer <= 0)
                {
                    cutsceneStage = SansCutsceneStage.DialogueActive;
                    StartSansDialogue(BuildSansJudgementDialogue());
                }
            }
            else if (cutsceneStage == SansCutsceneStage.WaitingAfterBells)
            {
                cutsceneTimer--;
                if (cutsceneTimer <= 0)
                {
                    cutsceneStage = SansCutsceneStage.DialogueActive;
                    StartSansDialogue(BuildSansJudgementDialogue());
                }
            }
            else if (cutsceneStage == SansCutsceneStage.PanningCameraBack)
            {
                float playerCenterX = player.X + (player.DrawWidth / 2f);

                float viewportWorldWidth = VirtualCanvasWidth / cameraZoom;
                float targetNormalCamX = playerCenterX - (viewportWorldWidth / 1.75f);

                if (locationBackground != null)
                {
                    targetNormalCamX = ClampCamera(targetNormalCamX, viewportWorldWidth, LocationWorldWidth);
                }

                if (Math.Abs(cutsceneCameraX - targetNormalCamX) <= CutsceneCameraSpeed)
                {
                    cutsceneCameraX = targetNormalCamX;
                    cutsceneStage = SansCutsceneStage.None;
                    sansEncounterCameraLocked = false;
                    sansVisibleInWorld = false;
                    PlayLocationMusic();
                }
                else if (cutsceneCameraX > targetNormalCamX)
                {
                    cutsceneCameraX -= CutsceneCameraSpeed;
                }
                else if (cutsceneCameraX < targetNormalCamX)
                {
                    cutsceneCameraX += CutsceneCameraSpeed;
                }
            }

            if (isDialogueActive && dialogueQueue.Count > 0)
            {
                DialogueLine currentLine = dialogueQueue[dialogueIndex];
                string fullText = currentLine.Text;

                if (dialogueCharProgress < fullText.Length)
                {
                    int prevVisibleChars = (int)dialogueCharProgress;
                    dialogueCharProgress = Math.Min(fullText.Length, dialogueCharProgress + DialogueCharsPerTick);
                    int newVisibleChars = (int)dialogueCharProgress;

                    if (newVisibleChars > prevVisibleChars)
                    {
                        char lastChar = fullText[Math.Min(newVisibleChars, fullText.Length) - 1];

                        if (!char.IsWhiteSpace(lastChar) && currentLine.IsSansFont)
                        {
                            PlayVoiceBlip();
                        }
                    }
                }
            }

            this.Invalidate();
        }

        private void StartSansCutscene()
        {
            player.IsMoving = false;
            pressedKeys.Clear();

            cutsceneCameraX = lastCameraX;
            cutsceneCameraTargetX = cutsceneCameraX + CutscenePanDistance;

            float viewportWorldWidth = VirtualCanvasWidth / cutsceneCameraZoom;
            sansWorldX = cutsceneCameraTargetX + viewportWorldWidth * SansAheadOffsetX;
            sansWorldY = sansTriggerPos.Y + SansVerticalOffset;
            sansVisibleInWorld = true;
            sansEncounterCameraLocked = true;

            cutsceneStage = SansCutsceneStage.WaitingBeforePan;
            cutsceneTimer = OneSecondTicks / 2;

            StopMusic();
            this.Invalidate();
        }

        private void HandleInput()
        {
            if (isMenuOpen || isDialogueActive || IsSansCutsceneActive || introActive)
            {
                player.IsMoving = false;
                return;
            }

            float dx = 0;
            float dy = 0;

            if (pressedKeys.Contains(Keys.Left) || pressedKeys.Contains(Keys.A))
            {
                dx -= player.Speed;
                player.Direction = 2;
            }
            if (pressedKeys.Contains(Keys.Right) || pressedKeys.Contains(Keys.D))
            {
                dx += player.Speed;
                player.Direction = 1;
            }
            if (pressedKeys.Contains(Keys.Up) || pressedKeys.Contains(Keys.W))
            {
                dy -= player.Speed;
                player.Direction = 3;
            }
            if (pressedKeys.Contains(Keys.Down) || pressedKeys.Contains(Keys.S))
            {
                dy += player.Speed;
                player.Direction = 0;
            }

            if (dx != 0 && dy != 0)
            {
                dx *= 0.7071f;
                dy *= 0.7071f;
            }

            player.IsMoving = (dx != 0 || dy != 0);

            if (dx != 0)
            {
                float nextX = player.X + dx;
                if (!CheckCollision(nextX, player.Y, player.Width, player.Height))
                {
                    player.X = nextX;
                }
            }

            if (dy != 0)
            {
                float nextY = player.Y + dy;
                if (!CheckCollision(player.X, nextY, player.Width, player.Height))
                {
                    player.Y = nextY;
                }
            }

            if (locationBackground != null)
            {
                float halfW = player.Width / 2f;
                float halfH = player.Height / 2f;
                player.X = Math.Max(-halfW, Math.Min(player.X, LocationWorldWidth - halfW));
                player.Y = Math.Max(-halfH, Math.Min(player.Y, LocationWorldHeight - halfH));
            }
        }

        private float ClampCamera(float cam, float viewportSize, float mapSize, float centerOffset = 0f)
        {
            if (mapSize <= viewportSize)
            {
                return -(viewportSize - mapSize) / 2f + centerOffset;
            }
            return Math.Max(0, Math.Min(cam, mapSize - viewportSize));
        }

        private Bitmap LoadSansFace(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", fileName);
            if (File.Exists(path))
            {
                Bitmap bmp = new Bitmap(path);
                bmp.MakeTransparent(Color.White);
                return bmp;
            }
            return null;
        }

        private Bitmap GetSansFace(string expression)
        {
            switch (expression)
            {
                case "wink": return sansFaceWink;
                case "closed": return sansFaceClosed;
                case "empty": return sansFaceEmpty;
                case "serious": return sansFaceSerious;
                case "side": return sansFaceSide;
                default: return sansFaceNormal;
            }
        }

        private void StartSansDialogue(List<DialogueLine> lines)
        {
            if (lines == null || lines.Count == 0) return;

            dialogueQueue = lines;
            dialogueIndex = 0;
            dialogueCharProgress = 0f;
            isDialogueActive = true;

            pressedKeys.Clear();
            player.IsMoving = false;

            StopMusic();
            OpenVoice("voice_sans.mp3");

            this.Invalidate();
        }

        private void AdvanceDialogue()
        {
            if (!isDialogueActive || dialogueQueue.Count == 0) return;

            string fullText = dialogueQueue[dialogueIndex].Text;

            if (dialogueCharProgress < fullText.Length)
            {
                dialogueCharProgress = fullText.Length;
            }
            else
            {
                dialogueIndex++;
                dialogueCharProgress = 0f;

                if (dialogueIndex >= dialogueQueue.Count)
                {
                    isDialogueActive = false;
                    dialogueQueue.Clear();
                    dialogueIndex = 0;

                    cutsceneStage = SansCutsceneStage.PanningCameraBack;
                }
            }

            this.Invalidate();
        }

        private List<DialogueLine> BuildSansJudgementDialogue()
        {
            return new List<DialogueLine>
    {
        new DialogueLine("So you finally made it.", "normal", false),
        new DialogueLine("The end of your journey is at hand.", "normal", false),
        new DialogueLine("In a few moments, you will meet the king.", "normal", false),
        new DialogueLine("Together... you will determine the future of this \nworld.", "normal", false),
        new DialogueLine("That's then.", "normal", false),
        new DialogueLine("Now.", "serious", false),
        new DialogueLine("You will be judged.", "serious", false),

        new DialogueLine("You will be judged for every EXP you've earned.", "serious", false),
        new DialogueLine("What's EXP? it's an acronym.", "normal", false),
        new DialogueLine("It stands for \"execution points\".", "normal", false),
        new DialogueLine("A way of quantifying the pain you have inflicted \non others.", "normal", false),
        new DialogueLine("When you kill someone, your EXP increases.", "normal", false),
        new DialogueLine("When you have enough EXP, your LOVE increases.", "normal", false),
        new DialogueLine("LOVE, too, is an acronym.", "normal", false),
        new DialogueLine("It stands for \"Level of Violence\".", "normal", false),
        new DialogueLine("A way of measuring someone's capacity to hurt.", "normal", false),
        new DialogueLine("... But you.", "side", false),

        new DialogueLine("you never gained any LOVE.", "normal"),
        new DialogueLine("your EXP is 0. your LV is 1.", "normal"),
        new DialogueLine("you didn't spill a single drop of blood.", "normal"),
        new DialogueLine("you became friends with papyrus...", "side"),
        new DialogueLine("saved undyne, and hung out with alphys...", "side"),
        new DialogueLine("you look like a real saint, kid.", "wink"),

        new DialogueLine("you know... over the years, i've learned to read \npeople by the way they walk.", "normal"),
        new DialogueLine("Usually, those who refuse to hurt anyone move on \nwith a heavy heart.", "side"),
        new DialogueLine("They're thinking about the sacrifice, about how to \nsave all of us.", "side"),
        new DialogueLine("They carry the weight of every choice on their \nshoulders.", "serious"),
        new DialogueLine("But you...", "empty"),
        new DialogueLine("You're walking very quietly. too quietly.", "empty"),
        new DialogueLine("As if you weren't going to decide anything.", "empty"),
        new DialogueLine("it's as if you're just looking for a way to take \nwhat's yours and slip away while no one is looking.", "closed"),
        new DialogueLine("or maybe...", "serious"),
        new DialogueLine("you're holding back something much worse.", "empty"),

        new DialogueLine(". . .", "closed"),
        new DialogueLine("heh. guess i'm just overthinking it.", "wink"),
        new DialogueLine("or maybe I just slept too much on my station.", "wink"),
        new DialogueLine("forget it.", "closed"),

        new DialogueLine("asgore is ahead.", "normal"),
        new DialogueLine("he doesn't want to fight you, but he has no choice.", "side"),
        new DialogueLine("do what you're supposed to do, kid.", "normal"),
        new DialogueLine("Just... remember one thing.", "serious"),
        new DialogueLine("True friends don't abandon each other in a dungeon.", "empty"),
        new DialogueLine("and they certainly don't play with people's lives \njust to see what happens.", "serious"),
        new DialogueLine(". . .", "closed"),
        new DialogueLine("See you later.", "wink")
    };
        }

        public class Item
        {
            public string Name { get; set; }
            public string Description { get; set; }

            public Item(string name, string description)
            {
                Name = name;
                Description = description;
            }
        }

        private const float WallSizeScale = 1f;
        private const float WallSortHeightRatio = 1f;

        private float GetWallSortY(WallObject w)
        {
            float worldWallHeight = wallTexture.Height * WallSizeScale;
            return w.Pos.Y - (worldWallHeight * WallSortHeightRatio);
        }

        private void DrawWorldWall(Graphics g, Matrix screenMatrix, float cameraX, float cameraY, WallObject w)
        {
            float wallScreenX = (w.Pos.X - cameraX) * CurrentCameraZoom;
            float wallScreenY = (w.Pos.Y - cameraY) * CurrentCameraZoom;
            float wallW = wallTexture.Width * CurrentCameraZoom * WallSizeScale;
            float wallH = wallTexture.Height * CurrentCameraZoom * WallSizeScale;

            g.Transform = screenMatrix;
            g.DrawImage(wallTexture, wallScreenX - wallW / 2f, wallScreenY - wallH, wallW, wallH);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            float virtualWidth = 800f;
            float virtualHeight = 600f;

            float screenScale = Math.Min(this.ClientSize.Width / virtualWidth, this.ClientSize.Height / virtualHeight);

            float screenOffsetX = (this.ClientSize.Width - (virtualWidth * screenScale)) / 2f;
            float screenOffsetY = (this.ClientSize.Height - (virtualHeight * screenScale)) / 2f;

            lastScreenScale = screenScale;
            lastScreenOffsetX = screenOffsetX;
            lastScreenOffsetY = screenOffsetY;

            using (Matrix screenMatrix = new Matrix())
            {
                screenMatrix.Translate(screenOffsetX, screenOffsetY);
                screenMatrix.Scale(screenScale, screenScale);

                e.Graphics.Transform = screenMatrix;
                e.Graphics.SetClip(new Rectangle(0, 0, (int)virtualWidth, (int)virtualHeight));
                e.Graphics.Clear(Color.Black);

                if (introActive)
                {
                    DrawIntro(e.Graphics, virtualWidth, virtualHeight);
                    return;
                }

                float playerCenterX = player.X + (player.DrawWidth / 2f);
                float playerCenterY = player.Y + (player.DrawHeight / 2f);

                float viewportWorldWidth = virtualWidth / BaseCameraZoom;
                float viewportWorldHeight = virtualHeight / BaseCameraZoom;

                float cameraX;
                float cameraY;

                if (sansEncounterCameraLocked)
                {
                    cameraX = cutsceneCameraX;
                    cameraY = 0f;
                }
                else
                {
                    cameraX = playerCenterX - (viewportWorldWidth / 2f + 30);
                    cameraY = playerCenterY - (viewportWorldHeight / 2f + 30);

                    if (locationBackground != null)
                    {
                        cameraX = ClampCamera(cameraX, viewportWorldWidth, LocationWorldWidth, 0f);
                        cameraY = ClampCamera(cameraY, viewportWorldHeight, LocationWorldHeight, 0f);
                    }
                }

                lastCameraX = cameraX;
                lastCameraY = cameraY;

                using (Matrix cameraMatrix = screenMatrix.Clone())
                {
                    cameraMatrix.Scale(CurrentCameraZoom, CurrentCameraZoom);
                    cameraMatrix.Translate(-cameraX, -cameraY);
                    e.Graphics.Transform = cameraMatrix;

                    if (locationBackground != null)
                    {
                        e.Graphics.DrawImage(locationBackground, 0, 0, LocationWorldWidth, LocationWorldHeight);
                    }

                    if (showCollisions)
                    {
                        using (Pen redPen = new Pen(Color.FromArgb(220, Color.Red), 1f))
                        using (SolidBrush redBrush = new SolidBrush(Color.FromArgb(80, Color.Red)))
                        using (Pen yellowPen = new Pen(Color.Yellow, 1f))
                        using (Pen bluePen = new Pen(Color.Cyan, 1.5f))
                        using (Pen greenPen = new Pen(Color.Lime, 1f))
                        using (SolidBrush greenBrush = new SolidBrush(Color.FromArgb(60, Color.Lime)))
                        {
                            foreach (var box in collisionBoxes)
                            {
                                e.Graphics.FillRectangle(redBrush, box.Rect);
                                e.Graphics.DrawRectangle(redPen, box.Rect.X, box.Rect.Y, box.Rect.Width, box.Rect.Height);
                            }

                            if (isCreatingCollision)
                            {
                                float x = Math.Min(collisionDragStart.X, collisionDragCurrent.X);
                                float y = Math.Min(collisionDragStart.Y, collisionDragCurrent.Y);
                                float w = Math.Abs(collisionDragCurrent.X - collisionDragStart.X);
                                float h = Math.Abs(collisionDragCurrent.Y - collisionDragStart.Y);

                                e.Graphics.DrawRectangle(yellowPen, x, y, w, h);
                            }

                            e.Graphics.DrawRectangle(
                                bluePen,
                                player.X + player.HitboxOffsetX,
                                player.Y + player.HitboxOffsetY,
                                player.Width,
                                player.Height
                            );

                            if (currentLocationName == "last_corridor" && hasSansTrigger && !sansTriggerConsumed)
                            {
                                float trigX = sansTriggerPos.X - TriggerRadius;
                                float trigY = sansTriggerPos.Y - TriggerRadius;
                                float diameter = TriggerRadius * 2f;

                                e.Graphics.FillEllipse(greenBrush, trigX, trigY, diameter, diameter);
                                e.Graphics.DrawEllipse(greenPen, trigX, trigY, diameter, diameter);

                                e.Graphics.FillEllipse(Brushes.Lime, sansTriggerPos.X - 2f, sansTriggerPos.Y - 2f, 4f, 4f);
                            }

                            using (Pen orangePen = new Pen(Color.Orange, 1f))
                            using (SolidBrush orangeBrush = new SolidBrush(Color.FromArgb(80, Color.Orange)))
                            using (Font labelFont = new Font("Consolas", 6f))
                            {
                                foreach (var trig in roomTriggers)
                                {
                                    float trigX = trig.Pos.X - CastleTriggerRadius;
                                    float trigY = trig.Pos.Y - CastleTriggerRadius;
                                    float diameter = CastleTriggerRadius * 2f;

                                    e.Graphics.FillEllipse(orangeBrush, trigX, trigY, diameter, diameter);
                                    e.Graphics.DrawEllipse(orangePen, trigX, trigY, diameter, diameter);

                                    e.Graphics.FillEllipse(Brushes.Orange, trig.Pos.X - 2f, trig.Pos.Y - 2f, 4f, 4f);

                                    e.Graphics.DrawString(trig.TargetRoom, labelFont, Brushes.Orange, trig.Pos.X - CastleTriggerRadius, trig.Pos.Y + CastleTriggerRadius + 1f);
                                }
                            }

                            using (Pen magentaPen = new Pen(Color.Magenta, 1f))
                            using (Pen sortLinePen = new Pen(Color.Cyan, 1f))
                            {
                                foreach (var w in worldWalls)
                                {
                                    e.Graphics.DrawLine(magentaPen, w.Pos.X - 6f, w.Pos.Y, w.Pos.X + 6f, w.Pos.Y);
                                    e.Graphics.DrawLine(magentaPen, w.Pos.X, w.Pos.Y - 6f, w.Pos.X, w.Pos.Y + 6f);

                                    if (wallTexture != null)
                                    {
                                        float sortY = GetWallSortY(w);
                                        e.Graphics.DrawLine(sortLinePen, w.Pos.X - 10f, sortY, w.Pos.X + 10f, sortY);
                                    }
                                }
                            }
                        }
                    }
                }

                e.Graphics.Transform = screenMatrix;

                float playerFeetY = player.Y + player.HitboxOffsetY + player.Height;

                if (wallTexture != null)
                {
                    foreach (var w in worldWalls)
                    {
                        if (playerFeetY <= GetWallSortY(w))
                        {
                            DrawWorldWall(e.Graphics, screenMatrix, cameraX, cameraY, w);
                        }
                    }
                }

                float playerScreenX = (player.X - cameraX) * CurrentCameraZoom;
                float playerScreenY = (player.Y - cameraY) * CurrentCameraZoom;

                using (Matrix playerMatrix = screenMatrix.Clone())
                {
                    playerMatrix.Translate(playerScreenX - player.X, playerScreenY - player.Y);
                    e.Graphics.Transform = playerMatrix;
                    player.Draw(e.Graphics, friskSprite);
                }

                e.Graphics.Transform = screenMatrix;

                if (wallTexture != null)
                {
                    foreach (var w in worldWalls)
                    {
                        if (playerFeetY > GetWallSortY(w))
                        {
                            DrawWorldWall(e.Graphics, screenMatrix, cameraX, cameraY, w);
                        }
                    }
                }

                e.Graphics.Transform = screenMatrix;

                if (sansVisibleInWorld && sansSprite != null)
                {
                    float sansScreenX = (sansWorldX - cameraX) * CurrentCameraZoom;
                    float sansScreenY = (sansWorldY - cameraY) * CurrentCameraZoom;
                    float sansW = sansSprite.Width * SansWorldScale;
                    float sansH = sansSprite.Height * SansWorldScale;

                    using (Matrix sansMatrix = screenMatrix.Clone())
                    {
                        sansMatrix.Translate(sansScreenX - sansWorldX, sansScreenY - sansWorldY);
                        e.Graphics.Transform = sansMatrix;
                        e.Graphics.DrawImage(sansSprite, sansWorldX - sansW / 2f, sansWorldY - sansH, sansW, sansH);
                    }
                }

                e.Graphics.Transform = screenMatrix;

                if (columnTexture != null)
                {
                    foreach (float colX in foregroundColumns)
                    {
                        float colScreenX = (colX - (cameraX * parallaxFactor)) * CurrentCameraZoom;
                        float colScreenY = (0 - cameraY) * CurrentCameraZoom;

                        e.Graphics.DrawImage(columnTexture, colScreenX, colScreenY,
                                             columnTexture.Width * CurrentCameraZoom,
                                             columnTexture.Height * CurrentCameraZoom);
                    }
                }

                e.Graphics.Transform = screenMatrix;

                if (isMenuOpen)
                {
                    DrawInventory(e.Graphics);
                }

                if (isDialogueActive)
                {
                    DrawSansDialogue(e.Graphics);
                }

                if (gameFadeInActive)
                {
                    int a = (int)Math.Max(0, Math.Min(255, gameFadeInAlpha));
                    using (SolidBrush fadeBrush = new SolidBrush(Color.FromArgb(a, Color.Black)))
                    {
                        e.Graphics.FillRectangle(fadeBrush, 0, 0, virtualWidth, virtualHeight);
                    }
                }

                if (isRoomTransitioning)
                {
                    int a = (int)Math.Max(0, Math.Min(255, roomTransitionAlpha));
                    using (SolidBrush transitionBrush = new SolidBrush(Color.FromArgb(a, Color.Black)))
                    {
                        e.Graphics.FillRectangle(transitionBrush, 0, 0, virtualWidth, virtualHeight);
                    }
                }
            }
        }

        private void DrawInventory(Graphics g)
        {
            using (Pen thickWhitePen = new Pen(Color.White, 5f))
            {
                thickWhitePen.Alignment = PenAlignment.Inset;

                int statBoxX = 30;
                int statBoxY = 40;
                int statBoxW = 200;
                int statBoxH = 150;

                g.FillRectangle(Brushes.Black, statBoxX, statBoxY, statBoxW, statBoxH);
                g.DrawRectangle(thickWhitePen, statBoxX, statBoxY, statBoxW, statBoxH);

                g.DrawString(PlayerName, pixelFont, Brushes.White, statBoxX + 20, statBoxY + 15);
                g.DrawString("LV  1", pixelFontSmall, Brushes.White, statBoxX + 20, statBoxY + 60);
                g.DrawString($"HP  {CurrentHP}/{MaxHP}", pixelFontSmall, Brushes.White, statBoxX + 20, statBoxY + 85);
                g.DrawString($"G   {Gold}", pixelFontSmall, Brushes.White, statBoxX + 20, statBoxY + 110);

                int menuBoxX = 30;
                int menuBoxY = statBoxY + statBoxH + 15;
                int menuBoxW = 200;
                int menuBoxH = 220;

                g.FillRectangle(Brushes.Black, menuBoxX, menuBoxY, menuBoxW, menuBoxH);
                g.DrawRectangle(thickWhitePen, menuBoxX, menuBoxY, menuBoxW, menuBoxH);

                string[] menuOptions = { "ITEM", "STAT", "CELL" };
                int menuTextX = menuBoxX + 65;
                int menuItemStartY = menuBoxY + 30;
                int menuItemSpacing = 55;

                for (int i = 0; i < menuOptions.Length; i++)
                {
                    g.DrawString(menuOptions[i], pixelFont, Brushes.White, menuTextX, menuItemStartY + (i * menuItemSpacing));
                }

                if (!isInSubMenu)
                {
                    int heartY = menuItemStartY + (selectedMenuIndex * menuItemSpacing) + 4;
                    int heartX = menuBoxX + 30;

                    if (heartTexture != null) g.DrawImage(heartTexture, heartX, heartY, heartTexture.Width * 1.5f, heartTexture.Height * 1.5f);
                    else g.FillEllipse(Brushes.Red, heartX + 3, heartY + 4, 12, 12);
                }

                if (isInSubMenu)
                {
                    int subMenuX = menuBoxX + menuBoxW + 15;
                    int subMenuY = statBoxY;
                    int subMenuW = 400;
                    int subMenuH = (menuBoxY + menuBoxH) - subMenuY;

                    g.FillRectangle(Brushes.Black, subMenuX, subMenuY, subMenuW, subMenuH);
                    g.DrawRectangle(thickWhitePen, subMenuX, subMenuY, subMenuW, subMenuH);

                    if (selectedMenuIndex == 0)
                    {
                        int heartBaseX = subMenuX + 35;
                        int itemTextX = heartBaseX + 35;

                        int maxSlots = 8;
                        int slotStartY = subMenuY + 30;
                        int slotSpacing = 35;

                        for (int i = 0; i < maxSlots; i++)
                        {
                            int slotY = slotStartY + (i * slotSpacing);
                            if (i < inventory.Count)
                            {
                                Brush color = (i == selectedItemIndex && currentInvState != InventoryState.SelectingItem) ? Brushes.Yellow : Brushes.White;
                                g.DrawString(inventory[i].Name, pixelFont, color, itemTextX, slotY);
                            }
                            else
                            {
                                g.DrawString("---", pixelFont, Brushes.Gray, itemTextX, slotY);
                            }
                        }

                        if (currentInvState == InventoryState.SelectingItem)
                        {
                            int itemHeartY = slotStartY + (selectedItemIndex * slotSpacing) + 5;

                            if (heartTexture != null)
                                g.DrawImage(heartTexture, heartBaseX, itemHeartY, heartTexture.Width * 1.5f, heartTexture.Height * 1.5f);
                            else
                                g.FillEllipse(Brushes.Red, heartBaseX + 5, itemHeartY + 5, 12, 12);
                        }

                        if (currentInvState == InventoryState.SelectingAction)
                        {
                            string[] actions = { "USE", "INFO", "DROP" };
                            int actionY = subMenuY + subMenuH - 60;

                            int zoneWidth = (subMenuW - 70) - 195;

                            for (int i = 0; i < actions.Length; i++)
                            {
                                Brush color = (i == selectedActionIndex) ? Brushes.Yellow : Brushes.White;

                                int actionX = subMenuX + 35 + (i * zoneWidth);

                                g.DrawString(actions[i], pixelFont, color, actionX, actionY);

                                if (i == selectedActionIndex)
                                {
                                    if (heartTexture != null)
                                    {
                                        g.DrawImage(heartTexture, actionX - 25, actionY + 5, heartTexture.Width * 1.2f, heartTexture.Height * 1.2f);
                                    }
                                    else
                                    {
                                        g.FillEllipse(Brushes.Red, actionX - 20, actionY + 10, 12, 12);
                                    }
                                }
                            }
                        }
                    }
                    else if (selectedMenuIndex == 1)
                    {
                        g.DrawString($"\"{PlayerName}\"", pixelFont, Brushes.White, subMenuX + 35, subMenuY + 25);
                        g.DrawString("LV  1", pixelFontSmall, Brushes.White, subMenuX + 35, subMenuY + 65);
                        g.DrawString($"HP  {CurrentHP}/{MaxHP}", pixelFontSmall, Brushes.White, subMenuX + 35, subMenuY + 95);

                        g.DrawString($"AT  {Attack} ({WeaponAttack})", pixelFontSmall, Brushes.White, subMenuX + 35, subMenuY + 145);
                        g.DrawString($"DF  {Defense} ({ArmorDefense})", pixelFontSmall, Brushes.White, subMenuX + 35, subMenuY + 180);

                        g.DrawString($"WEAPON: {EquippedWeapon}", pixelFontSmall, Brushes.White, subMenuX + 35, subMenuY + 230);
                        g.DrawString($"ARMOR:  {EquippedArmor}", pixelFontSmall, Brushes.White, subMenuX + 35, subMenuY + 270);
                    }
                }
            }
        }

        private void DrawSansDialogue(Graphics g)
        {
            if (dialogueQueue.Count == 0) return;

            DialogueLine currentLine = dialogueQueue[dialogueIndex];
            string fullText = currentLine.Text;
            int visibleChars = (int)dialogueCharProgress;
            string visibleText = fullText.Substring(0, Math.Min(visibleChars, fullText.Length));

            int boxX = 40;
            int boxY = 30;
            int boxW = 720;
            int boxH = 150;

            using (Pen thickWhitePen = new Pen(Color.White, 5f))
            {
                thickWhitePen.Alignment = PenAlignment.Inset;
                g.FillRectangle(Brushes.Black, boxX, boxY, boxW, boxH);
                g.DrawRectangle(thickWhitePen, boxX, boxY, boxW, boxH);
            }

            int textOffsetX = 30;
            Font fontToUse = pixelFont;

            if (currentLine.IsSansFont)
            {
                fontToUse = sansFont;
                Bitmap face = GetSansFace(currentLine.Expression);

                if (face != null)
                {
                    const float faceScale = 2.6f;
                    float faceW = face.Width * faceScale;
                    float faceH = face.Height * faceScale;
                    float faceX = boxX + 25;
                    float faceY = boxY + (boxH - faceH) / 2f;

                    g.DrawImage(face, faceX, faceY, faceW, faceH);
                    textOffsetX = (int)(25 + faceW + 25);
                }
            }

            var prevHint = g.TextRenderingHint;

            g.TextRenderingHint = currentLine.IsSansFont
                ? System.Drawing.Text.TextRenderingHint.AntiAlias
                : System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            g.DrawString("* " + visibleText, fontToUse, Brushes.White, boxX + textOffsetX, boxY + 25);

            g.TextRenderingHint = prevHint;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
