using System;
using AppKit;
using CoreGraphics;
using MO5Emulator.Audio;
using MO5Emulator.Input;
using nMO5;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform.MacOS;

namespace MO5Emulator
{
    public class GameView : MonoMacGameView
    {
        private int id;
        private Color[] _colors;

        private AppDelegate AppDelegate => (AppDelegate)NSApplication.SharedApplication.Delegate;
        public Machine Machine => AppDelegate.Machine;

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
            var opts = ((NSTrackingAreaOptions.MouseMoved | NSTrackingAreaOptions.ActiveInKeyWindow | NSTrackingAreaOptions.InVisibleRect));
            var trackingArea = new NSTrackingArea(new CGRect(0, 0, FittingSize.Width, FittingSize.Height), opts, Self, null);
            AddTrackingArea(trackingArea);

            _colors = new Color[Screen.Width * Screen.Height];

            id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          Screen.Width, Screen.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                          PixelType.UnsignedByte, _colors);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);

            base.OnLoad(e);
        }

        protected override void OnUpdateFrame(OpenTK.FrameEventArgs e)
        {
            if (!Machine.IsScriptRunning)
            {
                Machine.Step();
            }
            (Machine.Sound as Sound)?.UpdateQueue();

            base.OnUpdateFrame(e);
        }

        protected override void OnResize(EventArgs e)
        {
            // Adjust the GL view to be the same size as the window
            GL.Viewport(0, 0, Size.Width, Size.Height);
            base.OnResize(e);
        }

        public override void FlagsChanged(NSEvent theEvent)
        {
            ((CocoaInput)Machine.Input).FlagsChanged(theEvent);
        }

        public override void KeyDown(NSEvent theEvent)
        {
            ((CocoaInput)Machine.Input).KeyDown(theEvent);
        }

        public override void KeyUp(NSEvent theEvent)
        {
            ((CocoaInput)Machine.Input).KeyUp(theEvent);
        }

        public override void MouseDown(NSEvent theEvent)
        {
            ((CocoaInput)Machine.Input).MouseDown();
        }

        public override void MouseUp(NSEvent theEvent)
        {
            ((CocoaInput)Machine.Input).MouseUp();
        }

        public override void MouseMoved(NSEvent theEvent)
        {
            var x = Screen.Width * theEvent.LocationInWindow.X / Bounds.Width;
            var y = Screen.Height - (Screen.Height * theEvent.LocationInWindow.Y / Bounds.Height);
            ((CocoaInput)Machine.Input).MouseMoved((int)x - 8, (int)y - 8);
        }

        protected override void OnRenderFrame(OpenTK.FrameEventArgs e)
        {
            Machine.Screen.Update(_colors);
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          Screen.Width, Screen.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                          PixelType.UnsignedByte, _colors);

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
