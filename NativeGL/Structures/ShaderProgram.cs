using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace NativeGL.Structures
{
    public class GLShaderProgram : IDisposable
    {
        private GLShaderProgram(int handle)
        {
            Handle = handle;
        }

        public int Handle { get; private set; }

        public static GLShaderProgram Compile(string vertShader, string fragShader)
        {
            int program;
            int vertex = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertex, vertShader);
            GL.CompileShader(vertex);
            ErrorCode anyError = GL.GetError();
            if (anyError != ErrorCode.NoError)
            {
                Debug.WriteLine("Error while compiling vertex shader! " + anyError.ToString());
                Debug.WriteLine(GL.GetShaderInfoLog(vertex));
                return new GLShaderProgram(0);
            }

            int fragment = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragment, fragShader);
            GL.CompileShader(fragment);
            anyError = GL.GetError();
            if (anyError != ErrorCode.NoError)
            {
                Debug.WriteLine("Error while compiling fragment shader! " + anyError.ToString());
                Debug.WriteLine(GL.GetShaderInfoLog(fragment));
                return new GLShaderProgram(0);
            }

            program = GL.CreateProgram();
            GL.AttachShader(program, vertex);
            GL.AttachShader(program, fragment);
            GL.LinkProgram(program);
            anyError = GL.GetError();
            if (anyError != ErrorCode.NoError)
            {
                Debug.WriteLine("Error while linking shader program! " + anyError.ToString());
                Debug.WriteLine(GL.GetProgramInfoLog(program));
                return new GLShaderProgram(0);
            }

            GL.DetachShader(program, vertex);
            GL.DetachShader(program, fragment);
            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);

            return new GLShaderProgram(program);
        }

        public void Dispose()
        {
            GL.DeleteProgram(Handle);
        }
    }
}
