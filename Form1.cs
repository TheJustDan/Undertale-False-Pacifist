using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Undertale_False_Pacifist
{
    public partial class Form1 : Form
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
        private float cameraZoom = 2.5f;
        private float cutsceneCameraZoom = 2.5f;
        private float CurrentCameraZoom => sansEncounterCameraLocked ? cutsceneCameraZoom : cameraZoom;
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

        private Bitmap columnTexture;
        private List<float> foregroundColumns = new List<float>();

        private float parallaxFactor = 2f;
        private string columnsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "columns_data.txt");

        private List<Item> inventory = new List<Item>();

        private bool isFullscreen = false;
        private Rectangle windowedBounds;

<<<<<<< HEAD
=======
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

>>>>>>> Обязательно
        public Form1()
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

            string spritePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "dark_frisk.bmp");

            if (File.Exists(spritePath))
            {
                friskSprite = new Bitmap(spritePath);
                friskSprite.MakeTransparent(Color.White);
            }

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

            inventory.Add(new Item("Stick", "A standard stick."));
            inventory.Add(new Item("Bandage", "Heals 10 HP."));

            this.KeyDown += Form1_KeyDown;

            player = new Player { X = 105f, Y = 100f };

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

<<<<<<< HEAD
            LoadColumns();
=======
            StartIntro();
            LoadColumns();
            LoadSansTrigger();
>>>>>>> Обязательно
            PlayLocationMusic();

            this.KeyDown += (s, e) => pressedKeys.Add(e.KeyCode);
            this.KeyUp += (s, e) => pressedKeys.Remove(e.KeyCode);
        }

<<<<<<< HEAD
=======
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

>>>>>>> Обязательно
        private void LoadColumns()
        {
            if (File.Exists(columnsFilePath))
            {
                string[] lines = File.ReadAllLines(columnsFilePath);
                foreach (string line in lines)
                {
                    if (float.TryParse(line, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x))
                    {
                        foregroundColumns.Add(x);
                    }
                }
            }
        }

