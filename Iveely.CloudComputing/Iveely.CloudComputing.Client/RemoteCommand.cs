﻿/*==========================================
 *创建人：刘凡平
 *邮  箱：liufanping@iveely.com
 *世界上最快乐的事情，莫过于为理想而奋斗！
 *版  本：0.1.0
 *Iveely=I void everything,except love you!
 *========================================*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Iveely.CloudComputing.StateAPI;
using Iveely.Framework.Log;
using Iveely.Framework.Network;
using Iveely.Framework.Network.Synchronous;
using Iveely.Framework.Text;

namespace Iveely.CloudComputing.Client
{
    /// <summary>
    /// 远程命令
    /// </summary>
    public abstract class RemoteCommand
    {
        public abstract void ProcessCmd(string[] args);

        public static void UnknowCommand()
        {
            Console.WriteLine("        未知命令,用法如下:");
            Console.WriteLine("            submit [filepath] [namespace.classname] [appname] [Optional:true] 提交程序");
            Console.WriteLine("            split [filepath] [remotepath] 切分文件");
            Console.WriteLine("            split [filepath] [remotepath] splitstring key1 key2 key3... 切分文件");
            Console.WriteLine("            download [remotepath] [filepath] 下载");
            Console.WriteLine("            delete [remotepath] 删除");
            Console.WriteLine("            rename [filepath] [newfileName] 重命名");
            Console.WriteLine("            list [/folder] 显示文件");
            Console.WriteLine("            task 显示任务");
            Console.WriteLine("            kill [app name] 停止任务命令");
            Console.WriteLine("            exit 退出命令");
        }
    }

    /// <summary>
    /// 提交命令
    /// </summary>
    public class SubmitCmd : RemoteCommand
    {
        private static Server _server;

        private static bool _showMsgFromRemote;

        public override void ProcessCmd(string[] args)
        {
            if (args.Length == 5)
            {
                _showMsgFromRemote = args[4] == "true";
            }

            if (args.Length != 4 && !_showMsgFromRemote)
            {
                UnknowCommand();
                return;
            }
            //1.1 编译应用程序
            Logger.Info("开始编译您的代码...");
            string appName = args[3];
            string className = args[2];
            string filePath = args[1];
            string timeStamp = DateTime.Now.ToFileTimeUtc().ToString(CultureInfo.InvariantCulture);
            string compileResult = CompileCode(filePath);
            if (compileResult != string.Empty)
            {
                Logger.Error(compileResult);
                return;
            }

            //1.2 读取编译后的文件
            Logger.Info("准备发送您的应用程序到平台...");
            string sourceCode = File.ReadAllText(filePath);
            byte[] bytes = Encoding.UTF8.GetBytes(sourceCode);

            //1.3 上传程序至各个节点
            Thread thread = new Thread(StartListen);
            thread.Start();

            StateHelper.Put("ISE://history/" + timeStamp + "/" + appName, Dns.GetHostName());

            IEnumerable<string> ipPathes = StateHelper.GetChildren("ISE://system/state/worker");
            var ipPaths = ipPathes as string[] ?? ipPathes.ToArray();
            foreach (var ipPath in ipPaths)
            {
                Logger.Info("当前Worker Ip Path:" + ipPath);
                string[] ip = ipPath.Substring(ipPath.LastIndexOf('/') + 1, ipPath.Length - ipPath.LastIndexOf('/') - 1).Split(',');
                Framework.Network.Synchronous.Client transfer = new Framework.Network.Synchronous.Client(ip[0], int.Parse(ip[1]));
                ExcutePacket codePacket = new ExcutePacket(bytes, className, appName, timeStamp, ExcutePacket.Type.Code);
                codePacket.SetReturnAddress(Dns.GetHostName(), 8800);
                //1207
                codePacket.WaiteCallBack = false;
                transfer.Send<object>(codePacket);
            }

            //1.4 结点运行程序，直至结束
            DateTime submitTime = DateTime.UtcNow;
            while (!IsDelay(submitTime, 60))
            {
                if (CheckApplicationExit(timeStamp, appName, ipPaths.Count()))
                {
                    Console.WriteLine("应用程序已提交，您可以使用[task]命令查看状态.");
                    return;
                }
                Thread.Sleep(1000);
            }
            Console.WriteLine("应用程序运行时间太长出现故障.");

        }

        /// <summary>
        /// 启动返回数据监听
        /// </summary>
        private static void StartListen()
        {
            if (_server == null)
            {
                _server = new Server(Dns.GetHostName(), 8800, ProcessResponse);
                _server.Listen();
            }
        }

        /// <summary>
        /// 是否已经运行超时
        /// </summary>
        /// <param name="sumbitTime">应用程序提交时间</param>
        /// <param name="allowMaxTime">允许最长运行时间(单位：分)</param>
        /// <returns></returns>
        private static bool IsDelay(DateTime sumbitTime, long allowMaxTime)
        {
            return (DateTime.UtcNow - sumbitTime).TotalMinutes > allowMaxTime;
        }

        /// <summary>
        /// 检查应用程序是否已经退出
        /// </summary>
        /// <returns></returns>
        private static bool CheckApplicationExit(string timeStamp, string appName, int workerCount)
        {
            IEnumerable<string> finishedStates = StateHelper.GetChildren("ISE://application/" + timeStamp + "/" + appName);
            var enumerable = finishedStates as string[] ?? finishedStates.ToArray();
            if (enumerable.Any() && enumerable.Count() == workerCount)
            {
                foreach (var finishedState in enumerable)
                {
                    Logger.Info(finishedState + ":" + StateHelper.Get<string>(finishedState));
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 编译代码
        /// </summary>
        /// <param name="fileName">文件</param>
        /// <returns></returns>
        private static string CompileCode(string fileName)
        {
            if (File.Exists(fileName))
            {
                //Framework.Text.CodeCompiler compiler = new CodeCompiler();
                List<string> references = new List<string>
                {
                    "Iveely.CloudComputing.Client.exe",
                    "Iveely.Framework.dll",
                    "NDatabase3.dll"
                };
                return CodeCompiler.Compile(File.ReadAllLines(fileName), references);
            }
            throw new FileNotFoundException(fileName + " 文件没找到!");
        }

        private static byte[] ProcessResponse(byte[] bytes)
        {
            try
            {
                if (_showMsgFromRemote)
                {
                    Packet packet = Serializer.DeserializeFromBytes<Packet>(bytes);
                    byte[] dataBytes = packet.Data;
                    string information = Serializer.DeserializeFromBytes<string>(dataBytes);
                    Console.WriteLine("[响应输出： {0}] {1}", DateTime.UtcNow, information);
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
            }
            return null;

        }
    }

    /// <summary>
    /// 切分命令
    /// </summary>
    public class SplitCommond : RemoteCommand
    {
        public override void ProcessCmd(string[] args)
        {
            if (args.Length < 3 || args.Length == 4)
            {
                UnknowCommand();
                return;
            }
            //2.1 将文件切分成块
            List<string> workers = new List<string>(StateHelper.GetChildren("ISE://system/state/worker"));
            string filePath = args[1];
            if (File.Exists(filePath))
            {
                //普通切分模式
                if (args.Length == 3)
                {
                    FileBlock.Split(filePath, workers.Count);
                }
                //用户自定义切分
                else
                {
                    string splitStr = args[3];
                    List<int> keys = new List<int>();
                    for (int i = 4; i < args.Length; i++)
                    {
                        int index;
                        if (!int.TryParse(args[i], out index))
                        {
                            Logger.Error("index should be an int.");
                            break;
                        }
                        keys.Add(index);
                    }
                    FileBlock.Split(filePath, workers.Count, splitStr, keys.ToArray());
                }
            }
            else
            {
                Logger.Error(filePath + " is not found.");
                return;
            }

            //2.2 告诉worker即将发送文件块
            string remotePath = args[2];
            string[] childFiles = Directory.GetFiles(filePath + ".part");
            for (int i = 0; i < workers.Count; i++)
            {
                string[] ip =
                    workers[i].Substring(workers[i].LastIndexOf('/') + 1, workers[i].Length - workers[i].LastIndexOf('/') - 1)
                        .Split(',');
                Framework.Network.Synchronous.Client transfer = new Framework.Network.Synchronous.Client(ip[0],
                    int.Parse(ip[1]));
                ExcutePacket codePacket = new ExcutePacket(Encoding.UTF8.GetBytes(remotePath), string.Empty, string.Empty, string.Empty,
                    ExcutePacket.Type.FileFragment);
                codePacket.SetReturnAddress(Dns.GetHostName(), 8800);
                //1207
                codePacket.WaiteCallBack = false;
                transfer.Send<bool>(codePacket);
                Logger.Info(ip[0] + "," + ip[1] + " has been noticed to receive the file.");

                int maxRetryCount = 5;
                while (maxRetryCount > 0)
                {
                    try
                    {
                        FileTransfer fileTransfer = new FileTransfer();
                        fileTransfer.Send(childFiles[i], ip[0], 7001);
                        maxRetryCount = -1;
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception.Message);
                        maxRetryCount--;
                        if (maxRetryCount > 0)
                        {
                            Console.WriteLine("Now retry...");
                            Thread.Sleep(1000);
                        }
                    }
                }
                if (maxRetryCount == 0)
                {
                    Logger.Info(ip[0] + "," + ip[1] + " do not get the file.");
                }
            }
            //2.4 删掉切分的文件
            Directory.Delete(filePath + ".part", true);
            StateHelper.Put("ISE://File/" + remotePath, new FileInfo(filePath).Length / 1024);
        }
    }

    /// <summary>
    /// 下载命令
    /// </summary>
    public class DownloadCmd : RemoteCommand
    {
        public override void ProcessCmd(string[] args)
        {
            if (args.Length != 3)
            {
                UnknowCommand();
                return;
            }
            //3.1 创建子文件存放的文件夹
            string fileName = args[2];
            string remoteFilePath = args[1];
            if (remoteFilePath.EndsWith(".part"))
            {
                string partFileFolder = fileName + ".part";
                if (Directory.Exists(partFileFolder))
                {
                    Directory.Delete(partFileFolder, true);
                }
                Directory.CreateDirectory(partFileFolder);

                List<string> workers = new List<string>(StateHelper.GetChildren("ISE://system/state/worker"));
                for (int i = 0; i < workers.Count; i++)
                {
                    //3.2 通知下载文件
                    string[] ip =
                        workers[i].Substring(workers[i].LastIndexOf('/') + 1,
                            workers[i].Length - workers[i].LastIndexOf('/') - 1)
                            .Split(',');
                    Framework.Network.Synchronous.Client transfer = new Framework.Network.Synchronous.Client(ip[0],
                        int.Parse(ip[1]));
                    ExcutePacket codePacket = new ExcutePacket(Encoding.UTF8.GetBytes(remoteFilePath), string.Empty,
                        string.Empty, string.Empty,
                        ExcutePacket.Type.Download);
                    codePacket.SetReturnAddress(Dns.GetHostName(), 8800);
                    //1207
                    codePacket.WaiteCallBack = false;
                    transfer.Send<bool>(codePacket);
                    Logger.Info(ip[0] + "," + ip[1] + " has been noticed to send file " + fileName);

                    //3.3 准备接收文件块
                    FileTransfer fileTransfer = new FileTransfer();
                    fileTransfer.Receive(7002, partFileFolder + "/" + i);
                }

                //3.4 合并文件
                FileBlock.Merge(partFileFolder, fileName);
                Directory.Delete(partFileFolder, true);
            }
            else
            {
                List<string> workers = new List<string>(StateHelper.GetChildren("ISE://system/state/worker"));
                string[] ip =
                    workers[0].Substring(workers[0].LastIndexOf('/') + 1,
                        workers[0].Length - workers[0].LastIndexOf('/') - 1)
                        .Split(',');
                Framework.Network.Synchronous.Client transfer = new Framework.Network.Synchronous.Client(ip[0],
                    int.Parse(ip[1]));
                ExcutePacket codePacket = new ExcutePacket(Encoding.UTF8.GetBytes(remoteFilePath), string.Empty,
                    string.Empty, string.Empty,
                    ExcutePacket.Type.Download);
                codePacket.SetReturnAddress(Dns.GetHostName(), 8800);
                //1207
                codePacket.WaiteCallBack = false;
                transfer.Send<bool>(codePacket);
                Logger.Info(ip[0] + "," + ip[1] + " has been noticed to send file " + fileName);
                FileTransfer fileTransfer = new FileTransfer();
                fileTransfer.Receive(7002, fileName);
            }
        }
    }

    /// <summary>
    /// 删除命令
    /// </summary>
    public class DeleteCmd : RemoteCommand
    {
        public override void ProcessCmd(string[] args)
        {
            if (args.Length != 2)
            {
                UnknowCommand();
                return;
            }
            List<string> workers = new List<string>(StateHelper.GetChildren("ISE://system/state/worker"));
            string filePath = args[1];
            foreach (string t in workers)
            {
                //3.2 通知下载文件
                string[] ip =
                    t.Substring(t.LastIndexOf('/') + 1,
                        t.Length - t.LastIndexOf('/') - 1)
                        .Split(',');
                Framework.Network.Synchronous.Client transfer = new Framework.Network.Synchronous.Client(ip[0],
                    int.Parse(ip[1]));
                ExcutePacket codePacket = new ExcutePacket(Encoding.UTF8.GetBytes(filePath), string.Empty,
                    string.Empty, string.Empty,
                    ExcutePacket.Type.Delete);
                codePacket.SetReturnAddress(Dns.GetHostName(), 8800);
                //1207
                codePacket.WaiteCallBack = false;
                transfer.Send<bool>(codePacket);
                StateHelper.Delete("ISE://File/" + filePath);
            }
        }
    }

    /// <summary>
    /// 重命名命令
    /// </summary>
    public class RenameCmd : RemoteCommand
    {
        public override void ProcessCmd(string[] args)
        {
            if (args.Length != 3)
            {
                UnknowCommand();
                return;
            }
            List<string> workers = new List<string>(StateHelper.GetChildren("ISE://system/state/worker"));
            string filePath = args[1];
            string fileNewName = args[2];
            foreach (string t in workers)
            {
                string[] ip =
                    t.Substring(t.LastIndexOf('/') + 1,
                        t.Length - t.LastIndexOf('/') - 1)
                        .Split(',');
                Framework.Network.Synchronous.Client transfer = new Framework.Network.Synchronous.Client(ip[0],
                    int.Parse(ip[1]));
                Tuple<string, string> fileTuple = new Tuple<string, string>(filePath, args[2]);
                ExcutePacket codePacket = new ExcutePacket(Serializer.SerializeToBytes(fileTuple), string.Empty,
                    string.Empty, string.Empty,
                    ExcutePacket.Type.Rename);
                codePacket.SetReturnAddress(Dns.GetHostName(), 8800);
                //1207
                codePacket.WaiteCallBack = false;
                transfer.Send<bool>(codePacket);
                StateHelper.Rename("ISE://File/" + filePath, fileNewName);
            }
        }
    }

    /// <summary>
    /// 显示文件命令
    /// </summary>
    public class ListCmd : RemoteCommand
    {
        public override void ProcessCmd(string[] args)
        {
            if (args.Length > 2)
            {
                UnknowCommand();
                return;
            }
            string path = string.Empty;
            if (args.Length != 1)
            {
                path = args[1];
            }
            List<string> files = new List<string>(StateHelper.GetChildren("ISE://File/" + path));
            if (files.Any())
            {
                foreach (string file in files)
                {
                    Console.Write("          " + file.Replace("ISE://File", ""));
                    Console.WriteLine("   Size:" + StateHelper.Get<long>(file) + "KB");
                }
            }
            else
            {
                Console.WriteLine("Not found any files or folders.");
            }
        }
    }

    /// <summary>
    /// 停止任务命令
    /// </summary>
    public class KillCmd : RemoteCommand
    {
        public override void ProcessCmd(string[] args)
        {
            if (args.Length != 2)
            {
                UnknowCommand();
                return;
            }
            List<string> workers = new List<string>(StateHelper.GetChildren("ISE://system/state/worker"));
            foreach (string t in workers)
            {
                string[] ip =
                    t.Substring(t.LastIndexOf('/') + 1,
                        t.Length - t.LastIndexOf('/') - 1)
                        .Split(',');
                Framework.Network.Synchronous.Client transfer = new Framework.Network.Synchronous.Client(ip[0],
                    int.Parse(ip[1]));
                string appName = args[1];
                ExcutePacket codePacket = new ExcutePacket(Encoding.UTF8.GetBytes(appName), string.Empty,
                    appName, string.Empty,
                    ExcutePacket.Type.Kill);
                codePacket.SetReturnAddress(Dns.GetHostName(), 8800);
                codePacket.WaiteCallBack = true;
                string result = transfer.Send<string>(codePacket);
                Console.WriteLine(result);
            }
        }
    }

    /// <summary>
    /// 显示所有任务命令
    /// </summary>
    public class TaskCmd : RemoteCommand
    {
        public override void ProcessCmd(string[] args)
        {
            if (args.Length != 1)
            {
                UnknowCommand();
                return;
            }
            List<string> workers = new List<string>(StateHelper.GetChildren("ISE://system/state/worker"));
            foreach (string t in workers)
            {
                string[] ip =
                    t.Substring(t.LastIndexOf('/') + 1,
                        t.Length - t.LastIndexOf('/') - 1)
                        .Split(',');
                Framework.Network.Synchronous.Client transfer = new Framework.Network.Synchronous.Client(ip[0],
                    int.Parse(ip[1]));
                ExcutePacket codePacket = new ExcutePacket(Encoding.UTF8.GetBytes("Get task list"), string.Empty,
                    string.Empty, string.Empty,
                    ExcutePacket.Type.Task);
                codePacket.SetReturnAddress(Dns.GetHostName(), 8800);
                codePacket.WaiteCallBack = true;
                List<string> result = transfer.Send<List<string>>(codePacket);
                foreach (string s in result)
                {
                    Console.WriteLine("    " + s);
                }
            }

        }
    }
}
