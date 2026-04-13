using FTPClient;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FR_TeachPendant
{

    public partial class mainForm : Form
    {
        public mainForm()
        {
            // Dpi Auto
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            InitializeComponent();

            //ToolTips
            initToolTips();
        }

        #region ToolTips
        private void initToolTips()
        {
            toolTip1.ToolTipTitle = "Mk小提示";

            // 为 tb_robotIP 设置提示信息
            toolTip1.SetToolTip(tb_robotIP, "输入机器人控制器的 IP 地址");

            // 为 bt_ConnectRobot 设置提示信息
            toolTip1.SetToolTip(bt_ConnectRobot, "连接到机器人控制器");

            // 为 bt_tbKeyShow 设置提示信息
            toolTip1.SetToolTip(bt_tbKeyShow, "显示/隐藏示教器键盘");

            // 为 bt_topMost 设置提示信息
            toolTip1.SetToolTip(bt_topMost, "设置窗口始终置顶");

            // 为 bt_Backup 设置提示信息
            toolTip1.SetToolTip(bt_Backup, "将机器人文件备份到本地文件夹");

            // 为 bt_KeyBoard 设置提示信息
            toolTip1.SetToolTip(bt_KeyBoard, "向机器人控制器发送字符串");

            // 为 tb_name 设置提示信息
            toolTip1.SetToolTip(tb_name, "密码保护功能的管理员用户名（级别 INSTALL）");

            // 为 tb_password 设置提示信息
            toolTip1.SetToolTip(tb_password, "密码保护功能的管理员密码");

            // 为 bt_help 设置提示信息
            toolTip1.SetToolTip(bt_help, "出错了? 点击我查看帮助");
            

        }
        #endregion

        #region Global Variables
        //FTP
        public bool robotIsConnected = false;
        public FTPClient.FtpClient ftpClient;

        //Button status
        bool bt_ShiftLPressed = false;

        //Comment Select
        bool commentIsSelect = false;
        #endregion

        #region Connect and disconnect
        private void bt_ConnectRobot_Click(object sender, EventArgs e)
        {
            #region Ping
            bool PingOk = false;
            using (Ping ping = new Ping())
            {
                try
                {
                    PingReply reply = ping.Send(tb_robotIP.Text, 100);
                    PingOk = reply.Status == IPStatus.Success;
                }
                catch (Exception ex)
                {
                    InitAll();
                    MessageBox.Show($"ERROR: iP Address invalid.\n Response: {ex.Message}");
                    return;
                }
            }
            if (PingOk == false)
            {
                InitAll();
                MessageBox.Show($"ERROR: Ping failed");
                return;
            }
            #endregion

            if (bt_ConnectRobot.Text == "Connect")
            {
                bt_ConnectRobot.Text = "Connecting";
                Task.Run(() =>
                {
                    try
                    {
                        string Content = RobotChecker.GetRobotVersion(tb_robotIP.Text);
                        if (Content == null)
                        {
                            UpdateUI(() =>
                            {
                                gb_tpKeyboard.Enabled = false;
                                InitAll();
                                MessageBox.Show("Not the FANUC R-30iB or R-30iB Plus Controller.\n");
                            });
                            return;
                        }

                        // Unlock HTTP
                        Spruncmd.WriteCMd(tb_robotIP.Text, "SETVAR $HTTP_AUTH[2].$TYPE 3");
                        Spruncmd.WriteCMd(tb_robotIP.Text, "SETVAR $HTTP_AUTH[3].$TYPE 3");

                        bool pcLoaded = false;
                        if (Content == "R-30iB")
                        {
                            pcLoaded = FRLoadUserPC.LoadMKWebServer(tb_robotIP.Text, 1, tb_name.Text, tb_password.Text, this);
                        }
                        if (Content == "R-30iB Plus")
                        {
                            pcLoaded = FRLoadUserPC.LoadMKWebServer(tb_robotIP.Text, 2, tb_name.Text, tb_password.Text, this);
                        }

                        if (!pcLoaded)
                        {
                            UpdateUI(() =>
                            {
                                InitAll();
                                MessageBox.Show($"Karel Program Load Error\n");
                            });
                            return;
                        }
                        

                        // Create FTP object
                        ftpClient = new FtpClient(tb_robotIP.Text, tb_name.Text, tb_password.Text);
                        ftpClient.Connect();

                        // Bind CGTP view
                        Uri url = new Uri("http://" + tb_robotIP.Text + "/frh/cgtp/echo.stm");
                        
                        UpdateUI(() =>
                        {
                            robotIsConnected = true;
                            // Exit View
                            if (cb_bfSelect.Checked)
                            {
                                // current screen
                                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiEdit);
                                System.Threading.Thread.Sleep(50);
                                // sencond screen
                                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KYDisp);
                                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiEdit);
                                System.Threading.Thread.Sleep(50);
                                // third screen
                                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KYDisp);
                                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiEdit);
                                System.Threading.Thread.Sleep(50);
                            }
                            
                            timer1.Start();
                            wb_CGTP.Url = url;
                            bt_ConnectRobot.Text = "Disconnect";
                            bt_ConnectRobot.BackColor = Color.GreenYellow;
                            gb_tpKeyboard.Enabled = true;
                            AllowDrop = true;
                            bt_tbKeyShow.Enabled = true;
                            tb_robotIP.Enabled = false;

                            lb_HostName.Text ="HostName:" + MkWebClient.ReadVar("http://" + tb_robotIP.Text, "*SYSTEM*", "$HOSTNAME","STR").Value;
                            tb_password.Enabled = false;
                            tb_name.Enabled = false;
                            gb_tpKeyboard.Visible = true;
                            SaveIP();
                        });
                    }
                    catch (Exception ex)
                    {
                        UpdateUI(() =>
                        {
                            InitAll();
                            MessageBox.Show($"ERROR: {ex.Message}");
                        });
                    }
                });
            }
            else if (bt_ConnectRobot.Text == "Disconnect")
            {
                Task.Run(() =>
                {
                    UpdateUI(() =>
                    {
                        InitAll();
                    });               
                }
                );

            }

        }

        private void UpdateUI(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }
        #endregion

        #region Send Key code
        public enum KeyCodes
        {
            // Arrow keys
            KyUpArrow = 212,
            KyDownArrow = 213,
            KyRightArrow = 208,
            KyLeftArrow = 209,

            // TPI keys
            TpiSelect = 143,
            TpiMenus = 144,
            TpiEdit = 145,
            TpiData = 146,
            TpiFunction = 147,

            // Misc. keys
            TpiItem = 148,
            TpiPctUp = 149,
            TpiPctDown = 150,
            TpiHold = 151,
            TpiStep = 152,
            TpiReset = 153,
            TpiGroup = 28,
            TpiIcon = 12288,

            // Shifted misc keys
            KyItemS = 154,
            TpiPctUpS = 155,
            TpiPctDownS = 156,
            TpiStepS = 157,
            TpiHoldS = 158,
            TpiResetS = 159,

            // Motion related keys
            TpiForward = 185,
            TpiBackward = 186,
            TpiCoord = 187,

            // Shifted motion related keys
            TpiForwardS = 200,
            TpiBackwardS = 201,
            TPiCoords = 202,

            // Keypad keys (shifted or unshifted)
            KyEnter = 13,
            KyBackspace = 8,
            KyComma = 44,
            KyMinus = 45,
            KyDot = 46,
            KyZero = 48,
            KyOne = 49,
            KyTwo = 50,
            KyThree = 51,
            KyFour = 52,
            KyFive = 53,
            KySix = 54,
            KySeven = 55,
            KyEight = 56,
            KyNine = 57,

            // Top row keys
            KyPrev = 128,
            KyF1 = 129,
            KyF2 = 131,
            KyF3 = 132,
            KyF4 = 133,
            KyF5 = 134,
            KyNext = 135,

            // Shifted top row keys
            KyPrevS = 136,
            KyF1S = 137,
            KyF2S = 138,
            KyF3S = 139,
            KyF4S = 140,
            KyF5S = 141,
            KyNextS = 142,

            // Shifted arrow keys
            KyUpArrowS = 204,
            KyDownArrowS = 205,
            KyRightArrowS = 206,
            KyLeftArrowS = 207,

            // User function keys
            KyUf1 = 173,
            KyUf2 = 174,
            KyUf3 = 175,
            KyUf4 = 176,
            KyUf5 = 177,
            KyUf6 = 178,
            KyUf7 = 210,

            // Shifted user function keys
            KyUf1S = 179,
            KyUf2S = 180,
            KyUf3S = 181,
            KyUf4S = 182,
            KyUf5S = 183,
            KyUf6S = 184,
            KyUf7S = 211,

            KYDispS = 227,
            KYDisp = 240,
            KYDiag = 239,
        }

        public void SendKeyCode(string ip, int keyCode)
        {        
            try
            {    
                if(robotIsConnected)
                {
                    int status = MkWebClient.SimTpKey("http://" + ip, keyCode.ToString());
                }
            }
            catch 
            {
                //None
            }
        }
        #endregion

        #region Save and Load IP
        private void SaveIP()
        {
            // 获取可执行文件的运行路径
            string exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = System.IO.Path.Combine(exePath, "robot_ip.txt");

            string ip = tb_robotIP.Text;

            // 将 IP 地址写入文件
            System.IO.File.WriteAllText(filePath, ip);
        }

        private void LoadIP()
        {
            // 获取可执行文件的运行路径
            string exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = System.IO.Path.Combine(exePath, "robot_ip.txt");

            // 检查文件是否存在
            if (System.IO.File.Exists(filePath))
            {
                // 从文件读取 IP 地址
                string ip = System.IO.File.ReadAllText(filePath);
                tb_robotIP.Text = ip;
            }
        }
        #endregion

        #region Button Event
        private void bt_menu_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiMenus);
        }

        private void bt_prev_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyPrevS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyPrev);
            }
        }

        private void bt_f1_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyF1S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyF1);
            }

        }

        private void bt_select_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiSelect);
        }

        private void bt_edit_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiEdit);
        }

        private void bt_data_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiData);
        }

        private void bt_fctn_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiFunction);
        }

        private void bt_i_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiIcon);
        }

        private void bt_disp_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KYDispS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KYDisp);
            }
        }

        private void bt_arrowUP_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUpArrowS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUpArrow);
            }
        }

        private void bt_arrowDOWN_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyDownArrowS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyDownArrow);
            }
        }

        private void bt_arrowLEFT_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyLeftArrowS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyLeftArrow);
            }
        }

        private void bt_arrowRIGHT_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyRightArrowS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyRightArrow);
            }
        }

        private void bt_reset_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiResetS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiReset);
            }

        }

        private void bt_backspace_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyBackspace);
        }

        private void bt_item_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiItem);
        }

        private void bt_enter_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyEnter);
        }

        private void bt_num7_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KySeven);
        }

        private void bt_num8_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyEight);
        }

        private void bt_num9_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyNine);
        }

        private void bt_num4_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyFour);
        }

        private void bt_num5_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyFive);
        }

        private void bt_num6_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KySix);
        }

        private void bt_num1_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyOne);
        }

        private void bt_num2_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyTwo);
        }

        private void bt_num3_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyThree);
        }

        private void bt_num0_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyZero);
        }

        private void bt_dot_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyDot);
        }

        private void bt_comm_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyComma);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyMinus);
            }

        }

        private void bt_diag_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KYDiag);
        }

        private void bt_posn_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf7S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf7);
            }
        }

        private void bt_io_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf6S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf6);
            }
        }

        private void bt_status_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf5S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf5);
            }
        }

        private void bt_setup_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf4S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf4);
            }
        }

        private void bt_moveMenu_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf3S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf3);
            }
        }

        private void bt_tool2_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf2S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf2);
            }
        }

        private void bt_tool1_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf1S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyUf1);
            }
        }

        private void bt_step_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiStep);
        }

        private void bt_hold_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiHold);
        }

        private void bt_fwd_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiForwardS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiForward);
            }
        }

        private void bt_bwd_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiBackwardS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiBackward);
            }
        }

        private void bt_coord_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TPiCoords);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiCoord);
            }
        }

        private void bt_group_Click(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiGroup);
        }

        private void bt_speedUp_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiPctUpS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiPctUp);
            }
        }

        private void bt_speedDown_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiPctDownS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiPctDown);
            }
        }

        private void bt_shiftL_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed == false)
            {
                bt_ShiftLPressed = true;
                bt_shiftL.BackColor = Color.GreenYellow;
            }
            else
            {
                bt_ShiftLPressed = false;
                bt_shiftL.BackColor = Color.MediumBlue;
            }
        }


        private void bt_f2_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyF2S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyF2);
            }
        }

        private void bt_f3_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyF3S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyF3);
            }

        }

        private void bt_f4_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyF4S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyF4);
            }
        }

        private void bt_f5_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyF5S);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyF5);
            }

        }

        private void bt_next_Click(object sender, EventArgs e)
        {
            if (bt_ShiftLPressed)
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyNextS);
            }
            else
            {
                SendKeyCode(tb_robotIP.Text, (int)KeyCodes.KyNext);
            }
        }

        private void bt_hold_Click_1(object sender, EventArgs e)
        {
            SendKeyCode(tb_robotIP.Text, (int)KeyCodes.TpiHold);
        }
        #endregion

        #region Window TopMost
        bool isTop = false;
        private void bt_topMost_Click(object sender, EventArgs e)
        {
            if (isTop == true)
            {
                isTop = false;
                this.TopMost = false;
                bt_topMost.BackColor = Color.White;
            }
            else
            {
                isTop = true;
                this.TopMost = true;
                bt_topMost.BackColor = Color.Yellow;
            }
        }
        #endregion

        #region KeyBoard
        private Thread sendThread;
        private byte[] dataBytes;
        private int sendIndex = 0;
        private bool isSending = false;
        private bool shouldStop = false;
        private void bt_KeyBoard_Click(object sender, EventArgs e)
        {
            //Disable TopMost
            this.TopMost = false;
            bt_topMost.BackColor = Color.White;

            using (InputDialog inputDialog = new InputDialog())
            {
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    string inputText = inputDialog.InputText;

                    dataBytes = Encoding.UTF8.GetBytes(inputText);
                    sendIndex = 0;
                    shouldStop = false;
                    isSending = true;

                    sendThread = new Thread(SendBytes);
                    sendThread.Start();
                }
            }
        }

        private void SendBytes()
        {
            while (isSending)
            {
                if (sendIndex < dataBytes.Length)
                {
                    SendKeyCode(tb_robotIP.Text, dataBytes[sendIndex]);
                    sendIndex++;
                }
                else
                {
                    isSending = false;
                }

                if (shouldStop)
                {
                    isSending = false;
                }
            }
        }
        #endregion

        #region Form Load and Closing
        private void mainForm_Load(object sender, EventArgs e)
        {
            LoadIP();
        }
        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            InitAll();
        }
        #endregion

        #region Teach Pendant Keyboard show
        bool bt_tpKeyPressed = false;
        private void bt_tbKeyShow_Click(object sender, EventArgs e)
        {
            if (bt_tpKeyPressed == false)
            {
                gb_tpKeyboard.Visible = true;
                bt_tpKeyPressed = true;
                bt_tbKeyShow.BackColor = Color.GreenYellow; ;
            }
            else
            {
                gb_tpKeyboard.Visible = false;
                bt_tpKeyPressed = false;
                bt_tbKeyShow.BackColor = SystemColors.Control;
            }
        }
        #endregion

        #region FTP Utilities
        #region FTP Download files
        private async void bt_download_Click(object sender, EventArgs e)
        {
            try
            {
                string text = await Task.Run(() => Kcl.WriteCMd(tb_robotIP.Text, "SHOW VAR $TP_DEFPROG"));
                string programName = ParseProgramName(text);
                if (string.IsNullOrEmpty(programName))
                {
                    MessageBox.Show("$TP_DEFPROG未指定程序名", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    if (folderDialog.ShowDialog() != DialogResult.OK)
                        return;

                    string localBackupFolder = folderDialog.SelectedPath;
                    pbBackup.Visible = true;
                    pbBackup.Value = 0;

                    int totalFiles = 0, processedFiles = 0;

                    // 进度回调，更新UI线程
                    var progress = new Progress<int>(value => pbBackup.Value = value);

                    // 包装获取文件列表为异步
                    async Task<List<FtpFile>> ListFilesAsync(string serverPath)
                    {
                        return await Task.Run(() =>
                        {
                            ftpClient.ServerPath = serverPath;
                            return ftpClient.ListDirectory();
                        });
                    }

                    // 下载单文件异步
                    async Task DownloadFileAsync(string fileName)
                    {
                        string localFile = Path.Combine(localBackupFolder, fileName);
                        await Task.Run(() => ftpClient.Download(fileName, localFile));
                        processedFiles++;
                        int percent = totalFiles == 0 ? 0 : (int)((double)processedFiles / totalFiles * 100);
                        ((IProgress<int>)progress).Report(percent);
                    }

                    // 1. MDB 列文件，筛选同名
                    var mdbAllFiles = await ListFilesAsync("MDB:");
                    var mdbFiles = mdbAllFiles.Where(f =>
                        f.Type == FtpFileType.File &&
                        Path.GetFileNameWithoutExtension(f.Name).Equals(programName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (mdbFiles.Count == 0)
                    {
                        MessageBox.Show($"MDB: 未找到程序名为'{programName}'的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        pbBackup.Visible = false;
                        return;
                    }

                    var downloadedFileNames = new HashSet<string>(mdbFiles.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);

                    // 2. MD 列文件，筛选同名但mdb没有的
                    var mdAllFiles = await ListFilesAsync("MD:");
                    var mdFilesToDownload = mdAllFiles.Where(f =>
                        f.Type == FtpFileType.File &&
                        Path.GetFileNameWithoutExtension(f.Name).Equals(programName, StringComparison.OrdinalIgnoreCase) &&
                        !downloadedFileNames.Contains(f.Name))
                        .ToList();

                    // 计算总文件数
                    totalFiles = mdbFiles.Count + mdFilesToDownload.Count;
                    processedFiles = 0;

                    // 3. 依次下载MDB文件
                    foreach (var f in mdbFiles)
                    {
                        await DownloadFileAsync(f.Name);
                    }

                    // 4. 下载MD文件
                    foreach (var f in mdFilesToDownload)
                    {
                        await DownloadFileAsync(f.Name);
                    }

                    pbBackup.Visible = false;
                    MessageBox.Show($"程序 '{programName}' 下载完成。\n" +
                        $"共下载 {mdbFiles.Count + mdFilesToDownload.Count} 个文件\n","下载完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                pbBackup.Visible = false;
                MessageBox.Show("下载过程出错：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 解析返回文本，提取程序名
        private string ParseProgramName(string text)
        {
            try
            {
                // 通过正则提取单引号中的内容
                var match = System.Text.RegularExpressions.Regex.Match(text, @"'([^']*)'");
                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    return match.Groups[1].Value.Trim();
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                // 忽略异常，返回null
            }
            return null;
        }
        #endregion
        #region FTP AOA backup
        private void bt_Backup_Click(object sender, EventArgs e)
        {
            // Disable TopMost
            this.TopMost = false;
            bt_topMost.BackColor = Color.White;

            string file_name = "";

            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string localPath = folderDialog.SelectedPath;
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
                    string backupFolderName = $"Backup_{timestamp}";
                    string backupFolderPath = Path.Combine(localPath, backupFolderName);
                    Directory.CreateDirectory(backupFolderPath);

                    // 删除注释
                    if (cb_Comment.Checked)
                    {
                        // 启用注释
                        Spruncmd.WriteCMd(tb_robotIP.Text, "SETVAR $MNDSP_CMNT 1");
                        // 启用输出IO状态
                        Spruncmd.WriteCMd(tb_robotIP.Text, "SETVAR $UI_CONFIG.$IOSTAT_INST 1");
                    }
                    else
                    {
                        // 删除注释
                        Spruncmd.WriteCMd(tb_robotIP.Text, "SETVAR $MNDSP_CMNT 0");
                        // 删除输出IO状态
                        Spruncmd.WriteCMd(tb_robotIP.Text, "SETVAR $UI_CONFIG.$IOSTAT_INST 0");
                    }

                    ftpClient.ServerPath = "MDB:";
                    List<string> tpFileNames = new List<string>(); // 用于存储去掉 ".TP" 后缀的文件名

                    // 显示进度条
                    this.Invoke(new Action(() =>
                    {
                        pbBackup.Visible = true;
                        pbBackup.Value = 0;
                    }));

                    // 使用多线程下载文件
                    Task.Run(async () =>
                    {
                        try
                        {
                            // 获取FTP目录下的文件列表
                            List<FtpFile> files = ftpClient.ListDirectory();
                            int totalFiles = files.Count;
                            int processedFiles = 0;

                            foreach (FtpFile file in files)
                            {
                                // 只处理文件，忽略目录
                                if (file.Type == FtpFileType.File)
                                {
                                    string remoteFilePath = file.Name;
                                    file_name = remoteFilePath;
                                    string localFilePath = Path.Combine(backupFolderPath, file.Name);

                                    // 跳过 A_MKWEB.PC 文件
                                    if (remoteFilePath.Equals("A_MKWEB.PC", StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }

                                    // 跳过以 "-" 开头的文件
                                    if (remoteFilePath.StartsWith("-", StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }

                                    // 如果是 "TP" 后缀的文件，存储去掉 ".TP" 后缀的文件名
                                    if (remoteFilePath.EndsWith(".TP", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // 去掉 ".TP" 后缀
                                        string fileNameWithoutTp = Path.GetFileNameWithoutExtension(remoteFilePath);
                                        tpFileNames.Add(fileNameWithoutTp);
                                    }

                                    // 下载文件
                                    await Task.Run(() =>
                                    {
                                        ftpClient.Download(remoteFilePath, localFilePath);
                                    });

                                    // 更新进度条
                                    processedFiles++;
                                    this.Invoke(new Action(() =>
                                    {
                                        pbBackup.Value = (int)((double)processedFiles / totalFiles * 100);
                                    }));
                                }
                            }

                            // 下载 MD: 设备文件
                            ftpClient.ServerPath = "MD:";
                            int totalTpFiles = tpFileNames.Count;
                            int processedTpFiles = 0;

                            foreach (string tpFileName in tpFileNames)
                            {
                                // 添加 ".LS" 后缀
                                string remoteFilePath = tpFileName + ".ls";
                                string localFilePath = Path.Combine(backupFolderPath, remoteFilePath);

                                await Task.Run(() =>
                                {
                                    ftpClient.Download(remoteFilePath, localFilePath);
                                });

                                // 更新进度条
                                processedTpFiles++;
                                this.Invoke(new Action(() =>
                                {
                                    pbBackup.Value = (int)((double)(processedFiles + processedTpFiles) / (totalFiles + totalTpFiles) * 100);
                                }));
                            }

                            // 回到主线程更新UI
                            this.Invoke(new Action(() =>
                            {
                                pbBackup.Visible = false;
                                MessageBox.Show("备份完成！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }));
                        }
                        catch (Exception ex)
                        {
                            // 回到主线程更新UI
                            this.Invoke(new Action(() =>
                            {
                                pbBackup.Visible = false;
                                MessageBox.Show($"发生错误：{ex.Message},文件:{file_name}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }));
                        }
                    });
                }
            }
        }
        #endregion
        #region FTP Upload 
        private void bt_UploadArea_DragDrop(object sender, DragEventArgs e)
        {
            // Disable TopMost
            this.TopMost = false;
            bt_topMost.BackColor = Color.White;

            // 获取拖动的文件路径
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                // 允许的文件扩展名列表
                string[] allowedExtensions = { ".TP", ".PC", ".VR", ".LS" };
                ftpClient.ServerPath = "MDB:"; // 设置FTP服务器路径

                // 创建一个列表来存储需要上传的文件
                List<string> filesToUpload = new List<string>();

                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file); // 获取文件名部分
                    string fileExtension = Path.GetExtension(file).ToUpper(); // 获取文件扩展名并转换为大写

                    // 检查文件扩展名是否在允许列表中
                    if (Array.IndexOf(allowedExtensions, fileExtension) >= 0)
                    {
                        filesToUpload.Add(file); // 添加到待上传文件列表
                    }
                    else
                    {
                        MessageBox.Show($"不允许上传该文件类型：{fileExtension}（文件：{fileName}）", "文件类型错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                // 如果有文件需要上传，弹出确认对话框
                if (filesToUpload.Count > 0)
                {
                    DialogResult result = MessageBox.Show($"是否上传 {filesToUpload.Count} 个文件到FTP服务器？", "确认上传", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    // 根据用户的选择决定是否上传
                    if (result == DialogResult.Yes)
                    {
                        // 创建一个新线程来处理文件上传
                        Thread uploadThread = new Thread(() =>
                        {
                            try
                            {
                                int uploadedCount = 0;
                                foreach (string file in filesToUpload)
                                {
                                    string fileName = Path.GetFileName(file);
                                    ftpClient.Upload(file, fileName); // 上传文件
                                    uploadedCount++;
                                }

                                // 回到主线程更新UI
                                this.Invoke(new Action(() =>
                                {
                                    MessageBox.Show($"成功上传 {uploadedCount} 个文件到FTP服务器！", "上传成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }));
                            }
                            catch (Exception ex)
                            {
                                // 回到主线程更新UI
                                this.Invoke(new Action(() =>
                                {
                                    MessageBox.Show($"上传文件时发生错误：{ex.Message}", "上传错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }));
                            }
                        });

                        // 启动线程
                        uploadThread.Start();
                    }
                    else
                    {
                        // 如果用户选择取消上传，显示提示信息
                        MessageBox.Show("文件上传已取消！", "操作取消", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("没有允许上传的文件！", "操作取消", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private void bt_UploadArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        #endregion
        #endregion

        #region Help Window
        private void bt_help_Click(object sender, EventArgs e)
        {
            Help help = new Help();
            help.Show();
        }
        #endregion

        #region Ping Robot Connection
        private async Task<bool> IsRobotConnectedAsync(string robotIp, int retryCount = 3, int timeout = 1000)
        {
            using (Ping ping = new Ping())
            {
                int successCount = 0;
                for (int i = 0; i < retryCount; i++)
                {
                    try
                    {
                        PingReply reply = await ping.SendPingAsync(robotIp, timeout);
                        if (reply.Status == IPStatus.Success)
                        {
                            successCount++;
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    await Task.Delay(100);
                }

                return successCount >= (retryCount / 2);
            }
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            bool robotIsConnected = await IsRobotConnectedAsync(tb_robotIP.Text);
            if (robotIsConnected)
            {
                this.robotIsConnected = true;
            }
            else
            {
                this.robotIsConnected = false;
                Application.Restart();
            }
        }
        #endregion

        #region Initialize
        private void InitAll()
        {
            timer1.Stop();
            
            ftpClient = null;
        
            robotIsConnected = false;
            gb_tpKeyboard.Enabled = false;
            this.AllowDrop = false;
            bt_ConnectRobot.Text = "Connect";
            bt_ConnectRobot.BackColor = Color.White;
            lb_HostName.Text = "";
            wb_CGTP.Navigate("about:blank");

            gb_tpKeyboard.Visible = false;
            bt_tpKeyPressed = false;
            bt_tbKeyShow.BackColor = SystemColors.Control;
            tb_robotIP.Enabled = true;
            bt_tbKeyShow.Enabled = true;

            tb_password.Enabled = true;
            tb_name.Enabled = true;
        }
        #endregion

        #region Disable password
        private void bt_dispassword_Click(object sender, EventArgs e)
        {
            try
            {
                string passId = string.Empty;
                
                // 手动输入
                passId = GetPassIdFromUser();
                if (string.IsNullOrEmpty(passId)) return;
               
                // 调用DisablePass获取ReleaseKey
                var disablePassResp = MkWebClient.DisablePass("http://" + tb_robotIP.Text, passId);
                
                if (disablePassResp.Status == 0)
                {
                    MessageBox.Show($"ReleaseKey:\n{disablePassResp.ReleaseKey}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"服务器返回错误状态：{disablePassResp.Status}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("执行出错：" + ex.Message, "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private string GetPassIdFromUser()
        {
            // 需在项目中添加对 Microsoft.VisualBasic 的引用
            string passId = FR_TeachPendant.UI.InputDialog.Show("请输入密码ID", "手动输入密码ID");
            return (passId);
        }
        #endregion
    }
}
