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
                // Both registrations result in two initializations of WritingSingleton
                builder.Register<ISingleton, WritingSingleton>(c => c.As<WritingSingleton>().With(Lifetimes.PerContainer));
                //builder.Register(typeof(WritingSingleton), c => c.As(typeof(ISingleton)).With(Lifetimes.PerContainer));
            });

            var childContainer = container.GetNestedContainer();

            var instance1 = childContainer.GetInstance<ISingleton>();
            var instance2 = childContainer.GetInstance<ISingleton>();
            var instance3 = childContainer.GetInstance<WritingSingleton>();
        }
    }
}
