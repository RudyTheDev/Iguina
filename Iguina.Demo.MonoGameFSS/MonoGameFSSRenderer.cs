using Iguina.Defs;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using FontStashSharp;


namespace Iguina.Demo.MonoGame
{
    /// <summary>
    /// Provide rendering for the GUI system.
    /// </summary>
    internal class MonoGameFSSRenderer : Iguina.Drivers.IRenderer
    {
        GraphicsDevice _device;
        SpriteBatch _spriteBatch;
        ContentManager _content;
        string _assetsRoot;
        Texture2D _whiteTexture;

        Dictionary<string, FontSystem> _fontSystems = new();
        Dictionary<string, Texture2D> _textures = new();

        public float GlobalFontScale = 1.15f;

        /// <summary>
        /// Create the monogame renderer.
        /// </summary>
        /// <param name="assetsPath">Root directory to load assets from. Check out the demo project for details.</param>
        public MonoGameFSSRenderer(ContentManager content, GraphicsDevice device, SpriteBatch spriteBatch, string assetsPath)
        {
            _content = content;
            _device = device;
            _spriteBatch = spriteBatch;
            _assetsRoot = assetsPath;

            // create white texture
            _whiteTexture = new Texture2D(_device, 1, 1);
            _whiteTexture.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Load / get font.
        /// </summary>
        DynamicSpriteFont GetFont(string? fontName, int fontSize)
        {        
            var fontNameOrDefault = fontName ?? "arial";
            if (_fontSystems.TryGetValue(fontNameOrDefault, out var font))
                return font.GetFont(fontSize * GlobalFontScale);

            var fontSystem = new FontSystem();
            fontSystem.AddFont(File.ReadAllBytes(fontName ?? "Content/arial.ttf"));
            _fontSystems[fontNameOrDefault] = fontSystem;
            return fontSystem.GetFont(fontSize * GlobalFontScale);
        }

        /// <summary>
        /// Load / get texture.
        /// </summary>
        Texture2D GetTexture(string textureId)
        {
            if (_textures.TryGetValue(textureId, out var texture))
            {
                return texture;
            }

            var path = System.IO.Path.Combine(_assetsRoot, textureId);
            var ret = Texture2D.FromFile(_device, path);
            _textures[textureId] = ret;
            return ret;
        }

        /// <summary>
        /// Load / get effect from id.
        /// </summary>
        Effect? GetEffect(string? effectId)
        {
            if (effectId == null) { return null; }
            return _content.Load<Effect>(effectId);
        }

        /// <summary>
        /// Set active effect id.
        /// </summary>
        void SetEffect(string? effectId)
        {
            if (_currEffectId != effectId)
            {
                _spriteBatch.End();
                _currEffectId = effectId;
                BeginBatch();
            }
        }
        string? _currEffectId;

        /// <summary>
        /// Convert iguina color to mg color.
        /// </summary>
        Microsoft.Xna.Framework.Color ToMgColor(Color color)
        {
            var colorMg = new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A);
            if (color.A < 255)
            {
                float factor = (float)color.A / 255f;
                colorMg.R = (byte)((float)color.R * factor);
                colorMg.G = (byte)((float)color.G * factor);
                colorMg.B = (byte)((float)color.B * factor);
            }
            return colorMg;
        }

        /// <summary>
        /// Called at the beginning of every frame.
        /// </summary>
        public void StartFrame()
        {
            _currEffectId = null;
            _currScissorRegion = null;
            BeginBatch();
        }

        /// <summary>
        /// Called at the end of every frame.
        /// </summary>
        public void EndFrame()
        {
            _spriteBatch.End();
        }

        /// <inheritdoc/>
        public Rectangle GetScreenBounds()
        {
            int screenWidth = _device.Viewport.Width;
            int screenHeight = _device.Viewport.Height;
            return new Rectangle(0, 0, screenWidth, screenHeight);
        }

        /// <inheritdoc/>
        public void DrawTexture(string? effectIdentifier, string textureId, Rectangle destRect, Rectangle sourceRect, Color color)
        {
            SetEffect(effectIdentifier);
            var texture = GetTexture(textureId);
            var colorMg = ToMgColor(color);
            _spriteBatch.Draw(texture,
                new Microsoft.Xna.Framework.Rectangle(destRect.X, destRect.Y, destRect.Width, destRect.Height),
                new Microsoft.Xna.Framework.Rectangle(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height),
                colorMg);
        }

