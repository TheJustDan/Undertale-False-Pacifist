using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

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

            this.MouseDown += Form1_MouseDown;
            this.KeyDown += (s, e) => pressedKeys.Add(e.KeyCode);
            this.KeyUp += (s, e) => pressedKeys.Remove(e.KeyCode);
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            float viewportWorldWidth = this.ClientSize.Width / cameraZoom;
            float playerCenterX = player.X + (player.Width / 2f);
            float currentCameraX = playerCenterX - (viewportWorldWidth / 2f);

            if (locationBackground != null)
            {
                currentCameraX = ClampCamera(currentCameraX, viewportWorldWidth, LocationWorldWidth);
            }

            if (e.Button == MouseButtons.Left)
            {
                float colWorldX = (e.X / cameraZoom) + (currentCameraX * parallaxFactor);

                foregroundColumns.Add(colWorldX);
                SaveColumns();
                this.Invalidate();
            }
            else if (e.Button == MouseButtons.Right && columnTexture != null)
            {
                for (int i = foregroundColumns.Count - 1; i >= 0; i--)
                {
                    float colX = foregroundColumns[i];

                    float colScreenX = (colX - (currentCameraX * parallaxFactor)) * cameraZoom;
                    float colScreenWidth = columnTexture.Width * cameraZoom;

                    if (e.X >= colScreenX && e.X <= colScreenX + colScreenWidth)
                    {
                        foregroundColumns.RemoveAt(i);
                        SaveColumns();
                        this.Invalidate();
                        break;
                    }
                }
            }
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

        private void SaveColumns()
        {
            List<string> lines = new List<string>();
            foreach (float x in foregroundColumns)
            {
                lines.Add(x.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            File.WriteAllLines(columnsFilePath, lines);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                float viewportWorldWidth = this.ClientSize.Width / cameraZoom;
                float playerCenterX = player.X + (player.Width / 2f);
                float currentCameraX = playerCenterX - (viewportWorldWidth / 2f);

                if (locationBackground != null)
                    currentCameraX = ClampCamera(currentCameraX, viewportWorldWidth, LocationWorldWidth);
                float colWorldX = player.X + (currentCameraX * (parallaxFactor - 1f));

                foregroundColumns.Add(colWorldX);
                this.Invalidate();
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
                        selectedMenuIndex = (selectedMenuIndex + 1) % 3;

                    if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
                        selectedMenuIndex = (selectedMenuIndex - 1 + 3) % 3;

                    if (e.KeyCode == Keys.Z)
                    {
                        if ((selectedMenuIndex == 0 && inventory.Count > 0) || selectedMenuIndex == 1 || selectedMenuIndex == 2)
                        {
                            isInSubMenu = true;
                            selectedItemIndex = 0;
                            currentInvState = InventoryState.SelectingItem;
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
                                selectedItemIndex = (selectedItemIndex + 1) % inventory.Count;

                            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
                                selectedItemIndex = (selectedItemIndex - 1 + inventory.Count) % inventory.Count;

                            if (e.KeyCode == Keys.X)
                                isInSubMenu = false;

                            if (e.KeyCode == Keys.Z && inventory.Count > 0)
                            {
                                currentInvState = InventoryState.SelectingAction;
                                selectedActionIndex = 0;
                            }
                        }
                        else if (currentInvState == InventoryState.SelectingAction)
                        {
                            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right)
                                selectedActionIndex = (selectedActionIndex + 1) % 3;

                            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left)
                                selectedActionIndex = (selectedActionIndex - 1 + 3) % 3;

                            if (e.KeyCode == Keys.X)
                                currentInvState = InventoryState.SelectingItem;

                            if (e.KeyCode == Keys.Z)
                            {
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
                        if (e.KeyCode == Keys.X) isInSubMenu = false;
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

            float playerCenterX = player.X + (player.Width / 2f);
            float playerCenterY = player.Y + (player.Height / 2f);

            float viewportWorldWidth = this.ClientSize.Width / cameraZoom;
            float viewportWorldHeight = this.ClientSize.Height / cameraZoom;

            float cameraX = playerCenterX - (viewportWorldWidth / 1.75f);
            float cameraY = 0f;

            if (locationBackground != null)
            {
                cameraX = ClampCamera(cameraX, viewportWorldWidth, LocationWorldWidth);
            }

            e.Graphics.ScaleTransform(cameraZoom, cameraZoom);
            e.Graphics.TranslateTransform(-cameraX, -cameraY);

            if (locationBackground != null)
            {
                e.Graphics.DrawImage(locationBackground, 0, 0, LocationWorldWidth, LocationWorldHeight);
            }

            e.Graphics.ResetTransform();

            float playerScreenX = (player.X - cameraX) * cameraZoom;
            float playerScreenY = (player.Y - cameraY) * cameraZoom;

            using (Matrix playerMatrix = new Matrix())
            {
                playerMatrix.Translate(playerScreenX - player.X, playerScreenY - player.Y);
                e.Graphics.Transform = playerMatrix;
                player.Draw(e.Graphics, friskSprite);
            }

            using (Matrix playerMatrix = new Matrix())
            {
                playerMatrix.Translate(playerScreenX - player.X, playerScreenY - player.Y);
                e.Graphics.Transform = playerMatrix;
                player.Draw(e.Graphics, friskSprite);
            }
            e.Graphics.ResetTransform();

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

            e.Graphics.ResetTransform();

            if (isMenuOpen)
            {
                DrawInventory(e.Graphics);
            }
        }


        private void DrawInventory(Graphics g)
        {
            int statBoxX = 30;
            int statBoxY = 40;
            int statBoxW = 200;
            int statBoxH = 160;

            if (inventoryBox != null) g.DrawImage(inventoryBox, statBoxX, statBoxY, statBoxW, statBoxH);
            else g.DrawRectangle(Pens.White, statBoxX, statBoxY, statBoxW, statBoxH);

            g.DrawString(PlayerName, pixelFont, Brushes.White, statBoxX + 20, statBoxY + 20);
            g.DrawString("LV  1", pixelFontSmall, Brushes.White, statBoxX + 20, statBoxY + 65);
            g.DrawString($"HP  {CurrentHP}/{MaxHP}", pixelFontSmall, Brushes.White, statBoxX + 20, statBoxY + 95);
            g.DrawString($"G   {Gold}", pixelFontSmall, Brushes.White, statBoxX + 20, statBoxY + 125);

            int menuBoxX = 30;
            int menuBoxY = statBoxY + statBoxH + 12;
            int menuBoxW = 200;
            int menuBoxH = 200;

            if (inventoryBox != null) g.DrawImage(inventoryBox, menuBoxX, menuBoxY, menuBoxW, menuBoxH);
            else g.DrawRectangle(Pens.White, menuBoxX, menuBoxY, menuBoxW, menuBoxH);

            string[] menuOptions = { "ITEM", "STAT", "CELL" };
            int menuTextX = menuBoxX + 60;
            int menuItemStartY = menuBoxY + 35;
            int menuItemSpacing = 55;

            for (int i = 0; i < menuOptions.Length; i++)
            {
                g.DrawString(menuOptions[i], pixelFont, Brushes.White, menuTextX, menuItemStartY + (i * menuItemSpacing));
            }

            if (!isInSubMenu)
            {
                int heartY = menuItemStartY + (selectedMenuIndex * menuItemSpacing) + 4;
                int heartX = menuBoxX + 28;
                if (heartTexture != null) g.DrawImage(heartTexture, heartX, heartY, heartTexture.Width * 1.5f, heartTexture.Height * 1.5f);
                else g.FillEllipse(Brushes.Red, heartX + 3, heartY + 4, 12, 12);
            }

            if (isInSubMenu)
            {
                int subMenuX = menuBoxX + menuBoxW + 20;
                int subMenuY = statBoxY;
                int subMenuW = 310;
                int subMenuH = (menuBoxY + menuBoxH) - subMenuY;
                int textOffsetX = subMenuX + 50;

                if (inventoryBox != null) g.DrawImage(inventoryBox, subMenuX, subMenuY, subMenuW, subMenuH);
                else g.DrawRectangle(Pens.White, subMenuX, subMenuY, subMenuW, subMenuH);

                if (selectedMenuIndex == 0)
                {
                    int maxSlots = 8;
                    int slotStartY = subMenuY + 25;
                    int slotSpacing = 28;

                    for (int i = 0; i < maxSlots; i++)
                    {
                        int slotY = slotStartY + (i * slotSpacing);
                        if (i < inventory.Count)
                        {
                            Brush color = (i == selectedItemIndex && currentInvState != InventoryState.SelectingItem) ? Brushes.Yellow : Brushes.White;
                            g.DrawString(inventory[i].Name, pixelFont, color, textOffsetX, slotY);
                        }
                        else
                        {
                            g.DrawString("---", pixelFont, Brushes.Gray, textOffsetX, slotY);
                        }
                    }

                    if (currentInvState == InventoryState.SelectingItem)
                    {
                        int itemHeartY = slotStartY + (selectedItemIndex * slotSpacing) + 4;
                        if (heartTexture != null) g.DrawImage(heartTexture, textOffsetX - 28, itemHeartY, heartTexture.Width * 1.5f, heartTexture.Height * 1.5f);
                    }

                    if (currentInvState == InventoryState.SelectingAction)
                    {
                        string[] actions = { "USE", "INFO", "DROP" };
                        int zoneWidth = subMenuW / 3;
                        int actionY = subMenuY + subMenuH - 45;

                        for (int i = 0; i < actions.Length; i++)
                        {
                            Brush color = (i == selectedActionIndex) ? Brushes.Yellow : Brushes.White;
                            int actionX = subMenuX + (i * zoneWidth) + 15;
                            g.DrawString(actions[i], pixelFont, color, actionX, actionY);

                            if (i == selectedActionIndex && heartTexture != null)
                            {
                                g.DrawImage(heartTexture, actionX - 20, actionY + 5, heartTexture.Width * 1.2f, heartTexture.Height * 1.2f);
                            }
                        }
                    }
                }
                else if (selectedMenuIndex == 1)
                {
                    g.DrawString($"\"{PlayerName}\"", pixelFont, Brushes.White, subMenuX + 30, subMenuY + 20);
                    g.DrawString("LV  1", pixelFontSmall, Brushes.White, subMenuX + 30, subMenuY + 55);
                    g.DrawString($"HP  {CurrentHP}/{MaxHP}", pixelFontSmall, Brushes.White, subMenuX + 30, subMenuY + 85);

                    g.DrawString($"AT  {Attack} ({WeaponAttack})", pixelFontSmall, Brushes.White, subMenuX + 30, subMenuY + 130);
                    g.DrawString($"DF  {Defense} ({ArmorDefense})", pixelFontSmall, Brushes.White, subMenuX + 30, subMenuY + 165);

                    g.DrawString($"WEAPON: {EquippedWeapon}", pixelFontSmall, Brushes.White, subMenuX + 30, subMenuY + 210);
                    g.DrawString($"ARMOR:  {EquippedArmor}", pixelFontSmall, Brushes.White, subMenuX + 30, subMenuY + 245);
                }
            }
        }
    }
}