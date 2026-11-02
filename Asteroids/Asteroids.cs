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
    private static Texture2D Pixel;

    public static void Initialize(ContentManager contentManager, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        ContentManager = contentManager;
        SpriteBatch = spriteBatch;
        GraphicsDevice = graphicsDevice;

        Pixel = new(GraphicsDevice, 1, 1);
        Pixel.SetData([Color.White]);

        GetMatrix();
    }

    public static void Update(GameTime gameTime) =>
        Time = (float)gameTime.ElapsedGameTime.TotalSeconds;

    public static void GetMatrix()
    {
        float zoom = 1f;

        Vector2 center = new(GameWindow.X / 2, GameWindow.Y / 2);

        GameMatrix = Matrix.CreateTranslation(-center.X, -center.Y, 0) *
                     Matrix.CreateScale(zoom, zoom, 1f) *
                     Matrix.CreateTranslation(center.X, center.Y, 0);
    }

    public static void DrawLine(Vector2 firstPoint, Vector2 secondPoint)
    {
        float distance = Vector2.Distance(firstPoint, secondPoint);
        float angle = (float)Math.Atan2(secondPoint.Y - firstPoint.Y, secondPoint.X - firstPoint.X);

        SpriteBatch.Draw(Pixel, firstPoint, null, Color.White, angle, Vector2.Zero, new Vector2(distance, 0.5f), SpriteEffects.None, 0f);
    }

    public static Vector2 RotatePoint(Vector2 localPoint, float rotation)
    {
        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        return new(localPoint.X * cos - localPoint.Y * sin,
                   localPoint.X * sin + localPoint.Y * cos);
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
        if (Instance is null) return;

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
        Vector2 p1 = new(20, 0);
        Vector2 p2 = new(-15, -12);
        Vector2 p3 = new(-10, 0);
        Vector2 p4 = new(-15, 12);

        Vector2 tf1 = Position + Globals.RotatePoint(p1, Rotation);
        Vector2 tf2 = Position + Globals.RotatePoint(p2, Rotation);
        Vector2 tf3 = Position + Globals.RotatePoint(p3, Rotation);
        Vector2 tf4 = Position + Globals.RotatePoint(p4, Rotation);

        Globals.DrawLine(tf1, tf2);
        Globals.DrawLine(tf2, tf3);
        Globals.DrawLine(tf3, tf4);
        Globals.DrawLine(tf4, tf1);

        foreach (Bullet bullet in Bullets) bullet.Draw();
    }

    private void HandleInput()
    {
        if (InputManager.IsKeyDown(Keys.Right)) Rotation += 0.04f;
        if (InputManager.IsKeyDown(Keys.Left)) Rotation -= 0.04f;
        if (InputManager.IsKeyDown(Keys.Up)) Speed += 50f * Globals.Time;
        if (InputManager.IsKeyDown(Keys.Z)) Fire();
    }

    private void Fire()
    {
        if (_fireRate <= 0.5f) return;

        Bullet bullet = new(Position, Direction, 0.5f);
        Bullets.Add(bullet);

        _fireRate = 0f;
    }
}

public class Bullet
{
    private readonly BulletFlyweight _bulletFlyweight;

    public Vector2 Position { get; private set; }
    public Rectangle Bounds { get; private set; }
    public Vector2 Direction { get; private set; }
    public float Speed { get; private set; }
    public float Scale { get; private set; }
    public bool IsDestroyed { get; private set; }

    public Bullet(Vector2 position, Vector2 direction, float scale)
    {
        _bulletFlyweight = BulletFlyweightFactory.GetTexture();
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
        Globals.SpriteBatch.Draw(_bulletFlyweight.Texture, Position, Bounds, Color.White, 0f, new(Bounds.Width / 2, Bounds.Height / 2), 1f, SpriteEffects.None, 0f);

    public void Destroy() =>
        IsDestroyed = true;

    private void UpdateBounds() =>
        Bounds = new((int)Position.X, (int)Position.Y, 3, 3);
}

public class Asteroid
{
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

    public void Draw()
    {
        Vector2 p1 = new Vector2(0, 0) * Scale;
        Vector2 p2 = new Vector2(15, 0) * Scale;
        Vector2 p3 = new Vector2(25, -5) * Scale;
        Vector2 p4 = new Vector2(25, -15) * Scale;
        Vector2 p5 = new Vector2(20, -25) * Scale;
        Vector2 p6 = new Vector2(25, -35) * Scale;
        Vector2 p7 = new Vector2(20, -50) * Scale;
        Vector2 p8 = new Vector2(15, -45) * Scale;
        Vector2 p9 = new Vector2(10, -40) * Scale;
        Vector2 p10 = new Vector2(-10, -25) * Scale;
        Vector2 p11 = new Vector2(-10, -10) * Scale;
        Vector2 p12 = new Vector2(5, -10) * Scale;

        Vector2 tf1 = Position + Globals.RotatePoint(p1, Rotation);
        Vector2 tf2 = Position + Globals.RotatePoint(p2, Rotation);
        Vector2 tf3 = Position + Globals.RotatePoint(p3, Rotation);
        Vector2 tf4 = Position + Globals.RotatePoint(p4, Rotation);
        Vector2 tf5 = Position + Globals.RotatePoint(p5, Rotation);
        Vector2 tf6 = Position + Globals.RotatePoint(p6, Rotation);
        Vector2 tf7 = Position + Globals.RotatePoint(p7, Rotation);
        Vector2 tf8 = Position + Globals.RotatePoint(p8, Rotation);
        Vector2 tf9 = Position + Globals.RotatePoint(p9, Rotation);
        Vector2 tf10 = Position + Globals.RotatePoint(p10, Rotation);
        Vector2 tf11 = Position + Globals.RotatePoint(p11, Rotation);
        Vector2 tf12 = Position + Globals.RotatePoint(p12, Rotation);
    
        Globals.DrawLine(tf1, tf2);
        Globals.DrawLine(tf2, tf3);
        Globals.DrawLine(tf3, tf4);
        Globals.DrawLine(tf4, tf5);
        Globals.DrawLine(tf5, tf6);
        Globals.DrawLine(tf6, tf7);
        Globals.DrawLine(tf7, tf8);
        Globals.DrawLine(tf8, tf9);
        Globals.DrawLine(tf9, tf10);
        Globals.DrawLine(tf10, tf11);
        Globals.DrawLine(tf11, tf12);
        Globals.DrawLine(tf12, tf1);
    }

    public void Destroy() =>
        IsDestroyed = true;

    private void UpdateBounds()
    {
        int width = (int)(50 * Scale);
        int height = (int)(50 * Scale);

        Bounds = new((int)Position.X - width / 2, (int)Position.Y - height / 2, width, height);
    }
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

        DestroyedAsteroids.AddRange(Asteroids.Where(x => x.IsDestroyed));
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

            float minX = -5;
            float maxX = screenWidth + 5;
            float minY = -5;
            float maxY = screenHeight + 5;

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

// # applying flyweight pattern for store textures
public class BulletFlyweightFactory
{
    private static BulletFlyweight _cache;

    public static BulletFlyweight GetTexture()
    {
        if (_cache is null) _cache = new();
        return _cache;
    }
}

public class BulletFlyweight
{
    public Texture2D Texture { get; private set; }
    
    public BulletFlyweight()
    {
        Texture = new(Globals.GraphicsDevice, 1, 1);
        Texture.SetData([Color.White]);
    }
}