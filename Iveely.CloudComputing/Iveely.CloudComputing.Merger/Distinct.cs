﻿/*==========================================
 *创建人：刘凡平
 *邮  箱：liufanping@iveely.com
 *世界上最快乐的事情，莫过于为理想而奋斗！
 *版  本：0.1.0
 *Iveely=I void everything,except love you!
 *========================================*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Iveely.CloudComputing.Merger
{
    public class Distinct : Operate
    {
        private const string OperateType = "distinct";

        private readonly string _flag;

        /// <summary>
        /// 初始运算操作集
        /// </summary>
        /// <param name="appTimeStamp">时间戳</param>
        /// <param name="appName">应用程序名称</param>
        public Distinct(string appTimeStamp, string appName)
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
        public override T Compute<T>(T val)
        {
            lock (Table)
            {
                if (Table[_flag] == null)
                {
                    Table.Add(_flag, val);
                    CountTable.Add(_flag, 1);
                }
                else
                {
                    List<object> objects = (List<object>) Table[_flag];
                    List<object> newObjects = (List<object>) Convert.ChangeType(val, typeof (List<object>));
                    objects.AddRange(newObjects);
                    List<object> distinctObjects = new List<object>(objects.Distinct());
                    Table[_flag] = distinctObjects;

                    int count = int.Parse(CountTable[_flag].ToString());
                    CountTable[_flag] = count + 1;
                }
            }
            if (Waite(_flag))
            {
                T t = (T)Convert.ChangeType(Table[_flag], typeof(T));
                return t;
            }
            throw new TimeoutException();
        }
    }
}
