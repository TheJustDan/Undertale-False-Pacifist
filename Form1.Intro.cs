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

        private class IntroScene
        {
            public Bitmap Image;
            public List<string> Lines;
            public bool IsInstant;

            public IntroScene(Bitmap image, bool isInstant, params string[] lines)
            {
                Image = image;
                IsInstant = isInstant;
                Lines = new List<string>(lines);
            }

            public IntroScene(Bitmap image, params string[] lines)
                : this(image, false, lines)
            {
            }
        }

        private enum IntroPhase { FadeIn, Typing, Hold, FadeOut }

        private bool introActive = false;
        private List<IntroScene> introScenes = new List<IntroScene>();
        private Bitmap[] introBitmaps = new Bitmap[13];

        private int introSceneIndex = 0;
        private int introLineIndex = 0;
        private IntroPhase introPhase = IntroPhase.FadeIn;

        private float introCharProgress = 0f;
        private const float IntroCharsPerTick = 0.35f;

        private Font introFont;
        private float introFadeAlpha = 255f;
        private const float IntroFadeSpeed = 7f;
        private const float IntroImageScale = 1.15f;

        private const float LogoSceneImageYOffset = 60f;

        private int introHoldTimer = 0;

        private float introImageBottomY = 0f;
        private List<string> introWrappedLines = new List<string>();
        private const float IntroTextMaxWidth = 620f;

        private bool gameFadeInActive = false;
        private float gameFadeInAlpha = 255f;
        private const float GameFadeInSpeed = 4f;


        private void LoadIntroAssets()
        {

            if (introFont == null)
            {
                string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fonts", "determination_mono.otf");

                if (File.Exists(fontPath) && pfc.Families.Length == 0)
                {
                    pfc.AddFontFile(fontPath);
                }

                if (pfc.Families.Length > 0)
                {
                    introFont = new Font(pfc.Families[0], 28, FontStyle.Regular);
                }
                else
                {
                    introFont = new Font("Courier New", 28, FontStyle.Bold);
                }
            }

            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textures", "intro");

            for (int i = 0; i < 13; i++)
            {
                string path = Path.Combine(dir, $"{i + 1}.bmp");
                introBitmaps[i] = File.Exists(path) ? new Bitmap(path) : null;
            }

            introScenes = new List<IntroScene>
            {
                new IntroScene(introBitmaps[0],
                    "Long, long ago, two races \nruled the Earth: HUMANS \nand MONSTERS."),

                new IntroScene(introBitmaps[1],
                    "One day, a war broke out \nbetween them..."),

                new IntroScene(introBitmaps[2],
                    "But this world has \nalready come to an end.",
                    "It was destroyed."),

                new IntroScene(introBitmaps[3],
                    "And sold for a single \nhuman SOUL."),

                new IntroScene(introBitmaps[4],
                    "Time has turned back.",
                    "Mount Abbott still \nholds its secrets."),

                new IntroScene(introBitmaps[5],
                    "Every time, you come \nback.",
                    "Over and over again."),

                new IntroScene(introBitmaps[6],
                    "You're falling, \nexpecting to hit the \nground."),

                new IntroScene(introBitmaps[7],
                    "Once again, you're \nflying down into the \ndepths of Mount Abbott."),

                new IntroScene(introBitmaps[8],
                    "That familiar fake \nflower again...",
                    "Everything looks \nexactly the same as \nbefore...",
                    "But with every step you \ntake, someone else's \nchoice echoes back."),

                new IntroScene(introBitmaps[9],
                    "Do you believe you can \nfix everything?",
                    "Do you believe that \nthis world has \nforgotten the taste \nof dust?",
                    "The fairy tale begins \nagain.",
                    "But it already has a \ndifferent owner."),

                new IntroScene(introBitmaps[10], true, ""),

                new IntroScene(introBitmaps[11], true,
                    "Undertale by Toby Fox\n\nUndertale False Pacifist \nby The Just Dan\n\nTextures by Toby fox\n\nSounds by Toby Fox"),

                new IntroScene(introBitmaps[12], true, "")
            };
        }

        private void StartIntro()
        {
            LoadIntroAssets();

            introActive = true;
            introSceneIndex = 0;
            introLineIndex = 0;

            IntroScene firstScene = introScenes[0];
            PrepareIntroLineWrap();

            if (firstScene.IsInstant)
            {
                introFadeAlpha = 0f;
                introPhase = IntroPhase.Typing;

                if (firstScene.Lines.Count > 0)
                {
                    introCharProgress = firstScene.Lines[introLineIndex].Length;
                }

                PlaySoundEffect("logo.mp3");
            }
            else
            {
                introCharProgress = 0f;
                introFadeAlpha = 255f;
                introPhase = IntroPhase.FadeIn;
            }

            StopMusic();
        }

        private void EndIntro()
        {
            introActive = false;
            introFadeAlpha = 0f;

            gameFadeInActive = true;
            gameFadeInAlpha = 255f;

            if (introSceneIndex < 12)
            {
                PlayLocationMusic();
            }
        }

        private void SkipIntro()
        {
            EndIntro();
        }

        private void UpdateIntro()
        {
            switch (introPhase)
            {
                case IntroPhase.FadeIn:
                    introFadeAlpha -= IntroFadeSpeed;
                    if (introFadeAlpha <= 0f)
                    {
                        introFadeAlpha = 0f;
                        introPhase = IntroPhase.Typing;
                    }
                    break;

                case IntroPhase.Typing:
                    {
                        string fullText = introScenes[introSceneIndex].Lines[introLineIndex];

                        if (introCharProgress < fullText.Length)
                        {
                            int prevCount = (int)introCharProgress;
                            introCharProgress = Math.Min(fullText.Length, introCharProgress + IntroCharsPerTick);
                            int currentCount = (int)introCharProgress;

                            if (currentCount > prevCount)
                            {
                                char newChar = fullText[currentCount - 1];

                                if (newChar != ' ' && newChar != '\n' && newChar != '\r')
                                {
                                    PlaySoundEffect("text.mp3");
                                }
                            }
                        }

                        if (introCharProgress >= fullText.Length)
                        {
                            introHoldTimer = CalcHoldTicks(fullText);
                            introPhase = IntroPhase.Hold;
                        }
                    }
                    break;

                case IntroPhase.Hold:
                    introHoldTimer--;
                    if (introHoldTimer <= 0)
                    {
                        AdvanceIntroLine();
                    }
                    break;

                case IntroPhase.FadeOut:
                    introFadeAlpha += IntroFadeSpeed;
                    if (introFadeAlpha >= 255f)
                    {
                        introFadeAlpha = 255f;
                        AdvanceIntroScene();
                    }
                    break;
            }
        }

        private int CalcHoldTicks(string text)
        {
            return OneSecondTicks + (int)(text.Length * 1.3f);
        }

        private void AdvanceIntroLine()
        {
            introLineIndex++;
            introCharProgress = 0f;

            if (introLineIndex >= introScenes[introSceneIndex].Lines.Count)
            {
                if (introScenes[introSceneIndex].IsInstant)
                {
                    AdvanceIntroScene();
                }
                else
                {
                    introPhase = IntroPhase.FadeOut;
                }
            }
            else
            {
                PrepareIntroLineWrap();
                introPhase = IntroPhase.Typing;
            }
        }

        private void AdvanceIntroScene()
        {
            introSceneIndex++;
            introLineIndex = 0;

            if (introSceneIndex >= introScenes.Count)
            {
                EndIntro();
            }
            else
            {
                IntroScene currentScene = introScenes[introSceneIndex];
                PrepareIntroLineWrap();

                if (currentScene.IsInstant)
                {
                    introFadeAlpha = 0f;
                    introPhase = IntroPhase.Typing;

                    if (currentScene.Lines.Count > 0)
                    {
                        introCharProgress = currentScene.Lines[introLineIndex].Length;
                    }
                    else
                    {
                        introCharProgress = 0f;
                    }

                    if (introSceneIndex == 12)
                    {
                        PlayLocationMusic();
                    }
                    else
                    {
                        PlaySoundEffect("logo.mp3");
                    }
                }
                else
                {
                    introCharProgress = 0f;
                    introFadeAlpha = 255f;
                    introPhase = IntroPhase.FadeIn;
                }
            }
        }
        private void HandleIntroKeyDown(Keys key)
        {

        }

        private void PrepareIntroLineWrap()
        {
            string fullText = introScenes[introSceneIndex].Lines[introLineIndex];
            using (Graphics g = this.CreateGraphics())
            {
                introWrappedLines = WrapText(g, fullText, introFont, IntroTextMaxWidth);
            }
        }

        private List<string> WrapText(Graphics g, string text, Font font, float maxWidth)
        {
            List<string> resultLines = new List<string>();

            string[] rawLines = text.Replace("\r", "").Split('\n');

            foreach (string rawLine in rawLines)
            {
                string[] words = rawLine.Split(' ');
                string current = "";

                foreach (string word in words)
                {
                    string test = string.IsNullOrEmpty(current) ? word : current + " " + word;

                    if (g.MeasureString(test, font).Width > maxWidth && !string.IsNullOrEmpty(current))
                    {
                        resultLines.Add(current);
                        current = word;
                    }
                    else
                    {
                        current = test;
                    }
                }

                if (!string.IsNullOrEmpty(current))
                {
                    resultLines.Add(current);
                }
            }

            return resultLines;
        }

        private const float IntroTextAreaHeight = 150f;
        private const float IntroImageTopMargin = 20f;
        private const float IntroTextLeftMargin = 150f;
        private const float IntroTextBottomMargin = 150f;

        private void DrawIntro(Graphics g, float virtualWidth, float virtualHeight)
        {
            g.Clear(Color.Black);

            IntroScene scene = introScenes[introSceneIndex];

            introImageBottomY = IntroImageTopMargin;

            if (scene.Image != null)
            {
                DrawIntroImage(g, scene.Image, virtualWidth, virtualHeight);
            }

            if (introLineIndex < scene.Lines.Count)
            {
                DrawIntroText(g, virtualWidth, virtualHeight);
            }

            if (introFadeAlpha > 0f)
            {
                int a = (int)Math.Max(0, Math.Min(255, introFadeAlpha));
                using (SolidBrush fadeBrush = new SolidBrush(Color.FromArgb(a, Color.Black)))
                {
                    g.FillRectangle(fadeBrush, 0, 0, virtualWidth, virtualHeight);
                }
            }
        }

        private void DrawIntroImage(Graphics g, Bitmap image, float virtualWidth, float virtualHeight)
        {
            float availableW = virtualWidth - IntroTextLeftMargin;
            float availableH = virtualHeight - IntroTextAreaHeight - IntroImageTopMargin;

            float srcRatio = image.Width / (float)image.Height;
            float boxRatio = availableW / availableH;

            float drawW, drawH;
            if (srcRatio > boxRatio)
            {
                drawW = availableW;
                drawH = drawW / srcRatio;
            }
            else
            {
                drawH = availableH;
                drawW = drawH * srcRatio;
            }

            drawW *= IntroImageScale;
            drawH *= IntroImageScale;

            float boxCenterX = IntroTextLeftMargin / 2f + availableW / 2f;
            float boxCenterY = IntroImageTopMargin + availableH / 2f;

            float drawX = boxCenterX - drawW / 2f;
            float drawY = boxCenterY - drawH / 2f;

            // Логотип (11.bmp, сцена index 10) - опустить чуть ниже стандартной позиции
            if (introSceneIndex == 10)
            {
                drawY += LogoSceneImageYOffset;
            }

            g.DrawImage(image, drawX, drawY, drawW, drawH);

            introImageBottomY = drawY + drawH;
        }

        private void DrawIntroText(Graphics g, float virtualWidth, float virtualHeight)
        {
            var prevHint = g.TextRenderingHint;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            float lineHeight = introFont.GetHeight(g) + 6f;

            IntroScene currentScene = introScenes[introSceneIndex];
            float startY;

            if (currentScene.Image != null)
            {
                startY = introImageBottomY - 180f;
            }
            else
            {
                startY = 330f;
            }

            float blockBottom = startY + introWrappedLines.Count * lineHeight;
            float maxBottomY = virtualHeight - IntroTextBottomMargin;

            if (blockBottom > maxBottomY)
            {
                startY -= (blockBottom - maxBottomY);
            }

            int remaining = (int)introCharProgress;

            for (int i = 0; i < introWrappedLines.Count; i++)
            {
                string lineText = introWrappedLines[i];
                int take = Math.Max(0, Math.Min(remaining, lineText.Length));
                string visible = lineText.Substring(0, take);
                remaining -= lineText.Length + 1;

                if (visible.Length > 0)
                {
                    float x = 188f;
                    g.DrawString(visible, introFont, Brushes.White, x, startY + i * lineHeight);
                }

                if (remaining < 0) break;
            }

            g.TextRenderingHint = prevHint;
        }
    }
}
