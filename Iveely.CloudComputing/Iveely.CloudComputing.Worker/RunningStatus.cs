/*==========================================
 *创建人：刘凡平
 *邮  箱：liufanping@iveely.com
 *电  话：
 *版  本：0.1.0
 *Iveely=I void everything,except love you!
 *========================================*/

using System;
using Iveely.CloudComputing.Client;

namespace Iveely.CloudComputing.Worker
{
    /// <summary>
    /// 运行状态类
    /// </summary>
    [Serializable]
    public class RunningStatus
    {
        /// <summary>
        /// 状态
        /// </summary>
        public string Description;
        /// <summary>
        /// 执行包
        /// </summary>
        public ExcutePacket Packet;

        /// <summary>
        /// 初始运行状态类
        /// </summary>
        /// <param name="packet">执行包</param>
        /// <param name="status">状态</param>
        public RunningStatus(ExcutePacket packet, string status)
        {
            Packet = packet;
            Description = status;
        }
    }
}