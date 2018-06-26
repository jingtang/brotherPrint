using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using bpac;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace brotherPrint
{
    /// <summary>
    /// 通用模板打印
    /// </summary>
    public class GeneralTmpl
    {
        private static string IMEI_10 = Environment.CurrentDirectory + "/template/10_IMEI.lbx";    // Template file name @"c:\QL-Address.lbx";
        private static string IMEI_20 = Environment.CurrentDirectory + "/template/20_IMEI.lbx";
        private static string ROS_IMEI_10 = Environment.CurrentDirectory + "/template/ROS_10_IMEI.lbx";    // Template file name @"c:\QL-Address.lbx";
        private static string ROS_IMEI_20 = Environment.CurrentDirectory + "/template/ROS_20_IMEI.lbx";

        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="printName">打印机名称</param>
        /// <param name="templateType">模板类型</param>
        /// <param name="content">模板数据</param>
        /// <returns></returns>
        public string printLable(bpac.DocumentClass doc,String printName, string templateType, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return "1";
            }

            //未找到打印机
            if (printName == null || "".Equals(printName))
            {
                return "2";
            }

            doc.SetPrinter(printName, true);
            //Console.WriteLine("将调用兄弟打印机：" + doc.Printer.Name + " 打印" + doc.Printer.IsPrinterSupported(printName));
            //打印的干活
            //打开一个模板文件
            //解析需打印的内容
            JObject obj = JObject.Parse(content);
            JArray sns = (JArray)obj["sn"];
            JToken[] s = sns.ToArray();
            //是否ROS
            string isRos = (string)obj["ros"];
            if ("1".Equals(isRos))
            {
                if (s.Length < 11)
                {
                    //根据定制类型加载不同的模板
                    //加载10个的模板
                    return printTemplate(doc, ROS_IMEI_10, obj, s);
                }
                else
                {
                    //加载20个的模板
                    return printTemplate(doc, ROS_IMEI_20, obj, s);

                }
            }
            else
            {
                if (s.Length < 11)
                {
                    //加载10个的模板
                    return printTemplate(doc, IMEI_10, obj, s);
                }
                else
                {
                    //加载20个的模板
                    return printTemplate(doc, IMEI_20, obj, s);

                }
            }


        }

        /// <summary>
        /// 打印模板
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="template"></param>
        /// <param name="obj"></param>
        /// <param name="sns"></param>
        /// <returns></returns>
        private static string printTemplate(bpac.DocumentClass doc, string template, JObject obj, JToken[] sns)
        {
            if (doc.Open(template))
            {

                //订单号             
                string pi = (string)obj["pi"];
                //型号
                string model = (string)obj["model"];
                //箱号
                string no = (string)obj["no"];
                //数量
                string qty = (string)obj["qty"];

                int offset = pi.Length - 2;
                //设置PI号后面带的箱号
                string piNo = no;
                if (piNo.Length == 1)
                {
                    piNo = "00" + piNo;
                }
                if (piNo.Length == 2)
                {
                    piNo = "0" + piNo;
                }
                //设置PI号后面带的数量
                string piQty = qty;
                if (piQty.Length == 1)
                {
                    piQty = "00" + piQty;
                }
                if (piQty.Length == 2)
                {
                    piQty = "0" + piQty;
                }
                //客户名称截取订单号最后两位
                string customer = null;
                if (pi.Contains("-"))
                {
                    int strIdx = pi.IndexOf("-");

                    customer = pi.Substring(strIdx - 2, 2);
                }
                else
                {

                    customer = pi.Substring(offset, 2);
                }
                /* 
                 * 替换模板的value
                 */
                doc.GetObject("customer").Text = customer;
                doc.GetObject("model").Text = model;
                doc.GetObject("pi").Text = (pi + piNo + piQty);
                // doc.GetObject("no").Text = no;
                //最新需求箱号设置成空
                doc.GetObject("no").Text = no;
                doc.GetObject("qty").Text = qty + "pcs";
                //二维码
                if (doc.GetObject("qr") != null)
                {
                    doc.GetObject("qr").Text = pi + ";" + customer + ";" + model + ";" + no + ";" + qty;
                }


                //设置PI单号条型码
                doc.SetBarcodeData(doc.GetBarcodeIndex("pIbarcode"), (pi + piNo + piQty));

                //设置barcode
                int idx;
                for (idx = 0; idx < sns.Length; idx++)
                {
                    doc.SetBarcodeData(doc.GetBarcodeIndex("barcode" + (idx + 1)), sns[idx] + "");

                }

                //将多余的SN框设置成空的
                if (idx < 10)
                {
                    Console.WriteLine("idx = " + idx);
                    for (; idx < 10; idx++)
                    {
                        doc.GetObject("sn" + (idx + 1)).Text = "";
                        doc.SetBarcodeData(doc.GetBarcodeIndex("barcode" + (idx + 1)), "");
                    }
                    Boolean st = doc.StartPrint("", bpac.PrintOptionConstants.bpoDefault);
                    Boolean po = doc.PrintOut(1, bpac.PrintOptionConstants.bpoDefault);
                    // Console.WriteLine(st + "--" + doc.ErrorCode + "---" + po);
                    doc.EndPrint();
                    doc.Close();
                    return doc.ErrorCode == 0 ? "0" : "5";
                }
                else if (idx == 10)
                {
                    Boolean st = doc.StartPrint("", bpac.PrintOptionConstants.bpoDefault);
                    Boolean po = doc.PrintOut(1, bpac.PrintOptionConstants.bpoDefault);
                    //Console.WriteLine(st + "--" + doc.ErrorCode + "---" + po);
                    doc.EndPrint();
                    doc.Close();
                    return doc.ErrorCode == 0 ? "0" : "5";
                }
                else if (idx > 10 && idx <= 20)
                {
                    for (; idx < 20; idx++)
                    {
                        doc.GetObject("sn" + (idx + 1)).Text = "";
                        doc.SetBarcodeData(doc.GetBarcodeIndex("barcode" + (idx + 1)), "");

                    }
                    Boolean st = doc.StartPrint("", bpac.PrintOptionConstants.bpoDefault);
                    Boolean po = doc.PrintOut(1, bpac.PrintOptionConstants.bpoDefault);
                    //Console.WriteLine(st + "--" + doc.ErrorCode + "---" + po);
                    doc.EndPrint();
                    doc.Close();
                    return doc.ErrorCode == 0 ? "0" : "5";
                }
                else
                {
                    return "5";
                }

            }
            else
            {
                //Console.WriteLine("模板文件打开失败！！！！！！");
                //找不到模板
                return "4";
            }
        }
    }
}
