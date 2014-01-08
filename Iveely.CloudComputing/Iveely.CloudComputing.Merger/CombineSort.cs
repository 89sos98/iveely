/*==========================================
 *创建人：刘凡平
 *邮  箱：liufanping@iveely.com
 *世界上最快乐的事情，莫过于为理想而奋斗！
 *版  本：0.1.0
 *Iveely=I void everything,except love you!
 *========================================*/

using System;
using Iveely.Framework.Algorithm;

namespace Iveely.CloudComputing.Merger
{
    /// <summary>
    /// 排序
    /// </summary>
    public class CombineSort : Operate
    {
        private const string OperateType = "combine_sort";

        private readonly string _flag;

        /// <summary>
        /// 初始运算操作集
        /// </summary>
        /// <param name="appTimeStamp">时间戳</param>
        /// <param name="appName">应用程序名称</param>
        public CombineSort(string appTimeStamp, string appName)
            : base(appTimeStamp, appName)
        {
            this.AppName = appName;
            this.AppTimeStamp = appTimeStamp;
            _flag = OperateType + "_" + appTimeStamp + "_" + appName;
        }

        /// <summary>
        /// 计算
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <returns></returns>
        //public override T Compute<T>(T val)
        //{
        //    return default(T);
        //}

        public T[] ArrayCompute<T>(T[] val)
        {
            lock (Table)
            {
                if (Table[_flag] == null)
                {
                    Table.Add(_flag, ChangeType(val));
                    CountTable.Add(_flag, 1);
                }
                else
                {
                    double[] list = (double[])Table[_flag];
                    CombineSort<double> combine = new CombineSort<double>();
                    list = combine.GetResult(list, ChangeType(val));
                    Table[_flag] = list;
                    int count = int.Parse(CountTable[_flag].ToString());
                    CountTable[_flag] = count + 1;
                }
            }
            if (Waite(_flag))
            {
                return Array.ConvertAll((double[])Table[_flag],
                    n => (T) Convert.ChangeType(n, typeof (T)));
            }
            throw new TimeoutException();
        }

        public override T Compute<T>(T val)
        {
            throw new NotImplementedException();
        }

        private double[] ChangeType<T>(T[] val)
        {
            return Array.ConvertAll(val, n => double.Parse(n.ToString()));
        }
    }



}
