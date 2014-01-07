namespace Iveely.CloudComputing.Cacher
{
    class Program
    {
        /// <summary>
        /// 分布式缓存。用于分布式存放小数据。采用一致性hash算法。
        /// </summary>
        static void Main()
        {
            Executor executor = new Executor();
            executor.Start();
        }
    }
}
