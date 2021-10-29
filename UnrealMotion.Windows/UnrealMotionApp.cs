using Stride.Engine;

namespace UnrealMotion
{
    class UnrealMotionApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