        /// <inheritdoc/>
        public Point MeasureText(string text, string? fontId, int fontSize, float spacing)
        {
            var spriteFont = GetFont(fontId, fontSize);
            Microsoft.Xna.Framework.Vector2 measured = spriteFont.MeasureString(text, Vector2.One, spacing - 1f);
            return new Point((int)measured.X, (int)measured.Y);
        }

        /// <inheritdoc/>
        public int GetTextLineHeight(string? fontId, int fontSize)
        {
            return GetFont(fontId, fontSize).LineHeight;
        }

        /// <inheritdoc/>
        [Obsolete("Note: currently we render outline in a primitive way. To improve performance and remove some visual artifact during transitions, its best to implement a shader that draw text with outline properly.")]
        public void DrawText(string? effectIdentifier, string text, string? fontId, int fontSize, Point position, Color fillColor, Color outlineColor, int outlineWidth, float spacing)
        {
            SetEffect(effectIdentifier);

            SpriteFontBase spriteFont = GetFont(fontId, fontSize);

            // draw outline
            if ((outlineColor.A > 0) && (outlineWidth > 0))
            {
                // because we draw outline in a primitive way, we want it to fade a lot faster than fill color
                if (outlineColor.A < 255)
                {
                    float alphaFactor = (float)(outlineColor.A / 255f);
                    outlineColor.A = (byte)((float)fillColor.A * Math.Pow(alphaFactor, 7));
                }

                // draw outline
                var outline = ToMgColor(outlineColor);
                _spriteBatch.DrawString(spriteFont, text, new Microsoft.Xna.Framework.Vector2(position.X - outlineWidth, position.Y), outline, 0f, new Microsoft.Xna.Framework.Vector2(0, 0), Vector2.One, 0f, spacing - 1f);
                _spriteBatch.DrawString(spriteFont, text, new Microsoft.Xna.Framework.Vector2(position.X, position.Y - outlineWidth), outline, 0f, new Microsoft.Xna.Framework.Vector2(0, 0), Vector2.One, 0f, spacing - 1f);
                _spriteBatch.DrawString(spriteFont, text, new Microsoft.Xna.Framework.Vector2(position.X + outlineWidth, position.Y), outline, 0f, new Microsoft.Xna.Framework.Vector2(0, 0), Vector2.One, 0f, spacing - 1f);
                _spriteBatch.DrawString(spriteFont, text, new Microsoft.Xna.Framework.Vector2(position.X, position.Y + outlineWidth), outline, 0f, new Microsoft.Xna.Framework.Vector2(0, 0), Vector2.One, 0f, spacing - 1f);
                _spriteBatch.DrawString(spriteFont, text, new Microsoft.Xna.Framework.Vector2(position.X - outlineWidth, position.Y - outlineWidth), outline, 0f, new Microsoft.Xna.Framework.Vector2(0, 0), Vector2.One, 0f, spacing - 1f);
                _spriteBatch.DrawString(spriteFont, text, new Microsoft.Xna.Framework.Vector2(position.X - outlineWidth, position.Y + outlineWidth), outline, 0f, new Microsoft.Xna.Framework.Vector2(0, 0), Vector2.One, 0f, spacing - 1f);
                _spriteBatch.DrawString(spriteFont, text, new Microsoft.Xna.Framework.Vector2(position.X + outlineWidth, position.Y - outlineWidth), outline, 0f, new Microsoft.Xna.Framework.Vector2(0, 0), Vector2.One, 0f, spacing - 1f);
                _spriteBatch.DrawString(spriteFont, text, new Microsoft.Xna.Framework.Vector2(position.X + outlineWidth, position.Y + outlineWidth), outline, 0f, new Microsoft.Xna.Framework.Vector2(0, 0), Vector2.One, 0f, spacing - 1f);
            }

            // draw fill
            {
                var colorMg = ToMgColor(fillColor);
                _spriteBatch.DrawString(spriteFont, text, new Microsoft.Xna.Framework.Vector2(position.X, position.Y), colorMg, 0f, new Microsoft.Xna.Framework.Vector2(0, 0), Vector2.One, 0f, spacing - 1f);
            }
        }

