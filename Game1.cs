using asteroids_clone.Asteroids;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace asteroids_clone;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private GameManager _gameManager;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = Globals.GameWindow.X;
        _graphics.PreferredBackBufferHeight = Globals.GameWindow.Y;
        _graphics.ApplyChanges();
        
        base.Initialize();
    }

    protected override void LoadContent()
    {
        Globals.GraphicsDevice = GraphicsDevice;
        Globals.SpriteBatch = new SpriteBatch(Globals.GraphicsDevice);
        Globals.ContentManager = Content;
        Globals.GetMatrix();

        _gameManager = new();
    }

    protected override void Update(GameTime gameTime)
    {
        Globals.Update(gameTime);
        _gameManager.Update();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        Globals.GraphicsDevice.Clear(Color.Black);

        Globals.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Globals.GameMatrix);
        _gameManager.Draw();
        Globals.SpriteBatch.End();

        base.Draw(gameTime);
    }
}
