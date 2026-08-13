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

        private Bitmap inventoryBox;
        private Bitmap heartTexture;
        private Bitmap locationBackground;
        private float cameraZoom = 2.5f;
        private float LocationWorldWidth => locationBackground != null ? locationBackground.Width : 0f;
        private float LocationWorldHeight => locationBackground != null ? locationBackground.Height : 0f;

        private PrivateFontCollection pfc = new PrivateFontCollection();
        private Font pixelFont;
        private Font pixelFontSmall;

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

        public Form1()
        {
            InitializeComponent();

            this.Text = "Undertale False Pacifist";
            this.ClientSize = new Size(800, 600);
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

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

            LoadColumns();
            PlayLocationMusic();

            this.KeyDown += (s, e) => pressedKeys.Add(e.KeyCode);
            this.KeyUp += (s, e) => pressedKeys.Remove(e.KeyCode);
        }

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
            HandleInput();
            player.UpdateAnimation();
            this.Invalidate();
        }

        private void HandleInput()
        {
            float dx = 0;
            float dy = 0;

            if (isMenuOpen)
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

            float playerCenterX = player.X + (player.Width / 2f);
            float playerCenterY = player.Y + (player.Height / 2f);

            float viewportWorldWidth = virtualWidth / cameraZoom;
            float viewportWorldHeight = virtualHeight / cameraZoom;

            float cameraX = playerCenterX - (viewportWorldWidth / 1.75f);
            float cameraY = 0f;

            if (locationBackground != null)
            {
                cameraX = ClampCamera(cameraX, viewportWorldWidth, LocationWorldWidth);
            }

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
                {
                    float colScreenX = (colX - (cameraX * parallaxFactor)) * cameraZoom;
                    float colScreenY = (0 - cameraY) * cameraZoom;

                    e.Graphics.DrawImage(columnTexture, colScreenX, colScreenY,
                                         columnTexture.Width * cameraZoom,
                                         columnTexture.Height * cameraZoom);
                }
            }

            e.Graphics.Transform = screenMatrix;

            if (isMenuOpen)
            {
                DrawInventory(e.Graphics);
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
    }
}
