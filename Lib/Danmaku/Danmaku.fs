namespace Danmaku

open System

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Content

open MonoGame.Extended
open MonoGame.Extended.Input.InputListeners
open MonoGame.Extended.Entities
open MonoGame.Extended.Sprites
open MonoGame.Extended.ViewportAdapters
open MonoGame.Extended.Screens
open MonoGame.Extended.Screens.Transitions
open MonoGame.Extended.Tweening

// open DanmakuPlayer
open RenderSystem
open GameScreenWithComponents
open Asteroids
open Boids
open Bullets

type DanmakuGame(game: Game) =
    inherit GameScreenWithComponents(game)

    [<DefaultValue>]
    val mutable dot: Texture2D
    
    [<DefaultValue>]
    val mutable camera: OrthographicCamera

    [<DefaultValue>]
    val mutable asteroids1: AsteroidShowerSystem
    [<DefaultValue>]
    val mutable asteroidsRenderSystem: AsteroidRenderSystem

    [<DefaultValue>]
    val mutable bullets1: BulletSystem

    [<DefaultValue>]
    val mutable boids1: BoidsSystem

    let mutable spriteBatch: SpriteBatch = Unchecked.defaultof<SpriteBatch>

    let playerSpeed = 300f;
    let player = Player(playerSpeed,Vector2(300f, 600f))
    let playerBoundaries = RectangleF(Point2(200f,100f), Size2(400f, 600f))

    let box = RectangleF(600f, 200f, 50f,80f)
    let bubble = EllipseF(Vector2(600f, 400f), 50f,80f) 
    let bulletTarget = CircleF(Vector2(400f, 200f), 1f)

    let boidsTarget = CircleF(Vector2(1300f, 600f), 0.2f)

    let mutable asteroidAngle = 0f

    let tweener = new Tweener()

    let mutable mouseListener = MouseListener()
    let mutable touchListener = TouchListener()
    let mutable kbdListener = KeyboardListener()

    
    override this.Initialize() =
        
        
        let viewportAdapter =
            // new BoxingViewportAdapter(this.Window, this.GraphicsDevice, 1280, 720)
            new BoxingViewportAdapter(this.Window, this.GraphicsDevice, 1920, 1080)

        this.camera <- OrthographicCamera(viewportAdapter)


        let easingFn = EasingFunctions.QuadraticIn

        let tween = 
            tweener.TweenTo(bubble, (fun bubble -> bubble.RadiusX), 100f, 1f, 1f)
                .RepeatForever(0.5f)
                .AutoReverse()
                .Easing(easingFn)
        
        // TODO: tween move bullet target left and right to test homing
        // let tween2 = 
        //     tweener.TweenTo(bulletTarget, (fun bulletTarget -> bulletTarget.Center.X), 100f, 1f, 1f)
        //         .RepeatForever(0.5f)
        //         .AutoReverse()
        //         .Easing(easingFn)

        let listenerComponent =
            new InputListenerComponent(this.Game, mouseListener, touchListener, kbdListener)

        this.Components.Add listenerComponent

        kbdListener.KeyPressed.Add(fun args  ->

            // FIXME: something is still wrong with the movement, not fully responsive
            match args.Key with 
            | Keys.Space ->
                this.asteroidsRenderSystem.AlwaysShow <- not this.asteroidsRenderSystem.AlwaysShow
            | Keys.Z ->
                this.bullets1.Firing <- true
            | Keys.W | Keys.I ->
                // player.SetVelocity(Vector2.UnitY * -playerSpeed)
                player.CurrentVelocity <- Vector2.UnitY * -playerSpeed + Vector2.UnitX* player.CurrentVelocity.X
            | Keys.A | Keys.J ->
                player.CurrentVelocity <- Vector2.UnitX * -playerSpeed + Vector2.UnitY* player.CurrentVelocity.Y
                // player.SetVelocity(Vector2.UnitX * -playerSpeed)
            | Keys.S | Keys.K ->
                player.CurrentVelocity <- Vector2.UnitY * playerSpeed + Vector2.UnitX* player.CurrentVelocity.X
                // player.SetVelocity(Vector2.UnitY * playerSpeed)
            | Keys.D | Keys.L ->
                player.CurrentVelocity <- Vector2.UnitX * playerSpeed + Vector2.UnitY* player.CurrentVelocity.Y
                // player.SetVelocity(Vector2.UnitX * playerSpeed)
            | _ -> ()

            ())

        kbdListener.KeyReleased.Add(fun args ->
            // if args.Key = Keys.Z then
            //     this.bullets1.Firing <- false
            //     ()

            match args.Key with 
            // | Keys.Space ->
                // this.asteroidsRenderSystem.AlwaysShow <- not this.asteroidsRenderSystem.AlwaysShow
            | Keys.Z ->
                this.bullets1.Firing <- false
            | Keys.W | Keys.I ->
                if( player.CurrentVelocity.Y < 0f) then
                    player.CurrentVelocity <- Vector2.UnitX * player.CurrentVelocity.X
            | Keys.A | Keys.J ->
                if( player.CurrentVelocity.X < 0f) then
                    player.CurrentVelocity <- Vector2.UnitY * player.CurrentVelocity.Y
            | Keys.S | Keys.K ->
                if( player.CurrentVelocity.Y > 0f) then
                    player.CurrentVelocity <- Vector2.UnitX * player.CurrentVelocity.X
            | Keys.D | Keys.L ->
                if( player.CurrentVelocity.X > 0f) then
                    player.CurrentVelocity <- Vector2.UnitY * player.CurrentVelocity.Y
            | _ -> ()
            ())

        base.Initialize()
        ()

    override this.LoadContent() =
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)

        this.dot <- this.Content.Load "1px"


        this.asteroids1 <- new AsteroidShowerSystem(EllipseF(bubble.Center, 300f, 200f))
        this.asteroids1.Bubble <- bubble

        this.asteroidsRenderSystem <- new AsteroidRenderSystem(this.GraphicsDevice, this.camera)

        this.boids1 <- new BoidsSystem(EllipseF(boidsTarget.Center, 300f, 450f))
        this.boids1.Target <- boidsTarget

        this.bullets1 <- new BulletSystem(player.Transform,playerBoundaries)

        // TODO
        this.bullets1.Target <- bulletTarget//CircleF(Vector2(300f, 200f), 1f)

        let world =
            WorldBuilder()
                
                .AddSystem(new SpriteRenderSystem(this.GraphicsDevice, this.camera))

                .AddSystem(this.asteroids1)
                .AddSystem(new AsteroidExpirySystem())
                .AddSystem(this.asteroidsRenderSystem)

                .AddSystem(this.boids1)
                .AddSystem(new BoidsRenderSystem(this.GraphicsDevice, this.camera))

                .AddSystem(this.bullets1)
                .AddSystem(new BulletRenderSystem(this.GraphicsDevice, this.camera))

                .Build()

        this.Components.Add(world)

        // let testEntity = world.CreateEntity()
        // testEntity.Attach(Transform2(100f, 300f, 0f, 100f, 100f))
        // let mutable dotSprite = Sprite(this.dot)
        // dotSprite.Color <- Color.Goldenrod
        // testEntity.Attach(dotSprite)

        base.LoadContent()
        ()

    override this.Update(gameTime) =
        let dt = gameTime.GetElapsedSeconds()

        // TODO: tweener component or entity?
        tweener.Update dt

        asteroidAngle <- (asteroidAngle + dt * 0.15f) % MathF.Tau
        this.asteroids1.SpawnAngle <- asteroidAngle
        this.boids1.SpawnAngle <- MathF.Tau - asteroidAngle

        player.Update gameTime
        player.ConstrainToFrame playerBoundaries

        base.Update gameTime
        ()

    override this.Draw(gameTime) =
        this.GraphicsDevice.Clear Color.PaleVioletRed

        spriteBatch.Begin(transformMatrix = this.camera.GetViewMatrix())

        spriteBatch.DrawEllipse(bubble.Center, Vector2(bubble.RadiusX, bubble.RadiusY), 32, Color.Azure)

        let pointOnBoundary = this.asteroids1.PointOnBoundary
        
        spriteBatch.DrawCircle(pointOnBoundary, 5f, 12, Color.Black)
        let rect = this.asteroids1.SpawnRange()
        let topleft =(Vector2(rect.TopLeft.X, rect.TopLeft.Y)).Rotate(this.asteroids1.SpawnAngle) + pointOnBoundary
        let topright=(Vector2(rect.TopRight.X, rect.TopRight.Y)).Rotate(this.asteroids1.SpawnAngle)  + pointOnBoundary
        let bottomleft =(Vector2(rect.BottomLeft.X, rect.BottomLeft.Y) ).Rotate(this.asteroids1.SpawnAngle) + pointOnBoundary
        let bottomright =(Vector2(rect.BottomRight.X, rect.BottomRight.Y)).Rotate(this.asteroids1.SpawnAngle) + pointOnBoundary

        spriteBatch.DrawLine(topleft, topright, Color.Brown)
        spriteBatch.DrawLine(topleft, bottomleft, Color.Brown)
        spriteBatch.DrawLine(bottomright, topright, Color.Brown)
        spriteBatch.DrawLine(bottomright, bottomleft, Color.Brown)


        spriteBatch.DrawCircle(boidsTarget, 12, Color.Chartreuse)

        player.Draw spriteBatch gameTime

        spriteBatch.End()

        base.Draw gameTime
        ()

