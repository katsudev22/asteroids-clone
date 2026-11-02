using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace asteroids_clone.Asteroids;

public static class Globals
{
    public static float Time { get; set; }
    public static ContentManager ContentManager { get; set; }
    public static SpriteBatch SpriteBatch { get; set; }
    public static GraphicsDevice GraphicsDevice { get; set; }
    public static Point GameWindow { get; set; } = new(1024, 768);
    public static Matrix GameMatrix { get; set; }

    public static void Update(GameTime gameTime)
    {
        Time = (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public static void GetMatrix()
    {
        float zoom = 4f;

        Vector2 center = new(GameWindow.X / 2, GameWindow.Y / 2);

        GameMatrix = Matrix.CreateTranslation(-center.X, -center.Y, 0) *
                     Matrix.CreateScale(zoom, zoom, 1f) *
                     Matrix.CreateTranslation(center.X, center.Y , 0);
    }

    public static void DrawLine(Vector2 firstPoint, Vector2 secondPoint)
    {
        Texture2D pixel = new(GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);

        float distance = Vector2.Distance(firstPoint, secondPoint);
        float angle = (float)Math.Atan2(secondPoint.Y - firstPoint.Y, secondPoint.X - firstPoint.X);

        SpriteBatch.Draw(pixel, firstPoint, null, Color.White, angle, Vector2.Zero, new Vector2(distance, 1f), SpriteEffects.None, 0f);
    }
}

public class InputManager
{
    public static InputManager Instance { get; private set; }
    
    private KeyboardState _currentKeyboardState;
    private KeyboardState _previousKeyboardState;

    private InputManager() { }

    public static void Initialize()
    {
        if (Instance is null) Instance = new();
    }

    public static void Update()
    {
        if (Instance is null ) return;

        Instance._previousKeyboardState = Instance._currentKeyboardState;
        Instance._currentKeyboardState = Keyboard.GetState();
    }

    public static bool IsKeyDown(Keys key)
    {
        if (Instance is null) return false;
        return Instance._currentKeyboardState.IsKeyDown(key);
    }

    public static bool IsKeyPressed(Keys key)
    {
        if (Instance is null) return false;
        return Instance._currentKeyboardState.IsKeyDown(key) &&
               Instance._previousKeyboardState.IsKeyUp(key);
    }
}

public class GameManager
{
    private Player _player;
    private AsteroidGenerator _asteroidGenerator;

    public GameManager()
    {
        InputManager.Initialize();
        _player = new(new(Globals.GameWindow.X / 2, Globals.GameWindow.Y / 2));
        _asteroidGenerator = new();
    }

    public void Update()
    {
        InputManager.Update();
        _player.Update();
        _asteroidGenerator.Update();
        CheckCollisions();
    }

    public void Draw()
    {
        _player.Draw();
        _asteroidGenerator.Draw();
    }

    private void CheckCollisions()
    {
        foreach (Bullet bullet in _player.Bullets)
            foreach (Asteroid asteroid in _asteroidGenerator.Asteroids)
                if (bullet.Bounds.Intersects(asteroid.Bounds))
                {
                    bullet.Destroy();
                    asteroid.Destroy();
                }
    }
}

public class Player
{
    public Vector2 Position { get; private set; }
    public Rectangle Bounds { get; private set; }
    public float Rotation { get; private set; }
    public float Speed { get; private set; }
    public Vector2 Direction => new(MathF.Cos(Rotation), MathF.Sin(Rotation));
    public List<Bullet> Bullets { get; private set; }

    private float _maxSpeed = 100f;
    private float _fireRate = 0f;

    public Player(Vector2 position)
    {
        Position = position;
        Bounds = new((int)Position.X, (int)Position.Y, 50, 50);
        Rotation = 0f;
        Speed = 0f;
        Bullets = new();
    }

    public void Update()
    {
        HandleInput();

        if (Speed > 0) Speed -= 25f * Globals.Time;
        if (Speed > _maxSpeed) Speed = _maxSpeed;

        Position += Direction * Speed * Globals.Time;

        _fireRate += Globals.Time;

        foreach (Bullet bullet in Bullets) bullet.Update();
        Bullets.RemoveAll(x => x.IsDestroyed);
    }

    public void Draw()
    {
        Vector2 centerPoint = Position;

        Vector2 firstPoint = new(centerPoint.X + 30, centerPoint.Y);
        Vector2 secondPoint = new(centerPoint.X, centerPoint.Y - 10);
        Vector2 thirdPoint = new(centerPoint.X, centerPoint.Y + 10);

        Globals.DrawLine(firstPoint, secondPoint);
        Globals.DrawLine(firstPoint, thirdPoint);

        foreach (Bullet bullet in Bullets) bullet.Draw();
    }
        
    private void HandleInput()
    {
        if (InputManager.IsKeyDown(Keys.Right)) Rotation += 0.04f;
        if (InputManager.IsKeyDown(Keys.Left)) Rotation -= 0.04f;
        if (InputManager.IsKeyDown(Keys.Up)) Speed += 50f * Globals.Time;
        if (InputManager.IsKeyPressed(Keys.Z)) Fire();
    }
    
    private void Fire()
    {
        if (_fireRate < 0.5f) return;

        Bullet bullet = new(Position, Direction, 0.5f);
        Bullets.Add(bullet);

        _fireRate = 0f;
    }
}

public class Bullet
{
    public Texture2D Texture { get; private set; }
    public Vector2 Position { get; private set; }
    public Rectangle Bounds { get; private set; }
    public Vector2 Direction { get; private set; }
    public float Speed { get; private set; }
    public float Scale { get; private set; }
    public bool IsDestroyed { get; private set; }

    public Bullet(Vector2 position, Vector2 direction, float scale)
    {
        Texture = Globals.ContentManager.Load<Texture2D>("bullet");
        Position = position;
        Direction = direction;
        Speed = 100f;
        Scale = scale;
        IsDestroyed = false;
    }

    public void Update()
    {
        Position += Direction * Speed * Globals.Time;
        UpdateBounds();
    }

    public void Draw() =>
        Globals.SpriteBatch.Draw(Texture, Position, null, Color.White, 0f, new(Texture.Width / 2, Texture.Height / 2), Scale, SpriteEffects.None, 0f);

    private void UpdateBounds() =>
        Bounds = new((int)Position.X, (int)Position.Y, Texture.Width / 2, Texture.Height / 2);

    public void Destroy() =>
        IsDestroyed = true;
}

public class Asteroid
{
    public Texture2D Texture { get; private set; }
    public Vector2 Position { get; private set; }
    public Rectangle Bounds { get; private set; }
    public Vector2 Direction { get; private set; }
    public float Speed { get; private set; }
    public float Scale { get; private set; }
    public float Rotation { get; private set; }
    public bool IsDestroyed { get; private set; }
    public int Level { get; private set; }

    private Random _random = new();

    public Asteroid(Vector2 position, Vector2 direction, float speed, float scale, int level)
    {
        Texture = _random.Next(0, 1) == 1 ? Globals.ContentManager.Load<Texture2D>("asteroid-one") : Globals.ContentManager.Load<Texture2D>("asteroid-two");
        Position = position;
        Direction = direction;
        Speed = speed;
        Scale = scale;
        Rotation = 0f;
        IsDestroyed = false;
        Level = level;
    }

    public void Update()
    {
        Rotation += 0.005f;
        Position += Direction * Speed * Globals.Time;
        UpdateBounds();
    }

    public void Draw() =>
        Globals.SpriteBatch.Draw(Texture, Position, null, Color.White, Rotation, new(Texture.Width / 2, Texture.Height / 2), Scale, SpriteEffects.None, 0f);

    private void UpdateBounds() =>
        Bounds = new((int)Position.X, (int)Position.Y, Texture.Width / 2 * (int)Scale, Texture.Height / 2 * (int)Scale);

    public void Destroy() =>
        IsDestroyed = true;
}

public class AsteroidGenerator
{
    public List<Asteroid> Asteroids { get; private set; }
    public List<Asteroid> DestroyedAsteroids { get; private set; }

    private Random _random = new();
    private float _spawnRate = 0f;

    public AsteroidGenerator()
    {
        Asteroids = new();
        DestroyedAsteroids = new();
    }

    public void Update()
    {
        _spawnRate += Globals.Time;
        SpawnAsteroid();

        foreach (Asteroid asteroid in Asteroids)
            asteroid.Update();

        DestroyedAsteroids.AddRange( Asteroids.Where(x => x.IsDestroyed) );
        Asteroids.RemoveAll(x => x.IsDestroyed);

        SpawnAsteroidFragments();
    }

    public void Draw()
    {
        foreach (Asteroid asteroid in Asteroids)
            asteroid.Draw();
    }

    private void SpawnAsteroid()
    {
        if (_spawnRate >= 2f)
        {
            Vector2 position = Vector2.Zero;
            Vector2 target = Vector2.Zero;

            int side = _random.Next(0, 4);

            int screenWidth = Globals.GameWindow.X;
            int screenHeight = Globals.GameWindow.Y;

            float zoom = 4f;
            float visibleWidth = screenWidth / zoom;
            float visibleHeight = screenHeight / zoom;

            float centerX = screenWidth / 2f;
            float centerY = screenHeight / 2f;

            float minX = centerX - (visibleWidth / 2);
            float maxX = centerX + (visibleWidth / 2);
            float minY = centerY - (visibleHeight / 2);
            float maxY = centerY + (visibleHeight / 2);

            switch (side)
            {
                case 0: // Up
                    position = new(_random.Next((int)minX, (int)maxX), minY - 10);
                    target = new(_random.Next((int)minX, (int)maxX), maxY + 10);
                    break;
                case 1: // Down
                    position = new(_random.Next((int)minX, (int)maxX), maxY + 10);
                    target = new(_random.Next((int)minX, (int)maxX), minY - 10);
                    break;
                case 2: // Left
                    position = new(minX - 10, _random.Next((int)minY, (int)maxY));
                    target = new(maxX + 10, _random.Next((int)minY, (int)maxY));
                    break;
                case 3: // Right
                    position = new(maxX + 10, _random.Next((int)minY, (int)maxY));
                    target = new(minX - 10, _random.Next((int)minY, (int)maxY));
                    break;
            }

            Vector2 direction = target - position;
            direction.Normalize();

            float speed = (float)_random.NextDouble() * 3f + 30f;
            float scale = (float)_random.NextDouble() * 2f + 1f;
            int level = (int)scale;

            Asteroid asteroid = new(position, direction, speed, scale, level);
            Asteroids.Add(asteroid);
            _spawnRate = 0f;
        }
    }

    private void SpawnAsteroidFragments()
    {
        for (int i = 0; i < DestroyedAsteroids.Count; i++)
        {
            if (DestroyedAsteroids[i].Level > 1)
            {
                Asteroid asteroid = DestroyedAsteroids[i];
                Vector2 position = asteroid.Position;
                Vector2 direction = asteroid.Direction;
                
                Asteroid firstAsteroidFragment = new(position, direction, asteroid.Speed / 2, 0.9f, 0);
                Asteroids.Add(firstAsteroidFragment);

                Asteroid secondAsteroidFragment = new(position, -direction, asteroid.Speed / 2, 0.9f, 0);
                Asteroids.Add(secondAsteroidFragment);
            }
        }

        DestroyedAsteroids.Clear();
    }
}
