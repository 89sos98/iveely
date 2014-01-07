/*==========================================
 *创建人：刘凡平
 *邮  箱：liufanping@iveely.com
 *世界上最快乐的事情，莫过于为理想而奋斗！
 *版  本：0.1.0
 *Iveely=I void everything,except love you!
 *========================================*/

using System;
using Iveely.Framework.Network;

namespace Iveely.CloudComputing.Client
{
    /// <summary>
    /// 执行包
    /// </summary>
    [Serializable]
    public class ExcutePacket : Packet
    {
        /// <summary>
        /// 执行包传送类型
        /// </summary>
        public enum Type
        {
            Code,
            Kill,
            Task,
            FileFragment,
            Download,
            Delete,
            Rename,
            List
        }

        /// <summary>
        /// 类全名
        /// </summary>
        public string ClassName
        {
            get;
            private set;
        }
        /// <summary>
        /// 应用程序名
        /// </summary>
        public string AppName
        {
            get;
            private set;
        }
        /// <summary>
        /// 当前时间戳
        /// </summary>
        public string TimeStamp
        {
            get;
            private set;
        }

        /// <summary>
        /// 信息返回IP
        /// </summary>
        public string ReturnIp
        {
            get;
            private set;
        }

        /// <summary>
        /// 信息返回接收端口
        /// </summary>
        public int Port
        {
            get;
            private set;
        }

        /// <summary>
        /// 执行类型
        /// </summary>
        public Type ExcuteType
        {
            get;
            private set;
        }
        /// <summary>
        /// 初始执行包
        /// </summary>
        /// <param name="codeBytes">数据</param>
        /// <param name="className">类全名</param>
        /// <param name="appName">应用程序名</param>
        /// <param name="timeStamp">当前时间戳</param>
        /// <param name="excuteType">执行类型</param>
        public ExcutePacket(byte[] codeBytes, string className, string appName, string timeStamp, Type excuteType)
        {
            this.Data = codeBytes;
            this.ClassName = className;
            this.AppName = appName;
            this.TimeStamp = timeStamp;
            this.ExcuteType = excuteType;
        }

        public ExcutePacket()
        {}

        /// <summary>
        /// 设置返回接收参数
        /// </summary>
        /// <param name="ip">接收IP</param>
        /// <param name="port">接收端口</param>
        public void SetReturnAddress(string ip, int port)
        {
            this.ReturnIp = ip;
            this.Port = port;
        }
    }
}

