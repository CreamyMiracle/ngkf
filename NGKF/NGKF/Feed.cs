using GameOverlay.Drawing;
using GameOverlay.Windows;
using SharpDX.Direct2D1;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NGKF.Feed;

namespace NGKF
{
    public class Feed : IDisposable
    {
        private SolidBrush _backgroundBrush;
        private SolidBrush _feedBrush;
        private SolidBrush _redBrush;
        private Font _feedFont;

        private readonly ConcurrentDictionary<string, FeedLine> _feedLines = new ConcurrentDictionary<string, FeedLine>();

        private readonly GraphicsWindow _window;

        private readonly int _fadeStartMillis;
        private readonly int _disappearMillis;
        private readonly int _feedStartX;
        private readonly int _feedStartY;
        private readonly int _feedFontSize;
        private readonly string _feedFontName;
        private readonly int _feedPartSpacing;

        private readonly bool _showOCRBounds;
        private readonly int _ocrStartX;
        private readonly int _ocrStartY;
        private readonly int _ocrWidth;
        private readonly int _ocrHeight;

        private readonly GameOverlay.Drawing.Color _feedColor;

        private readonly List<string> _statPropertyPaths = new List<string>();
        private readonly List<string> _statPropertyExplanations = new List<string>();

        public Feed()
        {
            _fadeStartMillis = int.Parse(ConfigurationManager.AppSettings.Get("FadeStartMillis"));
            _disappearMillis = int.Parse(ConfigurationManager.AppSettings.Get("DisappearMillis"));
            _feedStartX = int.Parse(ConfigurationManager.AppSettings.Get("FeedStartX"));
            _feedStartY = int.Parse(ConfigurationManager.AppSettings.Get("FeedStartY"));
            _feedFontSize = int.Parse(ConfigurationManager.AppSettings.Get("FeedFontSize"));
            _feedFontName = ConfigurationManager.AppSettings.Get("FeedFontName");
            _feedPartSpacing = int.Parse(ConfigurationManager.AppSettings.Get("FeedPartSpacing"));

            _statPropertyPaths = new List<string>(ConfigurationManager.AppSettings.Get("StatPropertyPaths").Split(new char[] { ';' }));
            _statPropertyExplanations = new List<string>(ConfigurationManager.AppSettings.Get("StatPropertyExplanations").Split(new char[] { ';' }));

            _ocrStartX = int.Parse(ConfigurationManager.AppSettings.Get("OcrStartX"));
            _ocrStartY = int.Parse(ConfigurationManager.AppSettings.Get("OcrStartY"));
            _ocrWidth = int.Parse(ConfigurationManager.AppSettings.Get("OcrWidth"));
            _ocrHeight = int.Parse(ConfigurationManager.AppSettings.Get("OcrHeight"));
            _showOCRBounds = bool.Parse(ConfigurationManager.AppSettings.Get("ShowOCRBounds"));

            List<int> feedColorComponents = ConfigurationManager.AppSettings.Get("FeedColorRGB").Split(",").Select(i => int.Parse(i)).ToList();
            _feedColor = new GameOverlay.Drawing.Color(feedColorComponents[0], feedColorComponents[1], feedColorComponents[2]);

            var gfx = new Graphics()
            {
                MeasureFPS = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true
            };

            int totalWidth = Screen.AllScreens.ToList().Sum(screen => screen.Bounds.Width);
            int maxHeight = Screen.AllScreens.ToList().MaxBy(screen => screen.Bounds.Height).Bounds.Height;

            _window = new GraphicsWindow(0, 0, totalWidth, maxHeight, gfx)
            {
                FPS = 60,
                IsTopmost = true,
                IsVisible = true
            };

            _window.DestroyGraphics += _window_DestroyGraphics;
            _window.DrawGraphics += _window_DrawGraphics;
            _window.SetupGraphics += _window_SetupGraphics;
        }

        public void StatHandler(object sender, EventArgs e)
        {
            FeedLine line = (FeedLine)sender;
            _feedLines.TryAdd(line.Id, line);
        }

        private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            _backgroundBrush = gfx.CreateSolidBrush(0, 0, 0, 0);
            _feedBrush = gfx.CreateSolidBrush(_feedColor);
            _redBrush = gfx.CreateSolidBrush(255, 0, 0);
            _feedFont = gfx.CreateFont(_feedFontName, _feedFontSize);
        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
        }

        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            DateTime now = DateTime.UtcNow;

            var gfx = e.Graphics;
            gfx.ClearScene(_backgroundBrush);

            if (_showOCRBounds)
            {
                gfx.DrawRectangle(_redBrush, _ocrStartX, _ocrStartY, _ocrStartX + _ocrWidth, _ocrStartY + _ocrHeight, 2.0f);
            }

            int currY = _feedStartY;
            int yInc = _feedFontSize;

            List<String> linesToBeRemoved = new List<String>();

            foreach (var kvPair in _feedLines.OrderBy(line => line.Value.TimeStamp))
            {
                string lineId = kvPair.Key;
                FeedLine line = kvPair.Value;
                float totalMillis = (float)(now - line.TimeStamp).TotalMilliseconds;

                if (totalMillis > _fadeStartMillis)
                {
                    float millisDiff = totalMillis - _fadeStartMillis;
                    float fadeMillis = _disappearMillis - _fadeStartMillis;

                    if (millisDiff >= fadeMillis)
                    {
                        linesToBeRemoved.Add(lineId);
                        continue;
                    }

                    float normalized = 1 - Normalize(millisDiff, 0, fadeMillis, 0, 1);
                    _feedBrush.Brush.Opacity = normalized;
                }
                else
                {
                    _feedBrush.Brush.Opacity = 1;
                }

                int currX = _feedStartX;

                for (int i = 0; i < _statPropertyExplanations.Count; i++)
                {
                    string explanation = _statPropertyExplanations[i];
                    string path = _statPropertyPaths[i];
                    gfx.DrawText(_feedFont, _feedBrush, currX, currY, explanation + FollowPropertyPath(line.Stats, path).ToString());
                    currX += _feedPartSpacing;
                }

                currY += yInc;
            }

            foreach (string lineId in linesToBeRemoved)
            {
                _feedLines.Remove(lineId, out var _);
            }
        }

        public static object FollowPropertyPath(object value, string path)
        {
            Type currentType = value.GetType();

            foreach (string propertyName in path.Split('.'))
            {
                PropertyInfo property = currentType.GetProperty(propertyName);
                value = property.GetValue(value, null);
                currentType = property.PropertyType;
            }
            return value;
        }

        private float Normalize(float val, float valmin, float valmax, float min, float max)
        {
            return (((val - valmin) / (valmax - valmin)) * (max - min)) + min;
        }

        public void Run()
        {
            _window.Create();
            _window.Join();
        }

        ~Feed()
        {
            Dispose(false);
        }

        #region IDisposable Support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _window.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}