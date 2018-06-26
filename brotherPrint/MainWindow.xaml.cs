using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using bpac;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace brotherPrint
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //private List<IpInfo> list;
        private bpac.DocumentClass doc = new DocumentClass();
        private string printer;//打印机名称
        private string type;//打印类型
        GeneralTmpl gt = new GeneralTmpl();
        private static byte[] result = new byte[1024];

        /// <summary>
        /// 委托方法用于更新文本框的信息
        /// </summary>
        /// <param name="str"></param>
        delegate void myDelegate(string str);

        private static int myProt = 8885;   //端口  
        
        Socket serverSocket;//服务端socket
        Socket clientSocket;//客户端socket

        Thread thread;

        /// <summary>
        /// 主程序入口
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
          

        }

        /// <summary>
        /// 主窗口载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //初始化IP下拉数据
            this.ipaddr.ItemsSource = getIpList();
            //this.ipaddr.SelectedValuePath = "ipAddr";
            //this.ipaddr.DisplayMemberPath = "ipAddr";
            //初始化打印机下拉数据
            printName.ItemsSource = getBrotherPrinterList();
            printType.SelectedValuePath = "typeCode";
            printType.DisplayMemberPath = "typeName";
            printType.ItemsSource = initPrintTypeData();
            
        }

        private void ipaddr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            printName.IsEnabled = true;
        }

        private void printName_SelectionChanged(object sender, SelectionChangedEventArgs e) 
        {
            string ip = ipaddr.SelectedValue == null ? null : ipaddr.SelectedValue.ToString();
            if (ip == null)
            {
                MessageBox.Show("请选择IP");
            }
            else
            {
                string printer = printName.SelectedValue == null ? null : printName.SelectedValue.ToString();
                
                connState.Text = isOnline(printer) ? "连接" : "断开";
            }
        }

        private void printType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           // MessageBox.Show(printType.SelectedValue.ToString());
        }

        /// <summary>
        /// 启动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startBtn_Click(object sender, RoutedEventArgs e)
        {

            string ip = ipaddr.SelectedValue==null?null:ipaddr.SelectedValue.ToString();

            //打印机名称
            printer = printName.SelectedValue==null?null: printName.SelectedValue.ToString();

            type = printType.SelectedValue==null?null:printType.SelectedValue.ToString();

            if (ip == null || printer == null || type == null)
            {
                MessageBox.Show("参数为空");
            }
            else if (!isOnline(printer))
            {
                MessageBox.Show("打印机未连接");
            }
            else
            {

                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), myProt);

                serverSocket = new Socket(ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                serverSocket.Bind(ipep);

                serverSocket.Listen(10);

                Thread serverThread = new Thread(() => {
                    while (true)
                    {
                        clientSocket = serverSocket.Accept();
                        thread = new Thread(doWork);
                        thread.Start();
                    }
                });

                serverThread.Start();

                if (serverSocket.IsBound)
                {

                    //string str = "打开服务器（端口：" +8885 + "）成功\n";
                    this.logBox.AppendText("[打印服务开启,ip:" + ip + "]\n");
                    startBtn.IsEnabled = false;

                }
                else
                {
                    logBox.AppendText("打开服务器失败");
                }


                //logBox.AppendText("[打印服务开启,ip:" + ip + "]\n");
                //    logBox.AppendText("click"+i.ToString()+"\n");
                //    if (i==0)
                //    {
                //        Thread Star = new Thread(() => {
                //            while (true)
                //            {
                //                //logBox.AppendText(i.ToString()+"\n");
                //                this.logBox.Dispatcher.Invoke(
                //                new Action(
                //                    delegate
                //                    {
                //                        //this.userModeControl.IsEnabled = true;
                //                        this.logBox.AppendText(i.ToString() + "\n");
                //                    }
                //                    ));
                //                Thread.Sleep(500);
                //            }
                //        });
                //        Star.Start();
                //    }
                //    i++;

            }


        }

        //获取网卡地址
        public List<string> getIpList()
        {
            List<string> list = new List<string>();
            string name = Dns.GetHostName();
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            //IpInfo info = null;
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {                   
                    list.Add(ipa.ToString());
                    
                }

            }

            return list;

        }

        //获取兄弟打印机列表
        public List<string> getBrotherPrinterList()
        {
            List<string> printList = new List<string>();
           

            // 获取兄弟打印机
            object[] printers = (object[])doc.Printer.GetInstalledPrinters();
            if (printers == null || printers.Length == 0)
            {
                return null;
            }

            foreach (object o in printers)
            {
                printList.Add(o.ToString());
            }
            return printList;

        }

        //兄弟打印机是否在线
        public bool isOnline(string printerName)
        {
            return doc.Printer.IsPrinterOnline(printerName);
        }

        //-------------------------------------------------

        /// <summary>
        /// Socket客户端信息监听
        /// </summary>
        public void doWork() {

            myDelegate d = new myDelegate(updateReceiveTextBox);

            Socket s = clientSocket;//客户端信息 

            IPEndPoint ipEndPoint = (IPEndPoint)s.RemoteEndPoint;

            String address = ipEndPoint.Address.ToString();

            String port = ipEndPoint.Port.ToString();

            Byte[] inBuffer = new Byte[1024];

            Byte[] outBuffer = new Byte[1024];

            String inBufferStr;

            String outBufferStr;

            try

            {

                while (true)

                {

                    int len = s.Receive(inBuffer, 1024, SocketFlags.None);//如果接收的消息为空 阻塞 当前循环  

                    inBufferStr = Encoding.UTF8.GetString(inBuffer, 0, len);
                    //显示请求信息
                    this.Dispatcher.Invoke(d, address + ":" + port + "请求打印:"+inBufferStr);
                    
                    //回复内容
                    outBufferStr = gt.printLable(doc, printer, type, inBufferStr);

                    outBuffer = Encoding.UTF8.GetBytes(outBufferStr);

                    s.Send(outBuffer, outBuffer.Length, SocketFlags.None);

                }

            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(d, address + ":" + port + "断开连接");
                //Console.WriteLine(ex.Message);
                clientSocket.Close();

            }


        }


        public void updateReceiveTextBox(string str)
        {
            this.logBox.AppendText(str + "\n");
            //logBox.ScrollToEnd();
        }

        /// <summary>
        /// 日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void logBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            logBox.ScrollToEnd();
        }

        /// <summary>
        /// 主窗口关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_Closed(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        /// <summary>
        /// 清除文本框内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CleanLog_Click(object sender, RoutedEventArgs e)
        {
            this.logBox.Clear();
        }

        private List<PrintType> initPrintTypeData()
        {
            List<PrintType> types = new List<PrintType>();
            
            types.Add(new PrintType("gen","通用"));
            types.Add(new PrintType("jp", "日本定制"));
            types.Add(new PrintType("noAddr", "无目的地"));
            return types;
        }
    }

    /// <summary>
    /// 打印类型
    /// </summary>
    public class PrintType
    {
        public PrintType(string typeCode, string typeName)
        {
            this.typeCode = typeCode;
            this.typeName = typeName;
        }
        //编码
        public string typeCode { get; set; }
        //名称
        public string typeName { get; set; }
    }

}