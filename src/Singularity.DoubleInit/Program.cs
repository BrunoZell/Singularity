using System;

namespace Singularity.DoubleInit
{
    public interface ISingleton { }

    public class WritingSingleton : ISingleton
    {
        public WritingSingleton() => Console.WriteLine("Initialized");
    }

    class Program
    {
        static void Main(string[] args)
        {
            var container = new Container(builder =>
            {
                // This registration does result in two initializations of WritingSingleton
                //builder.Register<ISingleton, WritingSingleton>(c => c.With(Lifetimes.PerContainer));

                // This registration does result in one initialization of WritingSingleton
                builder.Register(typeof(WritingSingleton), c => c.As(typeof(ISingleton)).With(Lifetimes.PerContainer));
            });

            var instance1 = container.GetInstance<ISingleton>();
            var instance2 = container.GetInstance<ISingleton>();
            var instance3 = container.GetInstance<WritingSingleton>();
        }
    }
}
