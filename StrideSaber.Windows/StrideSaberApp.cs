using Stride.Engine;

namespace StrideSaber
{
    class StrideSaberApp
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
