namespace Singularity.DoubleInit
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new Container(builder =>
            {
                builder.Register<ISingleton, WritingSingleton>();
            });

            var instance = container.GetInstance<ISingleton>();
        }
    }
}