        /// <inheritdoc/>
        public void DrawRectangle(Rectangle rectangle, Color color)
        {
            SetEffect(null);

            var texture = _whiteTexture;
            var colorMg = ToMgColor(color);
            _spriteBatch.Draw(texture,
                new Microsoft.Xna.Framework.Rectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height),
                null,
                colorMg);
        }

        /// <inheritdoc/>
        public void SetScissorRegion(Rectangle region)
        {
            _currScissorRegion = region;
            _currEffectId = null;
            _spriteBatch.End();
            BeginBatch();
        }

        /// <inheritdoc/>
        public Rectangle? GetScissorRegion()
        {
            return _currScissorRegion;
        }

        // current scissor region
        Rectangle? _currScissorRegion = null;

        /// <summary>
        /// Begin a new rendering batch.
        /// </summary>
        void BeginBatch()
        {
            var effect = GetEffect(_currEffectId);
            if (_currScissorRegion != null)
            {
                _device.ScissorRectangle = new Microsoft.Xna.Framework.Rectangle(_currScissorRegion.Value.X, _currScissorRegion.Value.Y, _currScissorRegion.Value.Width, _currScissorRegion.Value.Height);
            }
            var raster = new RasterizerState();
            raster.CullMode = _device.RasterizerState.CullMode;
            raster.DepthBias = _device.RasterizerState.DepthBias;
            raster.FillMode = _device.RasterizerState.FillMode;
            raster.MultiSampleAntiAlias = _device.RasterizerState.MultiSampleAntiAlias;
            raster.SlopeScaleDepthBias = _device.RasterizerState.SlopeScaleDepthBias;
            raster.ScissorTestEnable = _currScissorRegion.HasValue;
            _device.RasterizerState = raster;
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: effect, rasterizerState: raster);
        }

        /// <inheritdoc/>
        public void ClearScissorRegion()
        {
            _currScissorRegion = null;
            _currEffectId = null;
            _spriteBatch.End();
            BeginBatch();
        }

        /// <inheritdoc/>
        public Color GetPixelFromTexture(string textureId, Point sourcePosition)
        {
            var texture = GetTexture(textureId);
            var pixelData = new Microsoft.Xna.Framework.Color[1];
            if (sourcePosition.X < 0) sourcePosition.X = 0;
            if (sourcePosition.Y < 0) sourcePosition.Y = 0;
            if (sourcePosition.X >= texture.Width) sourcePosition.X = texture.Width - 1;
            if (sourcePosition.Y >= texture.Height) sourcePosition.Y = texture.Height - 1;
            texture.GetData(0, new Microsoft.Xna.Framework.Rectangle(sourcePosition.X, sourcePosition.Y, 1, 1), pixelData, 0, 1);
            var pixelColor = pixelData[0];
            return new Color(pixelColor.R, pixelColor.G, pixelColor.B, pixelColor.A);
        }

        /// <inheritdoc/>
        public Point? FindPixelOffsetInTexture(string textureId, Rectangle sourceRect, Color color, bool returnNearestColor)
        {
            var texture = GetTexture(textureId);
            var pixelData = new Microsoft.Xna.Framework.Color[sourceRect.Width * sourceRect.Height];
            texture.GetData(0, new Microsoft.Xna.Framework.Rectangle(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height), pixelData, 0, pixelData.Length);
            Point? ret = null;
            float nearestDistance = 255f * 255f * 255f * 255f;
            for (int x = 0; x < sourceRect.Width; x++)
            {
                for (int y = 0; y < sourceRect.Height; y++)
                {
                    var curr = pixelData[x + y * sourceRect.Width];
                    if (curr.R == color.R && curr.G == color.G && curr.B == color.B && curr.A == color.A)
                    {
                        return new Point(x, y);
                    }
                    else if (returnNearestColor)
                    {
                        float distance = Vector4.Distance(new Vector4(curr.R, curr.G, curr.B, curr.A), new Vector4(color.R, color.G, color.B, color.A));
                        if (distance <  nearestDistance)
                        {
                            nearestDistance = distance;
                            ret = new Point(x, y);
                        }
                    }
                }
            }
            return ret;
        }
    }
}
