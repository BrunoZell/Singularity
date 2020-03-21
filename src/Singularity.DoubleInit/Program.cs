namespace Singularity.DoubleInit
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new Container(builder =>
            {
                builder.Register<ISingleton, WritingSingleton>(c => c.With(Lifetimes.PerContainer));
            });

            var instance1 = container.GetInstance<ISingleton>();
            var instance2 = container.GetInstance<ISingleton>();
            var instance3 = container.GetInstance<WritingSingleton>();
        }
    }
}
