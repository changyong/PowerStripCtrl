using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading;

namespace PowerStripCtrl
{
    class PowerStripCtrl
    {
        const int BUFFER_SIZE = 1000;

        private IPAddress PowserStripIP;
        private IPEndPoint iep;
        private Socket socket;
        private StreamWriter m_LogFile;

        static int Main(string[] args)
        {
            string strLogFileName;

            if (!GetCommandLineParamByName(args, "Log", out strLogFileName))
            {
                strLogFileName = "PowerStrip.log";
            }

            string strLogFileFullPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\" + strLogFileName;

            try
            {
                if (File.Exists(strLogFileFullPath))
                {
                    File.Delete(strLogFileFullPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can not delete old log file - " + strLogFileFullPath);
                Console.WriteLine("=======================");
                Console.WriteLine(ex.Message);
                return 1;
            }

            using (StreamWriter log = new StreamWriter(strLogFileFullPath, false, Encoding.ASCII))
            {
                int returnValue;
                PowerStripCtrl controller = new PowerStripCtrl(log);
                returnValue = controller.Start(args);
                controller.LogLine("\nFinished.\n");
                return returnValue;
            }
        }

        public PowerStripCtrl(StreamWriter logFile)
        {
            m_LogFile = logFile;
        }

        public void LogLine(string line)
        {
            m_LogFile.WriteLine(line);
        }

        private int Start(string[] args)
        {
            if (args.Length == 0 || args[0] == @"/?" || args[0] == @"?" || string.Compare(args[0], @"/help",
                true, System.Globalization.CultureInfo.InvariantCulture) == 0 || string.Compare(args[0], @"help",
                true, System.Globalization.CultureInfo.InvariantCulture) == 0)
            {
                PrintUsage();
                return 1;
            }

            string strStripIP = null;
            string strOutlet = null;
            if (GetCommandLineParamByName(args, "StripIP", out strStripIP) &&
                GetCommandLineParamByName(args, "Outlet", out strOutlet))
            {
                Console.WriteLine("StripIP - " + strStripIP);
                Console.WriteLine("Outlet - " + strOutlet);
            }
            else
            {
                PrintUsage();
                return 1;
            }

            Console.WriteLine("Rebooting ...\n");

            try
            {
                PowserStripIP = IPAddress.Parse(strStripIP);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                iep = new IPEndPoint(PowserStripIP, 23);
                socket.Connect(iep);

                this.SendMsg("apc\r\n");
                SendMsg("apc\r\n");
                SendMsg("1\r\n");
                SendMsg("3\r\n");
                SendMsg(strOutlet + "\r\n");//outlet NO.
                SendMsg("1\r\n");
                SendMsg("3\r\n");
                SendMsg("yes\r\n");
                SendMsg("\r\n");
                SendMsg('\u001B');
                SendMsg('\u001B');
                SendMsg('\u001B');
                SendMsg('\u001B');
                SendMsg("4\r\n");
                Thread.Sleep(6000);
            }
            catch (Exception e)
            {
                Console.WriteLine("Main() - Application failed!");
                Console.WriteLine(e.ToString());
                LogLine(e.ToString());
                return 1;
            }
            return 0;
        }

        private void SendMsg(string strSend)
        {
            this.GetMsg();
            Byte[] byteBufferSend = new Byte[strSend.Length];
            for (int i = 0; i < strSend.Length; i++)
            {
                Byte byteI = Convert.ToByte(strSend[i]);
                byteBufferSend[i] = byteI;
            }
            socket.Send(byteBufferSend);
        }

        private void SendMsg(Char charSend)
        {
            this.GetMsg();
            Byte[] byteBufferSend = new Byte[1];
            byteBufferSend[0] = Convert.ToByte(charSend);
            socket.Send(byteBufferSend);
        }

        private void GetMsg()
        {
            Thread.Sleep(300);
            Byte[] byteBufferGet = new Byte[BUFFER_SIZE];
            socket.Receive(byteBufferGet);
            string strRec = Encoding.UTF8.GetString(byteBufferGet);
            this.LogLine(strRec);
            //Console.WriteLine(strRec);
        }

        private static void PrintUsage()
        {
            string strUsage = "Usage:\n" +
                               "---------------------------------------\n" +
                               "- Reboot the power of specified outlet-\n" +
                               "\tPowerRepoot /StripIP:[IP address of Powerstrip] /Outlet:[Value] [/Log:[Name of Log File]]\n";

            Console.WriteLine(strUsage);
        }

        /// <summary>
        /// Get the value of parameter by name of parameter.
        /// </summary>
        /// <returns>
        /// true -- get the value specified by the name
        /// false -- can not find the value specified by the name
        /// </returns>
        private static bool GetCommandLineParamByName(string[] arguments, string strParamName, out string strParamValue)
        {

            string strParam = "";

            foreach (string strArg in arguments)
            {
                strParam = strArg;

                if (IsSwitchChar(strParam))
                {
                    strParam = strParam.Substring(1, strParam.Length - 1);
                }

                string[] strParamSplit = strParam.Split(':');
                if (strParamSplit.Length >= 2)
                {
                    if (string.Compare(strParamSplit[0], strParamName, true, System.Globalization.CultureInfo.InvariantCulture) == 0)
                    {
                        strParamValue = strParam.Substring(strParamSplit[0].Length + 1);
                        if (strParamValue.Trim() == "")
                        {
                            strParamValue = null;
                            return false;
                        }
                        return true;
                    }
                }
            }

            strParamValue = null;
            return false;
        }

        /// <summary>
        /// if first char is '/',it's a switch char
        /// </summary>
        /// <param name="strArgument"></param>
        /// <returns>true/false</returns>
        private static bool IsSwitchChar(string strArgument)
        {
            if (strArgument.Length > 0)
            {
                return (strArgument.Substring(0, 1) == "/");
            }
            else
            {
                return false;
            }
        }
    }
}
