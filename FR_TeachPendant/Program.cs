using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Windows.Forms;

namespace FR_TeachPendant
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            #region 检查存在OCX注册
            // 指定要检查的注册表路径和值名
            string typeLibGuid = "{34F4C4DB-A64B-4D87-99DA-042F7FB7DEBA}";
            string registryPath = $@"TypeLib\{typeLibGuid}";

            // 检查注册表值是否存在
            if (CheckRegistryKeyExists(registryPath) == false)
            {
                // 弹出对话框询问用户是否进行注册
                DialogResult result = MessageBox.Show("检测到OCX文件未注册，是否现在进行注册？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    // 用户选择不注册，提示并退出程序
                    MessageBox.Show("未注册OCX文件可能导致示教器画面无法正常显示,只能使用备份/上传功能。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    goto RunApplication;
                }

                #region 提权
                // 获取当前的Windows用户
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);

                // 判断当前用户是否是管理员
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    // 如果已经是管理员，直接进行注册操作
                    goto RegisterOCX;
                }
                else
                {
                    // 创建启动对象
                    ProcessStartInfo processStartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = Application.ExecutablePath,
                        Verb = "runas"
                    };

                    try
                    {
                        Process.Start(processStartInfo);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("无法以管理员身份运行程序，请手动右键点击程序并选择'以管理员身份运行'。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    Application.Exit();
                    return;
                }
            #endregion
                #region 注册OCX文件
                RegisterOCX:
                // 获取当前执行文件的目录
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string directory = Path.GetDirectoryName(exePath);

                // 构建OCX文件的完整路径
                string ocxPath = Path.Combine(directory, "fripendant.ocx");

                // 创建Process对象来调用regsvr32工具
                Process p = new Process();
                p.StartInfo.FileName = "regsvr32.exe";
                p.StartInfo.Arguments = "/s " + "\"" + ocxPath + "\""; // 使用引号防止路径中的空格导致错误
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.Verb = "runas";
                p.Start();

                // 读取regsvr32的输出信息
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                // 检查regsvr32的退出代码
                if (p.ExitCode != 0)
                {
                    MessageBox.Show("注册OCX文件失败，示教器画面将不能正常显示，请从后面的Help帮助页面 安装VC运行库。\r\n" +
                                            "请确保安装了以下VC运行库：\r\n" +         
                                            "- Microsoft Visual C++ 2008 Redistributable - 9.0.30729\r\n",      
                                            "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Run(new Help());
                    return;
                }
                else
                {
                    MessageBox.Show("OCX文件注册成功,请重新运行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Exit();
                    return;
                }
                #endregion
            }
        #endregion

            RunApplication:
            Application.Run(new mainForm());
        }

        static bool CheckRegistryKeyExists(string path)
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(path, false))
                {
                    return key != null;
                }
            }
            catch (SecurityException)
            {
                // 当前用户可能没有足够的权限访问注册表项
                Console.WriteLine("没有足够的权限来检查注册表项。");
                return false;
            }
        }
    }
}
