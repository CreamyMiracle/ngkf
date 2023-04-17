using GameOverlay.Drawing;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace NGKF
{
    public class OCR
    {
        private readonly int _ocrStartX;
        private readonly int _ocrStartY;
        private readonly int _ocrWidth;
        private readonly int _ocrHeight;

        private readonly List<System.Drawing.Color> _replacedColors = new List<System.Drawing.Color>();
        private readonly System.Drawing.Color _substituteColor;
        private readonly int _replaceThreshold;
        private readonly int _contrastAdjustment;

        private readonly TesseractEngine _engine;

        public OCR()
        {
            _ocrStartX = int.Parse(ConfigurationManager.AppSettings.Get("OcrStartX"));
            _ocrStartY = int.Parse(ConfigurationManager.AppSettings.Get("OcrStartY"));
            _ocrWidth = int.Parse(ConfigurationManager.AppSettings.Get("OcrWidth"));
            _ocrHeight = int.Parse(ConfigurationManager.AppSettings.Get("OcrHeight"));

            _ocrHeight = int.Parse(ConfigurationManager.AppSettings.Get("OcrHeight"));
            var replacedColorLists = new List<string>(ConfigurationManager.AppSettings.Get("ReplacedColors").Split(new char[] { ';' }));
            foreach (string color in replacedColorLists)
            {
                List<int> replaceComponents = color.Split(",").Select(i => int.Parse(i)).ToList();
                _replacedColors.Add(System.Drawing.Color.FromArgb(replaceComponents[0], replaceComponents[1], replaceComponents[2]));
            }

            List<int> substituteComponents = ConfigurationManager.AppSettings.Get("SubstituteColor").Split(",").Select(i => int.Parse(i)).ToList();
            _substituteColor = System.Drawing.Color.FromArgb(substituteComponents[0], substituteComponents[1], substituteComponents[2]);

            _replaceThreshold = int.Parse(ConfigurationManager.AppSettings.Get("ReplaceThreshold"));
            _contrastAdjustment = int.Parse(ConfigurationManager.AppSettings.Get("ContrastAdjustment"));

            _engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
        }

        public List<string> Capture()
        {
            System.Drawing.Rectangle bounds = new System.Drawing.Rectangle(0, 0, _ocrWidth, _ocrHeight);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(_ocrStartX, _ocrStartY, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
                }

                Bitmap colorFixed = bitmap;
                foreach (System.Drawing.Color replacedColor in _replacedColors)
                {
                    colorFixed = colorFixed.ReplaceColors(replacedColor, _substituteColor, _replaceThreshold);
                }

                using (Bitmap grayScaleBmp = colorFixed.GrayScale())
                {
                    using (Bitmap adjustedBmp = grayScaleBmp.AdjustContrast(_contrastAdjustment))
                    {
                        var res = new List<byte[]>();
                        using (var ms = new MemoryStream())
                        {
                            adjustedBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

                            FileInfo fi3 = new FileInfo("./final.png");
                            adjustedBmp.Save(fi3.FullName);
                            fi3.Delete();

                            List<string> text = Parse(ms.ToArray());

                            Console.WriteLine(String.Join(Environment.NewLine, text.ToArray()));
                            return text;
                        }
                    }
                }
            }
        }

        public List<string> Parse(byte[] bytes)
        {
            List<string> lines = new List<string>();
            try
            {
                using (var img = Pix.LoadFromMemory(bytes))
                {
                    using (var page = _engine.Process(img))
                    {
                        string text = page.GetText();
                        using (var iter = page.GetIterator())
                        {
                            iter.Begin();

                            do
                            {
                                do
                                {
                                    do
                                    {
                                        string line = iter.GetText(PageIteratorLevel.TextLine);
                                        lines.Add(line);
                                    } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                                } while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                            } while (iter.Next(PageIteratorLevel.Block));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return lines;
        }
    }
}