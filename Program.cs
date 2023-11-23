// Decompiled with JetBrains decompiler
// Type: ConsoleApp1.Program
// Assembly: ConsoleApp1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B8A156E6-FD24-4149-9556-A1F93751DB75
// Assembly location: D:\Friwo\KitchenEyes\ConsoleApp1.dll

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Net.Http.Headers;
using ZXing;
using ZXing.Datamatrix;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;

namespace KitchenEyesApp
{
    internal class Program
    {
        private static string serialNumber { get; set; }

        private static string flag { get; set; }

        public static string? internalCode { get; set; }
        public static string? orderNo { get; set; }
        public static string? partNo { get; set; }
        public static string? hexValue { get; set; }
        public static int? gsid { get; set; } = 12000000;
        private static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    //Program.serialNumber = args[0];
                    //Program.flag = args[1];                  
                    Console.Write("Internal Barcode: ");
                    internalCode = Console.ReadLine();
                    orderNo = internalCode.Split("-")[2];

                    ////GET GSID FROM API////
                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage requestMessage = new HttpResponseMessage();
                    requestMessage = client.GetAsync("API url").Result;
                    if (requestMessage.IsSuccessStatusCode)
                    {
                        Console.WriteLine("");
                        gsid = int.Parse(requestMessage.Content.ReadAsStringAsync().Result);
                    }
                    client.DefaultRequestHeaders.Accept.Remove(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.Dispose();
                    /////////////////////////

                    if (gsid >= 1000000 && gsid < 10000000)
                    {
                        hexValue = gsid.Value.ToString("X").PadLeft(8, '0');
                        flag = "1";
                    }
                    else if (gsid >= 10000000 && gsid <= 19999999)
                    {
                        hexValue = gsid.Value.ToString("X").PadLeft(8, '0');
                        flag = "0";
                    }
                    Program.serialNumber = hexValue;
                    Program.flag = flag;

                    //// Main
                    //Program.serialNumber = "000F4245";
                    //Program.flag = "1";

                    //// Sensor
                    ////Program.serialNumber = "00989682";
                    ////Program.flag = "0";

                    Program.LaunchCommandLineApp();
                }
                catch (Exception e)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{e.Message}" );
                    Console.ResetColor();
                }
            }               
        }

        private static void LaunchCommandLineApp()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            try
            {               
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.FileName = (Program.flag == "0") ? "C:\\sensor_unit\\ELPRO.CfgGen.Sensor.exe" : "C:\\main_unit\\ELPRO.CfgGen.Main.exe";
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                uint uint32 = Convert.ToUInt32(Program.serialNumber.ToString().Replace("-", ""), 16);
                startInfo.Arguments = (Program.flag == "0") ? "gen -sn " + Program.serialNumber : string.Format("gen -sn {0} -sw {1}", (object)Program.serialNumber, (object)uint32);          
                using (Process process = Process.Start(startInfo))
                {
                    //PrintCode(serialNumber);
                    process.WaitForExit();
                    if(process.ExitCode == 0)
                    {
                        Program.Flash();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Generate Fail");
                    }
                }                    
            }
            catch (Exception ex)
            {
                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"{ex.Message}");
                Console.ResetColor();
            }
            Console.ResetColor();
        }

        public static void PrintCode(string code)
        {
            try
            {
                var qrCodeWriter = new BarcodeWriter();
                var qrCodeData = code;
                qrCodeWriter.Format = BarcodeFormat.DATA_MATRIX;
                var option = new DatamatrixEncodingOptions
                {
                    CharacterSet = "UTF-8",
                    Width = 30,
                    Height = 30,
                };
                qrCodeWriter.Options = option;
                var qrCodeResult = qrCodeWriter.Encode(qrCodeData);
                var bitmap = new Bitmap(qrCodeResult.Width, qrCodeResult.Height);
                for (int x = 0; x < qrCodeResult.Width; x++)
                {
                    for (int y = 0; y < qrCodeResult.Height; y++)
                    {
                        if (qrCodeResult[x, y])
                        {
                            bitmap.SetPixel(x, y, Color.Black);
                        }
                        else
                        {
                            bitmap.SetPixel(x, y, Color.White);
                        }
                    }
                }
                using (var printDocument = new PrintDocument())
                {
                    printDocument.PrintPage += (s, e) =>
                    {
                        Font drawFont = new Font("Arial", 8);
                        SolidBrush drawBrush = new SolidBrush(Color.Black);
                        float x = 36.0F;
                        float y = 3.0F;
                        float width = 33.0F;
                        float height = 80.0F;
                        RectangleF drawRect = new RectangleF(x, y, width, height);
                        e.Graphics.DrawImage(bitmap, 0, 0);
                        e.Graphics.DrawString(code, drawFont, drawBrush, drawRect);
                    };
                    printDocument.Print();
                }
            }
            catch(Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
            }
            Console.ResetColor();
        }

        public static void Flash()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "C:\\KitchenEyesApp\\flash_sensor.cmd";
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Program.PrintCode(Program.serialNumber);
                    bool status = Program.UpdateGSIDStatus(Program.serialNumber);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Flash Success");
                    if(status)
                    {
                        Console.WriteLine("Update GSID Status Success");
                    }    
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Flash Fail");                   
                }
            }
        }

        public static bool UpdateGSIDStatus(string gsid)
        {
            try
            {
                //API Update GSID Status
                //
                ////////////////////////
                return true;
            }
            catch (Exception ex)
            {
                return false;   
            }
        }
    }
}
