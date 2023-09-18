using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using QuickFont;
using System.Drawing;
using System.Diagnostics;

namespace NativeGL
{
    public class GLButton
    {
        private float _x;
        private float _y;
        private float _w;
        private float _h;
        private string _text;
        private string _id;
        private StaticResources _resources;
        private QFontDrawing _drawing;
        private QFont _font;
        private Matrix4 _projectionMatrix;
        private bool _mouseOver;
        private bool _enabled;

        public GLButton(StaticResources resources, float x, float y, float w, float h, string text, string id)
        {
            _x = x;
            _y = 1080 - y;
            _w = w;
            _h = h;
            _text = text;
            _resources = resources;
            _id = id;
            _font = _resources.Fonts["playername"];
            _mouseOver = false;
            _enabled = true;

            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, 1920, 0.0f, 1080, -1.0f, 1.0f);
            _drawing = new QFontDrawing();
            _drawing.ProjectionMatrix = _projectionMatrix;
            RedrawFont();
        }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                RedrawFont();
            }
        }

        private void RedrawFont()
        {
            _drawing.DrawingPrimitives.Clear();
            Color fontColor = Enabled ? Color.White : Color.Gray;
            _drawing.Print(_font, _text, new Vector3(_x + (_w / 2), _y - ((_h - _font.MaxLineHeight) / 2), 0), QFontAlignment.Centre, fontColor);
        }

        public void MouseDown(VirtualMouseClick args)
        {
            if (Enabled && IsWithinButton(args.Position))
            {
                OnClicked(args);
            }
        }

        public void MouseMoved(VirtualMousePosition args)
        {
            if (Enabled)
            {
                _mouseOver = IsWithinButton(args);
            }
            else
            {
                _mouseOver = false;
            }
        }

        private bool IsWithinButton(VirtualMousePosition pos)
        {
            return (pos.VirtualMouseX > _x && pos.VirtualMouseX < _x + _w && (pos.VirtualHeight - pos.VirtualMouseY) < _y && (pos.VirtualHeight - pos.VirtualMouseY) > _y - _h);
        }

        public void Render()
        {
            Matrix4 modelViewMatrix = Matrix4.Identity;

            int program = _resources.Shaders["solidcolor"].Handle;
            GL.UseProgram(program);
            GL.UniformMatrix4(GL.GetUniformLocation(program, "projectionMatrix"), false, ref _projectionMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(program, "modelViewMatrix"), false, ref modelViewMatrix);

            GL.Disable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            if (_mouseOver)
            {
                GL.Color4(0.5f, 0.5f, 0.5f, 0.7f);
            }
            else
            {
                GL.Color4(0.0f, 0.0f, 0.0f, 0.7f);
            }
            GL.Begin(PrimitiveType.Quads);
            {
                GL.Vertex2(_x, _y);
                GL.Vertex2(_x + _w, _y);
                GL.Vertex2(_x + _w, _y - _h);
                GL.Vertex2(_x, _y - _h);
            }
            GL.End();

            if (Enabled)
            {
                GL.Color4(1.0f, 1.0f, 1.0f, 0.6f);
            }
            else
            {
                GL.Color4(0.0f, 0.0f, 0.0f, 0.6f);
            }
            GL.LineWidth(5f);
            GL.Begin(PrimitiveType.LineLoop);
            {
                GL.Vertex2(_x, _y);
                GL.Vertex2(_x + _w, _y);
                GL.Vertex2(_x + _w, _y - _h);
                GL.Vertex2(_x, _y - _h);
            }
            GL.End();

            if (!string.IsNullOrEmpty(_text))
            {
                _drawing.RefreshBuffers();
                _drawing.Draw();
            }
        }

        public event EventHandler<ButtonPressedEventArgs> Clicked;

        private void OnClicked(VirtualMouseClick args)
        {
            EventHandler<ButtonPressedEventArgs> handler = Clicked;

            if (handler != null)
            {
                handler(this, new ButtonPressedEventArgs(_id, args.Button));
            }
        }
    }
}
