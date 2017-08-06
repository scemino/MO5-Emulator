using System;
using MO5Emulator.Audio;
using AppKit;
using CoreGraphics;
using nMO5;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform.MacOS;

namespace MO5Emulator
{
    public class GameView : MonoMacGameView
    {
        private int id;
        private Screen _screen;
        private Machine _machine;
        private Color[] _colors;
        private Sound _sound;

        public GameView(CGRect frame)
        : base(frame)
        {
        }

        public override NSView HitTest(CGPoint aPoint)
        {
            return this;
        }

        protected override void OnLoad(EventArgs e)
        {
            NSTrackingAreaOptions opts = ((NSTrackingAreaOptions.MouseMoved | NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.InVisibleRect));
            var trackingArea = new NSTrackingArea(new CGRect(0, 0, FittingSize.Width, FittingSize.Height), opts, Self, null);
            AddTrackingArea(trackingArea);

            // Initialize settings, load textures and sounds here
            _colors = new Color[Screen.Width * Screen.Height];

            id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          Screen.Width, Screen.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                          PixelType.UnsignedByte, _colors);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);

            _screen = new Screen();
            _sound = new Sound();
            _machine = new Machine(_screen, _sound);

            base.OnLoad(e);
        }

        public void OpenK7(string k7)
        {
            _machine.SetK7File(k7);
        }

        public void HardReset()
        {
            _machine.ResetHard();
        }

        public void SoftReset()
        {
            _machine.ResetSoft();
        }

        protected override void OnUpdateFrame(OpenTK.FrameEventArgs e)
        {
            _machine.Step();
            _sound.UpdateQueue();

            base.OnUpdateFrame(e);
        }

        protected override void OnResize(EventArgs e)
        {
            // Adjust the GL view to be the same size as the window
            GL.Viewport(0, 0, Size.Width, Size.Height);
            base.OnResize(e);
        }

        public override void KeyDown(NSEvent theEvent)
        {
            var c = theEvent.CharactersIgnoringModifiers[0];
            _machine.Keyboard.KeyPressed(c);
        }

        public override void KeyUp(NSEvent theEvent)
        {
            var c = theEvent.CharactersIgnoringModifiers[0];
            _machine.Keyboard.KeyReleased(c);
        }

        public override void MouseDown(NSEvent theEvent)
        {
            _screen.MouseClick = true;
        }

        public override void MouseUp(NSEvent theEvent)
        {
            _screen.MouseClick = false;
        }

        public override void MouseMoved(NSEvent theEvent)
        {
            var x = Screen.Width * theEvent.LocationInWindow.X / Size.Width;
            var y = Screen.Height - (Screen.Height * theEvent.LocationInWindow.Y / Size.Height);
            _screen.SetMousePosition((int)x, (int)y);
        }

        protected override void OnRenderFrame(OpenTK.FrameEventArgs e)
        {
            _screen.Update(_colors);
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          Screen.Width, Screen.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                          PixelType.UnsignedByte, _colors);

            // Setup buffer
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, 1.0, 1.0, 0.0, 0.0, 4.0);

            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.Begin(BeginMode.Quads);
            GL.TexCoord2(0.0f, 0.0f);
            GL.Vertex2(0.0f, 0.0f);
            GL.TexCoord2(1.0f, 0.0f);
            GL.Vertex2(1.0f, 0.0f);
            GL.TexCoord2(1.0f, 1.0f);
            GL.Vertex2(1.0f, 1.0f);
            GL.TexCoord2(0.0f, 1.0f);
            GL.Vertex2(0.0f, 1.0f);
            GL.End();
            GL.Disable(EnableCap.Texture2D);
        }
    }
}