<<<<<<< HEAD
        // --- Аудио система ---
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, string strReturn, int iReturnLength, IntPtr hwndCallback);

        private string currentMusic = "";
        private string currentLocationName = "last_corridor";

        private void PlaySoundEffect(string soundFileName)
        {
            string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", soundFileName);
            if (File.Exists(soundPath))
            {
                mciSendString("close sfx", null, 0, IntPtr.Zero);
                mciSendString($"open \"{soundPath}\" type mpegvideo alias sfx", null, 0, IntPtr.Zero);
                mciSendString("play sfx", null, 0, IntPtr.Zero);
            }
        }

        private void PlayLocationMusic()
        {
            string targetMusic = "";

            if (currentLocationName == "last_corridor")
            {
                targetMusic = "background.mp3";
            }

            if (currentMusic == targetMusic) return;

            mciSendString("close bgm", null, 0, IntPtr.Zero);
            currentMusic = "";

            if (!string.IsNullOrEmpty(targetMusic))
            {
                string musicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", targetMusic);
                if (File.Exists(musicPath))
                {
                    mciSendString($"open \"{musicPath}\" type mpegvideo alias bgm", null, 0, IntPtr.Zero);
                    mciSendString("play bgm repeat", null, 0, IntPtr.Zero);
                    currentMusic = targetMusic;
                }
            }
        }

        private void StopMusic()
        {
            mciSendString("close bgm", null, 0, IntPtr.Zero);
            currentMusic = "";
=======
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
>>>>>>> Обязательно
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
<<<<<<< HEAD
=======
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
>>>>>>> Обязательно
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

            HandleInput();
            player.UpdateAnimation();

            if (hasSansTrigger && !sansTriggerConsumed && !isDialogueActive && !IsSansCutsceneActive && !isMenuOpen)
            {
                float playerCenterXForTrigger = player.X + (player.Width / 2f);
                float triggerDistanceX = Math.Abs(playerCenterXForTrigger - sansTriggerPos.X);

                if (triggerDistanceX <= TriggerRadius)
                {
                    sansTriggerConsumed = true;
                    StartSansCutscene();
                }
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
                float playerCenterX = player.X + (player.Width / 2f);
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

                        if (currentLine.IsSansFont && !char.IsWhiteSpace(lastChar))
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
            float dx = 0;
            float dy = 0;

            if (isMenuOpen || isDialogueActive || IsSansCutsceneActive || introActive)
            {
                player.IsMoving = false;
                return;
            }

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
            player.X += dx;
            player.Y += dy;

            if (locationBackground != null)
            {
                float halfW = player.Width / 2f;
                float halfH = player.Height / 2f;
                player.X = Math.Max(-halfW, Math.Min(player.X, LocationWorldWidth - halfW));
                player.Y = Math.Max(-halfH, Math.Min(player.Y, LocationWorldHeight - halfH));
            }
        }

        private float ClampCamera(float cam, float viewportSize, float mapSize)
        {
            if (mapSize <= viewportSize)
            {
                return -(viewportSize - mapSize) / 2f;
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
                    sansVisibleInWorld = false;

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
                new DialogueLine("Usually, those who refuse to hurt anyone move on \nwith a heavy heart.", "serious"),
                new DialogueLine("They're thinking about the sacrifice, about how to \nsave all of us.", "serious"),
                new DialogueLine("But you...", "serious"),
                new DialogueLine("You're walking very quietly. too quietly.", "serious"),
                new DialogueLine("As if you weren't going to decide anything.", "serious"),
                new DialogueLine("it's as if you're just looking for a way to take \nwhat's yours and slip away while no one is looking.", "closed"),

                new DialogueLine("heh. guess i'm just overthinking it.", "wink"),
                new DialogueLine("forget it.", "closed"),
                new DialogueLine("asgore is ahead.", "normal"),
                new DialogueLine("he doesn't want to fight you, but he has no choice.", "side"),
                new DialogueLine("do what you're supposed to do, kid.", "normal"),
                new DialogueLine("Just... remember one thing.", "serious"),
                new DialogueLine("True friends don't abandon each other in a dungeon.", "serious"),
                new DialogueLine("good luck over there.", "normal")
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
<<<<<<< HEAD

            float virtualWidth = 800f;
            float virtualHeight = 600f;

            float screenScale = Math.Min(this.ClientSize.Width / virtualWidth, this.ClientSize.Height / virtualHeight);

            float screenOffsetX = (this.ClientSize.Width - (virtualWidth * screenScale)) / 2f;
            float screenOffsetY = (this.ClientSize.Height - (virtualHeight * screenScale)) / 2f;

            Matrix screenMatrix = new Matrix();
            screenMatrix.Translate(screenOffsetX, screenOffsetY);
            screenMatrix.Scale(screenScale, screenScale);

            e.Graphics.Transform = screenMatrix;
            e.Graphics.SetClip(new Rectangle(0, 0, (int)virtualWidth, (int)virtualHeight));
            e.Graphics.Clear(Color.Black);
=======
>>>>>>> Обязательно

            float virtualWidth = 800f;
            float virtualHeight = 600f;

<<<<<<< HEAD
            float viewportWorldWidth = virtualWidth / cameraZoom;
            float viewportWorldHeight = virtualHeight / cameraZoom;
=======
            float screenScale = Math.Min(this.ClientSize.Width / virtualWidth, this.ClientSize.Height / virtualHeight);
>>>>>>> Обязательно

            float screenOffsetX = (this.ClientSize.Width - (virtualWidth * screenScale)) / 2f;
            float screenOffsetY = (this.ClientSize.Height - (virtualHeight * screenScale)) / 2f;

            lastScreenScale = screenScale;
            lastScreenOffsetX = screenOffsetX;
            lastScreenOffsetY = screenOffsetY;

            using (Matrix screenMatrix = new Matrix())
            {
                screenMatrix.Translate(screenOffsetX, screenOffsetY);
                screenMatrix.Scale(screenScale, screenScale);

<<<<<<< HEAD
            Matrix cameraMatrix = screenMatrix.Clone();
            cameraMatrix.Scale(cameraZoom, cameraZoom);
            cameraMatrix.Translate(-cameraX, -cameraY);
            e.Graphics.Transform = cameraMatrix;

            if (locationBackground != null)
            {
                e.Graphics.DrawImage(locationBackground, 0, 0, LocationWorldWidth, LocationWorldHeight);
            }

            e.Graphics.Transform = screenMatrix;

            float playerScreenX = (player.X - cameraX) * cameraZoom;
            float playerScreenY = (player.Y - cameraY) * cameraZoom;

            using (Matrix playerMatrix = screenMatrix.Clone())
            {
                playerMatrix.Translate(playerScreenX - player.X, playerScreenY - player.Y);
                e.Graphics.Transform = playerMatrix;
                player.Draw(e.Graphics, friskSprite);
            }

            e.Graphics.Transform = screenMatrix;

            if (columnTexture != null)
            {
                foreach (float colX in foregroundColumns)
=======
                e.Graphics.Transform = screenMatrix;
                e.Graphics.SetClip(new Rectangle(0, 0, (int)virtualWidth, (int)virtualHeight));
                e.Graphics.Clear(Color.Black);

                if (introActive)
>>>>>>> Обязательно
                {
                    DrawIntro(e.Graphics, virtualWidth, virtualHeight);
                    return;
                }

                float playerCenterX = player.X + (player.Width / 2f);
                float playerCenterY = player.Y + (player.Height / 2f);

                float viewportWorldWidth = virtualWidth / cameraZoom;
                float viewportWorldHeight = virtualHeight / cameraZoom;

                float cameraX;
                float cameraY = 0f;

                if (sansEncounterCameraLocked)
                {
                    cameraX = cutsceneCameraX;
                }
                else
                {
                    cameraX = playerCenterX - (viewportWorldWidth / 1.75f);

                    if (locationBackground != null)
                    {
                        cameraX = ClampCamera(cameraX, viewportWorldWidth, LocationWorldWidth);
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
                }

                e.Graphics.Transform = screenMatrix;

                float playerScreenX = (player.X - cameraX) * CurrentCameraZoom;
                float playerScreenY = (player.Y - cameraY) * CurrentCameraZoom;

                using (Matrix playerMatrix = screenMatrix.Clone())
                {
                    playerMatrix.Translate(playerScreenX - player.X, playerScreenY - player.Y);
                    e.Graphics.Transform = playerMatrix;
                    player.Draw(e.Graphics, friskSprite);
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
            }
<<<<<<< HEAD

            e.Graphics.Transform = screenMatrix;

            if (isMenuOpen)
            {
                DrawInventory(e.Graphics);
            }
=======
>>>>>>> Обязательно
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
    }
}
