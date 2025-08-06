using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

class Program
{
    private static int _vertexBufferObject;
    private static int _vertexArrayObject;
    private static int _shaderProgram;
    private static int _width = 1800;
    private static int _height = 900;
    private static Vector2 _squarePosition = new Vector2(0.0f, 0.0f);
    private static Vector3 _squareColor = new Vector3(1.0f, 0.0f, 0.0f); // Початковий колір (червоний)

    static void Main()
    {
        using var window = new GameWindow(
            GameWindowSettings.Default,
            new NativeWindowSettings { Size = (_width, _height), Title = "Color Changing Square" }
        );

        window.Load += () =>
        {
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            float[] vertices = {
                // Позиція         Колір
                 0.1f,  0.1f, 0.0f, 1.0f, 1.0f, 1.0f, // Білий (множимо на customColor у шейдері)
                 0.1f, -0.1f, 0.0f, 1.0f, 1.0f, 1.0f,
                -0.1f, -0.1f, 0.0f, 1.0f, 1.0f, 1.0f,
                -0.1f,  0.1f, 0.0f, 1.0f, 1.0f, 1.0f
            };

            uint[] indices = { 0, 1, 3, 1, 2, 3 };

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            var elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            var vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec3 aPosition;
                layout (location = 1) in vec3 aColor;
                
                out vec3 ourColor;
                
                uniform mat4 projection;
                uniform mat4 model;
                
                void main()
                {
                    gl_Position = projection * model * vec4(aPosition, 1.0);
                    ourColor = aColor;
                }";

            var fragmentShaderSource = @"
                #version 330 core
                in vec3 ourColor;
                out vec4 FragColor;
                
                uniform vec3 customColor;
                
                void main()
                {
                    FragColor = vec4(ourColor * customColor, 1.0);
                }";

            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, vertexShader);
            GL.AttachShader(_shaderProgram, fragmentShader);
            GL.LinkProgram(_shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
        };

        window.Resize += (args) =>
        {
            GL.Viewport(0, 0, window.Size.X, window.Size.Y);
        };

        window.RenderFrame += (args) =>
        {
            // Зміна кольору за допомогою клавіш 1-3
            if (window.KeyboardState.IsKeyDown(Keys.D1))
                _squareColor = new Vector3(1.0f, 0.0f, 0.0f); // Червоний
            if (window.KeyboardState.IsKeyDown(Keys.D2))
                _squareColor = new Vector3(0.0f, 1.0f, 0.0f); // Зелений
            if (window.KeyboardState.IsKeyDown(Keys.D3))
                _squareColor = new Vector3(0.0f, 0.0f, 1.0f); // Синій

            // Рух квадрата (як у попередній версії)
            float moveSpeed = 0.003f;
            if (window.KeyboardState.IsKeyDown(Keys.D))
                _squarePosition.X += moveSpeed;
            if (window.KeyboardState.IsKeyDown(Keys.A))
                _squarePosition.X -= moveSpeed;
            if (window.KeyboardState.IsKeyDown(Keys.W))
                _squarePosition.Y += moveSpeed;
            if (window.KeyboardState.IsKeyDown(Keys.S))
                _squarePosition.Y -= moveSpeed;

            // Обмеження руху
            var aspectRatio = (float)window.Size.X / window.Size.Y;
            var maxX = aspectRatio - 0.1f;
            var maxY = 1.0f - 0.1f;
            _squarePosition.X = MathHelper.Clamp(_squarePosition.X, -maxX, maxX);
            _squarePosition.Y = MathHelper.Clamp(_squarePosition.Y, -maxY, maxY);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            var projection = Matrix4.CreateOrthographicOffCenter(-aspectRatio, aspectRatio, -1, 1, -1, 1);
            var model = Matrix4.CreateTranslation(_squarePosition.X, _squarePosition.Y, 0.0f);

            GL.UseProgram(_shaderProgram);
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "projection"), false, ref projection);
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref model);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "customColor"), ref _squareColor);

            GL.BindVertexArray(_vertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

            window.SwapBuffers();
        };

        window.Run();
    }
}