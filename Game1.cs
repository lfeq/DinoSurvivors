using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DinoSurvivors.Core;

namespace DinoSurvivors;

public class Game1 : Game {
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    private Simulation _simulation;
    private Texture2D _pixelTexture;
    private Vector2 _cameraPosition;
    private float _playerDamageFlashTimer = 0f;
    private bool _enableDamageNumbers = true;

    private class FloatingDamageNumber {
        public Vector2 Position;
        public float Damage;
        public float Timer;
        public const float MaxTimer = 0.5f;

        public FloatingDamageNumber(System.Numerics.Vector2 position, float damage) {
            Position = new Vector2(position.X, position.Y);
            Damage = damage;
            Timer = MaxTimer;
        }
    }

    private class DeathPopEffect {
        public Vector2 Position;
        public float Timer;
        public const float MaxTimer = 0.3f;

        public DeathPopEffect(System.Numerics.Vector2 position) {
            Position = new Vector2(position.X, position.Y);
            Timer = MaxTimer;
        }
    }

    private readonly System.Collections.Generic.List<FloatingDamageNumber> _floatingDamageNumbers = new();
    private readonly System.Collections.Generic.List<DeathPopEffect> _deathPopEffects = new();

    public Game1() {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize() {
        var rng = new SystemRng(1337);
        var persistence = new NullPersistence();
        var content = new NullContentProvider();
        _simulation = new Simulation(rng, persistence, content);

        _simulation.OnPlayerDamaged += () => {
            _playerDamageFlashTimer = 0.2f;
        };

        _simulation.OnEnemyHit += (position, damage) => {
            if (_enableDamageNumbers) {
                _floatingDamageNumbers.Add(new FloatingDamageNumber(position, damage));
            }
            PlayHitSound();
        };

        _simulation.OnEnemyKilled += (position) => {
            _deathPopEffects.Add(new DeathPopEffect(position));
        };

        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 768;
        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime) {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        // Map WASD keys to MoveDirection
        var moveDir = Vector2.Zero;
        if (keyboardState.IsKeyDown(Keys.W)) moveDir.Y -= 1;
        if (keyboardState.IsKeyDown(Keys.S)) moveDir.Y += 1;
        if (keyboardState.IsKeyDown(Keys.A)) moveDir.X -= 1;
        if (keyboardState.IsKeyDown(Keys.D)) moveDir.X += 1;

        // Convert to System.Numerics.Vector2
        var simMoveDir = new System.Numerics.Vector2(moveDir.X, moveDir.Y);

        // Map mouse cursor relative to player for AimDirection
        var screenCenter = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
        var mouseScreenPos = new Vector2(mouseState.X, mouseState.Y);
        var mouseWorldPos = mouseScreenPos + _cameraPosition - screenCenter;
        
        var aimDir = mouseWorldPos - new Vector2(_simulation.PlayerPosition.X, _simulation.PlayerPosition.Y);
        if (aimDir != Vector2.Zero) {
            aimDir.Normalize();
        }
        var simAimDir = new System.Numerics.Vector2(aimDir.X, aimDir.Y);

        var fire = mouseState.LeftButton == ButtonState.Pressed;
        var pause = keyboardState.IsKeyDown(Keys.P);
        var confirm = keyboardState.IsKeyDown(Keys.Enter);
        var cancel = keyboardState.IsKeyDown(Keys.Back);

        var actions = new ControlActions(simMoveDir, simAimDir, fire, pause, confirm, cancel);

        _simulation.Step(actions, (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (_playerDamageFlashTimer > 0f) {
            _playerDamageFlashTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        for (int i = _floatingDamageNumbers.Count - 1; i >= 0; i--) {
            _floatingDamageNumbers[i].Timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_floatingDamageNumbers[i].Timer <= 0f) {
                _floatingDamageNumbers.RemoveAt(i);
            }
        }

        for (int i = _deathPopEffects.Count - 1; i >= 0; i--) {
            _deathPopEffects[i].Timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_deathPopEffects[i].Timer <= 0f) {
                _deathPopEffects.RemoveAt(i);
            }
        }

        // Smooth camera follow player
        _cameraPosition = new Vector2(_simulation.PlayerPosition.X, _simulation.PlayerPosition.Y);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        var screenCenter = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
        var transform = Matrix.CreateTranslation(new Vector3(-_cameraPosition + screenCenter, 0));

        _spriteBatch.Begin(transformMatrix: transform);

        // Draw Arena Floor
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, (int)_simulation.ArenaSize.X, (int)_simulation.ArenaSize.Y), new Color(40, 40, 45));

        // Draw grid pattern for visual reference
        int gridSize = 100;
        for (int x = 0; x < _simulation.ArenaSize.X; x += gridSize) {
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, 0, 1, (int)_simulation.ArenaSize.Y), new Color(60, 60, 65));
        }
        for (int y = 0; y < _simulation.ArenaSize.Y; y += gridSize) {
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, y, (int)_simulation.ArenaSize.X, 1), new Color(60, 60, 65));
        }

        // Draw Arena Boundaries (Red walls)
        int wallThickness = 12;
        // Top
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, -wallThickness, (int)_simulation.ArenaSize.X, wallThickness), Color.Crimson);
        // Bottom
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, (int)_simulation.ArenaSize.Y, (int)_simulation.ArenaSize.X, wallThickness), Color.Crimson);
        // Left
        _spriteBatch.Draw(_pixelTexture, new Rectangle(-wallThickness, -wallThickness, wallThickness, (int)_simulation.ArenaSize.Y + wallThickness * 2), Color.Crimson);
        // Right
        _spriteBatch.Draw(_pixelTexture, new Rectangle((int)_simulation.ArenaSize.X, -wallThickness, wallThickness, (int)_simulation.ArenaSize.Y + wallThickness * 2), Color.Crimson);

        // Draw Bug Zapper visual shockwaves
        foreach (var weapon in _simulation.EquippedWeapons) {
            if (weapon.Definition.Id == "BugZapper") {
                var levelData = weapon.CurrentLevelData;
                float timeSinceTrigger = levelData.Cooldown - weapon.CooldownTimer;
                if (timeSinceTrigger >= 0f && timeSinceTrigger < 0.15f) {
                    var zapRadius = levelData.RangeOrRadius;
                    var zapRect = new Rectangle(
                        (int)_simulation.PlayerPosition.X - (int)zapRadius,
                        (int)_simulation.PlayerPosition.Y - (int)zapRadius,
                        (int)zapRadius * 2,
                        (int)zapRadius * 2
                    );
                    // Translucent electric blue square and outline
                    _spriteBatch.Draw(_pixelTexture, zapRect, new Color(0, 191, 255, 30));
                    int thickness = 2;
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(zapRect.X, zapRect.Y, zapRect.Width, thickness), Color.DeepSkyBlue * 0.5f);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(zapRect.X, zapRect.Bottom - thickness, zapRect.Width, thickness), Color.DeepSkyBlue * 0.5f);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(zapRect.X, zapRect.Y, thickness, zapRect.Height), Color.DeepSkyBlue * 0.5f);
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(zapRect.Right - thickness, zapRect.Y, thickness, zapRect.Height), Color.DeepSkyBlue * 0.5f);
                }
            }
        }

        int playerSize = 32;
        var playerRect = new Rectangle(
            (int)_simulation.PlayerPosition.X - playerSize / 2,
            (int)_simulation.PlayerPosition.Y - playerSize / 2,
            playerSize,
            playerSize
        );
        var playerColor = _playerDamageFlashTimer > 0f ? Color.Red : Color.LimeGreen;
        _spriteBatch.Draw(_pixelTexture, playerRect, playerColor);

        // Draw enemies (Compy: small orange rectangle, white if hit flashing)
        int enemySize = 24;
        foreach (var enemy in _simulation.Enemies) {
            var enemyRect = new Rectangle(
                (int)enemy.Position.X - enemySize / 2,
                (int)enemy.Position.Y - enemySize / 2,
                enemySize,
                enemySize
            );
            var enemyColor = enemy.HitFlashTimer > 0f ? Color.White : Color.Orange;
            _spriteBatch.Draw(_pixelTexture, enemyRect, enemyColor);
        }

        // Draw projectiles
        foreach (var proj in _simulation.Projectiles) {
            int projSize = (int)(proj.Radius * 2);
            var projRect = new Rectangle(
                (int)proj.Position.X - projSize / 2,
                (int)proj.Position.Y - projSize / 2,
                projSize,
                projSize
            );
            Color projColor = proj.Radius > 8f ? Color.Crimson : Color.Yellow;
            _spriteBatch.Draw(_pixelTexture, projRect, projColor);
        }

        // Draw XP Gems (small cyan squares)
        int gemSize = 8;
        foreach (var gem in _simulation.XpGems) {
            var gemRect = new Rectangle(
                (int)gem.Position.X - gemSize / 2,
                (int)gem.Position.Y - gemSize / 2,
                gemSize,
                gemSize
            );
            _spriteBatch.Draw(_pixelTexture, gemRect, Color.Cyan);
        }

        // Draw death pops (expanding particles)
        foreach (var effect in _deathPopEffects) {
            float progress = 1f - (effect.Timer / DeathPopEffect.MaxTimer);
            float radius = progress * 30f;
            Color color = Color.White * (1f - progress);
            int numParticles = 8;
            int particleSize = 4;
            for (int i = 0; i < numParticles; i++) {
                double angle = i * Math.PI * 2 / numParticles;
                Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                _spriteBatch.Draw(_pixelTexture, new Rectangle((int)(effect.Position.X + offset.X) - particleSize / 2, (int)(effect.Position.Y + offset.Y) - particleSize / 2, particleSize, particleSize), color);
            }
        }

        // Draw floating damage numbers
        foreach (var fdn in _floatingDamageNumbers) {
            float progress = 1f - (fdn.Timer / FloatingDamageNumber.MaxTimer);
            int yOffset = (int)(progress * 40f);
            int drawX = (int)fdn.Position.X - 6;
            int drawY = (int)fdn.Position.Y - 20 - yOffset;
            Color color = Color.Yellow * (1f - progress);
            DrawPixelNumber((int)fdn.Damage, drawX, drawY, 2, color);
        }

        _spriteBatch.End();

        // Screen-space HUD rendering
        _spriteBatch.Begin();

        // Health Bar (glassmorphic style)
        int hbX = 20;
        int hbY = 20;
        int hbWidth = 300;
        int hbHeight = 20;
        int border = 2;

        // Draw border and background
        _spriteBatch.Draw(_pixelTexture, new Rectangle(hbX - border, hbY - border, hbWidth + border * 2, hbHeight + border * 2), new Color(60, 60, 60));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(hbX, hbY, hbWidth, hbHeight), new Color(30, 30, 30, 150));

        // Draw current HP
        float hpPercent = _simulation.PlayerCurrentHp / _simulation.PlayerMaxHp;
        int currentHpWidth = (int)(hbWidth * hpPercent);
        Color hpColor = hpPercent > 0.3f ? Color.LimeGreen : Color.Red;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(hbX, hbY, currentHpWidth, hbHeight), hpColor);

        // XP Bar (sleek cyan progress bar with Level indicator)
        int xbX = 20;
        int xbY = 48;
        int xbWidth = 300;
        int xbHeight = 8;

        // Draw border and background
        _spriteBatch.Draw(_pixelTexture, new Rectangle(xbX - border, xbY - border, xbWidth + border * 2, xbHeight + border * 2), new Color(60, 60, 60));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(xbX, xbY, xbWidth, xbHeight), new Color(30, 30, 30, 150));

        // Draw current XP
        float xpPercent = _simulation.PlayerXp / _simulation.XpToNextLevel;
        int currentXpWidth = (int)(xbWidth * Math.Clamp(xpPercent, 0f, 1f));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(xbX, xbY, currentXpWidth, xbHeight), Color.Cyan);

        // Draw Player Level
        DrawPixelNumber(_simulation.PlayerLevel, xbX + xbWidth + 12, xbY - 4, 2, Color.Cyan);

        // Draw Weapon Slots in HUD
        int slotStartX = 20;
        int slotStartY = 64;
        int slotSize = 16;
        int slotSpacing = 24;

        for (int i = 0; i < 3; i++) {
            int x = slotStartX + i * slotSpacing * 2;
            int y = slotStartY;

            // Draw slot border
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x - 1, y - 1, slotSize + 2, slotSize + 2), Color.Gray);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, slotSize, slotSize), Color.Black);

            if (i < _simulation.EquippedWeapons.Count) {
                var weapon = _simulation.EquippedWeapons[i];
                Color weaponColor = Color.White;
                if (weapon.Definition.Id == "TranqPistol") weaponColor = Color.Yellow;
                else if (weapon.Definition.Id == "FlareGun") weaponColor = Color.Crimson;
                else if (weapon.Definition.Id == "BugZapper") weaponColor = Color.DeepSkyBlue;

                _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 2, y + 2, slotSize - 4, slotSize - 4), weaponColor);
                // Draw Level number next to slot
                DrawPixelNumber(weapon.Level, x + slotSize + 4, y, 2, Color.White);
            }
        }

        // If game over, show run-lost overlay
        if (_simulation.IsRunLost) {
            // Dark red overlay
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), new Color(40, 0, 0, 180));
            
            // Draw a center game-over banner
            int bannerWidth = 400;
            int bannerHeight = 100;
            int bannerX = (GraphicsDevice.Viewport.Width - bannerWidth) / 2;
            int bannerY = (GraphicsDevice.Viewport.Height - bannerHeight) / 2;
            
            // Draw banner background
            _spriteBatch.Draw(_pixelTexture, new Rectangle(bannerX, bannerY, bannerWidth, bannerHeight), new Color(10, 10, 10, 220));
            // Draw banner border
            _spriteBatch.Draw(_pixelTexture, new Rectangle(bannerX - 2, bannerY - 2, bannerWidth + 4, bannerHeight + 4), Color.Crimson);
            
            // Draw a big red cross
            int crossSize = 40;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(bannerX + 180, bannerY + 30, crossSize, 8), Color.Crimson);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(bannerX + 196, bannerY + 14, 8, crossSize), Color.Crimson);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void PlayHitSound() {
        try {
            int sampleRate = 44100;
            double duration = 0.08;
            int numSamples = (int)(sampleRate * duration);
            byte[] buffer = new byte[numSamples * 2];

            for (int i = 0; i < numSamples; i++) {
                double t = (double)i / sampleRate;
                double freq = 250.0 - (t / duration) * 170.0;
                double wave = Math.Sin(2 * Math.PI * freq * t);
                short sample = (short)(wave * 8000);
                buffer[i * 2] = (byte)(sample & 0xFF);
                buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

            var sound = new Microsoft.Xna.Framework.Audio.DynamicSoundEffectInstance(sampleRate, Microsoft.Xna.Framework.Audio.AudioChannels.Mono);
            sound.SubmitBuffer(buffer);
            sound.Play();
        } catch {
            // Ignore if audio device is unavailable
        }
    }

    private static readonly string[] DigitRepresentations = new string[] {
        "###" +
        "#.#" +
        "#.#" +
        "#.#" +
        "###", // 0

        ".#." +
        "##." +
        ".#." +
        ".#." +
        "###", // 1

        "###" +
        "..#" +
        "###" +
        "#.." +
        "###", // 2

        "###" +
        "..#" +
        "###" +
        "..#" +
        "###", // 3

        "#.#" +
        "#.#" +
        "###" +
        "..#" +
        "..#", // 4

        "###" +
        "#.." +
        "###" +
        "..#" +
        "###", // 5

        "###" +
        "#.." +
        "###" +
        "#.#" +
        "###", // 6

        "###" +
        "..#" +
        ".#." +
        ".#." +
        ".#.", // 7

        "###" +
        "#.#" +
        "###" +
        "#.#" +
        "###", // 8

        "###" +
        "#.#" +
        "###" +
        "..#" +
        "###"  // 9
    };

    private void DrawPixelDigit(int digit, int x, int y, int pixelSize, Color color) {
        if (digit < 0 || digit > 9) return;
        string rep = DigitRepresentations[digit];
        for (int r = 0; r < 5; r++) {
            for (int c = 0; c < 3; c++) {
                if (rep[r * 3 + c] == '#') {
                    _spriteBatch.Draw(_pixelTexture, new Rectangle(x + c * pixelSize, y + r * pixelSize, pixelSize, pixelSize), color);
                }
            }
        }
    }

    private void DrawPixelNumber(int number, int x, int y, int pixelSize, Color color) {
        string str = number.ToString();
        int spacing = 4 * pixelSize;
        for (int i = 0; i < str.Length; i++) {
            int digit = str[i] - '0';
            DrawPixelDigit(digit, x + i * spacing, y, pixelSize, color);
        }
    }
}