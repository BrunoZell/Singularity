using System;

namespace Singularity.DoubleInit
{
    public class WritingSingleton : ISingleton
    {
        public WritingSingleton()
        {
            Console.WriteLine("Initialized");
        }
    }
}
