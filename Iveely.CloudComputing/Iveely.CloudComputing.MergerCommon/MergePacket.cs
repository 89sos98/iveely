using System;
using Iveely.Framework.Network;

namespace Iveely.CloudComputing.MergerCommon
{
    /// <summary>
    /// 数据合并包
    /// </summary>
    [Serializable]
    public class MergePacket : Packet
    {
        /// <summary>
        /// 数据合并包枚举类型
        /// </summary>
        public enum MergeType
        {
            Sum,
            Average,
            Distinct,
            CombineTable,
            CombineList,
            CombineSort,
            TopN
        }

        /// <summary>
        /// 应用的名称
        /// </summary>
        public string AppName { get; private set; }

        /// <summary>
        /// 应用的时间戳
        /// </summary>
        public string TimeStamp { get; private set; }

        /// <summary>
        /// 数据合并包类型
        /// </summary>
        public MergeType Type { get; private set; }

        /// <summary>
        /// 初始数据合并包
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="type">包类型</param>
        /// <param name="timeStamp">当前时间</param>
        /// <param name="appName">应用程序名</param>
        public MergePacket(byte[] data, MergeType type, string timeStamp, string appName)
        {
            this.TimeStamp = timeStamp;
            this.AppName = appName;
            this.Data = data;
            this.Type = type;
        }

        /// <summary>
        /// 初始数据合并包
        /// </summary>
        public MergePacket()
        {}
    }
}
