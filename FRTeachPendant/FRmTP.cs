using FTPClient;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FRTeachPendant
{

    public partial class mainForm : Form
    {
        public mainForm()
        {
            InitializeComponent();

            //ToolTips
            initToolTips();
        }

        #region ToolTips
        private void initToolTips()
        {
            toolTip1.ToolTipTitle = "Mk Tips";

            // Tooltip for tb_robotIP
            toolTip1.SetToolTip(tb_robotIP, "Enter the IP address of the robot controller");

            // Tooltip for bt_ConnectRobot
            toolTip1.SetToolTip(bt_ConnectRobot, "Connect to the robot controller");

            // Tooltip for bt_tbKeyShow
            toolTip1.SetToolTip(bt_tbKeyShow, "Show/Hide the teach pendant keyboard");

            // Tooltip for bt_topMost
            toolTip1.SetToolTip(bt_topMost, "Keep the window always on top");

            // Tooltip for bt_Backup
            toolTip1.SetToolTip(bt_Backup, "Back up robot files to a local folder");

            // Tooltip for bt_KeyBoard
            toolTip1.SetToolTip(bt_KeyBoard, "Send a string to the robot controller");

            // Tooltip for tb_name
            toolTip1.SetToolTip(tb_name, "Administrator username for password-protected functions (level: INSTALL)");

            // Tooltip for tb_password
            toolTip1.SetToolTip(tb_password, "Administrator password for password-protected functions");

            // Tooltip for bt_help
            toolTip1.SetToolTip(bt_help, "Something went wrong? Click here for help");
        }
        #endregion

        #region Global Variables
        //FTP
        public bool robotIsConnected = false;
        public FTPClient.FtpClient ftpClient;

        //Button status
        bool bt_ShiftLPressed = false;

        //Robot Type
        string robotType = "";
        string robotName = "";

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

                string robotIp = tb_robotIP.Text;
                string adminName = tb_name.Text;
                string adminPass = tb_password.Text;

                Task.Run(() =>
                {
                    try
                    {
                        robotType = RobotChecker.GetRobotVersion(robotIp);
                        if (robotType == "Unknown")
                        {
                            UpdateUI(() =>
                            {
                                pnKeyboard.Enabled = false;
                                InitAll();
                                MessageBox.Show("Not the FANUC R-30iA,R-30iB,R-30iB Plus,R-50iA  Controller.\n");
                            });
                            return;
                        }


                        bool pcLoaded = false;
                        if (robotType == "R-30iA")
                        {
                            pcLoaded = FRLoadUserPC.LoadMKWebServer(robotIp, 1, adminName, adminPass, this);
                        }
                        if (robotType == "R-30iB")
                        {
                            pcLoaded = FRLoadUserPC.LoadMKWebServer(robotIp, 2, adminName, adminPass, this);
                        }
                        if (robotType == "R-30iB Plus")
                        {
                            pcLoaded = FRLoadUserPC.LoadMKWebServer(robotIp, 3, adminName, adminPass, this);
                        }
                        if (robotType == "R-50iA")
                        {
                            pcLoaded = FRLoadUserPC.LoadMKWebServer(robotIp, 4, adminName, adminPass, this);
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

                        Uri url = new Uri("http://" + robotIp + "/frh/cgtp/echo.htm");
                        UpdateUI(() =>
                        {
                            if (robotType == "R-30iA" || robotType == "R-30iB" || robotType == "R-30iB Plus")
                            {
                                wb_CGTP_IE.Visible = true;
                                wb_CGTP_IE.Url = url;

                                wb_CGTP_Edge.Visible = false;
                                wb_CGTP_Edge.Source = new Uri("about:blank");
                            }
                            else if (robotType == "R-50iA")
                            {
                                wb_CGTP_IE.Visible = false;
                                wb_CGTP_IE.Url = new Uri("about:blank");

                                wb_CGTP_Edge.Visible = true;
                                wb_CGTP_Edge.ZoomFactor = 0.55;
                                wb_CGTP_Edge.Source = url;
                            }
                        });

                        ftpClient = new FtpClient(robotIp, adminName, adminPass);
                        ftpClient.Connect();

                        UpdateUI(() =>
                        {
                            robotIsConnected = true;
                            // Exit View
                            if (cb_bfSelect.Checked)
                            {
                                // current screen
                                SendKeyCode(robotIp, (int)KeyCodes.TpiEdit);
                                System.Threading.Thread.Sleep(50);
                                // sencond screen
                                SendKeyCode(robotIp, (int)KeyCodes.KYDisp);
                                SendKeyCode(robotIp, (int)KeyCodes.TpiEdit);
                                System.Threading.Thread.Sleep(50);
                                // third screen
                                SendKeyCode(robotIp, (int)KeyCodes.KYDisp);
                                SendKeyCode(robotIp, (int)KeyCodes.TpiEdit);
                                System.Threading.Thread.Sleep(50);
                            }
                            
                            timer1.Start();
                            bt_ConnectRobot.Text = "Disconnect";
                            bt_ConnectRobot.BackColor = Color.GreenYellow;
                            
                            lb_HostName.Visible = true;
                            pnKeyboard.Enabled = true;
                            pnKeyboard.Visible = true;
                            pn_RobotCfg.Visible = false;
                            AllowDrop = true;
                           
                            //Show robot name
                            if (robotType == "R-30iA" || robotType == "R-30iB" || robotType == "R-30iB Plus")
                            {
                                robotName = MkWebClient.ReadVar("http://" + robotIp, "*SYSTEM*", "$HOSTNAME", "STR").Value;
                                lb_HostName.Text = "HostName:" + robotName;
                            }
                            else
                            {       
                                robotName = MkWebClient.ReadVar("http://" + robotIp, "$DID", "$HOSTCOMM.ROBOT_NAME", "STR").Value;
                                lb_HostName.Text = "HostName:" + robotName;
                            }
                            
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
            // Get the runtime path of the executable file
            string exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = System.IO.Path.Combine(exePath, "robot_ip.txt");

            string ip = tb_robotIP.Text;

            // Write the IP address to the file
            System.IO.File.WriteAllText(filePath, ip);
        }

        private void LoadIP()
        {
            // Get the runtime path of the executable file
            string exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = System.IO.Path.Combine(exePath, "robot_ip.txt");

            // Check whether the file exists
            if (System.IO.File.Exists(filePath))
            {
                // Read the IP address from the file
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
        bool isTop = true;
        private void bt_topMost_Click(object sender, EventArgs e)
        {
            if (isTop == true)
            {
                isTop = false;
                this.TopMost = false;
                bt_topMost.BackColor = SystemColors.Control;
            }
            else
            {
                isTop = true;
                this.TopMost = true;
                bt_topMost.BackColor = Color.GreenYellow;
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

        private async void InitWebView2()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MyWinFormsApp",
                    "WebView2");

                Directory.CreateDirectory(userDataFolder);

                var env = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    options: null);

                await wb_CGTP_Edge.EnsureCoreWebView2Async(env);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "WebView2 Init failed");
            }
        }

        private void mainForm_Load(object sender, EventArgs e)
        {   
            LoadIP();
            InitWebView2();
        }
        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            InitAll();
        }
        #endregion

        #region Teach Pendant Keyboard show
       bool istbKeyboardVisible = true;
        private void bt_tbKeyShow_Click(object sender, EventArgs e)
        {
            istbKeyboardVisible = !istbKeyboardVisible;
            if (istbKeyboardVisible)
            {
                pnKeyboard.Visible = true;
                this.ClientSize = new System.Drawing.Size(700, 840);   
                bt_tbKeyShow.BackColor = Color.GreenYellow;
            }
            else
            {
                pnKeyboard.Visible = false;
                this.ClientSize = new System.Drawing.Size(880, 520);
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
                string programName = null;
                string controllerType = await Task.Run(() => RobotChecker.GetRobotVersion(tb_robotIP.Text));
                if (controllerType == "R-30iA" || controllerType == "R-30iB" || controllerType == "R-30iB Plus")
                {
                    programName = MkWebClient.ReadVar("http://" + tb_robotIP.Text, "*SYSTEM*", "$TP_DEFPROG", "STR").Value;
                }
                else if (controllerType == "R-50iA")
                {
                    programName = MkWebClient.ReadVar("http://" + tb_robotIP.Text, "$DID", "$TP_STATUS.SELECTED_PROGRAM", "STR").Value;
                }

                if (string.IsNullOrEmpty(programName))
                {
                    MessageBox.Show("Program name not specified:$TP_DEFPROG", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    var progress = new Progress<int>(value => pbBackup.Value = value);

                    async Task<List<FtpFile>> ListFilesAsync(string serverPath)
                    {
                        return await Task.Run(() =>
                        {
                            ftpClient.ServerPath = serverPath;
                            return ftpClient.ListDirectory();
                        });
                    }
                    async Task DownloadFileAsync(string fileName)
                    {
                        string localFile = Path.Combine(localBackupFolder, fileName);
                        await Task.Run(() => ftpClient.Download(fileName, localFile));
                        processedFiles++;
                        int percent = totalFiles == 0 ? 0 : (int)((double)processedFiles / totalFiles * 100);
                        ((IProgress<int>)progress).Report(percent);
                    }

                    var mdbAllFiles = await ListFilesAsync("MDB:");
                    var mdbFiles = mdbAllFiles.Where(f =>
                        f.Type == FtpFileType.File &&
                        Path.GetFileNameWithoutExtension(f.Name).Equals(programName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (mdbFiles.Count == 0)
                    {
                        MessageBox.Show($"MDB: File with program name '{programName}' was not found.", "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        pbBackup.Visible = false;
                        return;
                    }

                    var downloadedFileNames = new HashSet<string>(mdbFiles.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);

                    var mdAllFiles = await ListFilesAsync("MD:");
                    var mdFilesToDownload = mdAllFiles.Where(f =>
                        f.Type == FtpFileType.File &&
                        Path.GetFileNameWithoutExtension(f.Name).Equals(programName, StringComparison.OrdinalIgnoreCase) &&
                        !downloadedFileNames.Contains(f.Name))
                        .ToList();

                    totalFiles = mdbFiles.Count + mdFilesToDownload.Count;
                    processedFiles = 0;

                    foreach (var f in mdbFiles)
                    {
                        await DownloadFileAsync(f.Name);
                    }

                    foreach (var f in mdFilesToDownload)
                    {
                        await DownloadFileAsync(f.Name);
                    }

                    pbBackup.Visible = false;
                    MessageBox.Show($"Program '{programName}' Download completed。\n" +
                        $"Total downloaded: {mdbFiles.Count + mdFilesToDownload.Count} files\n", "Download completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                pbBackup.Visible = false;
                MessageBox.Show("Error occurred during download:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Parse the returned text and extract the program name
        private string ParseProgramName(string text)
        {
            try
            {
                // Use regex to extract the content inside single quotes
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
                // Ignore exceptions and return null
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
                    string safeHostName = string.Concat(robotName
                    .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));

                    string backupFolderName = $"{safeHostName}_Backup_{timestamp}";
                    string backupFolderPath = Path.Combine(localPath, backupFolderName);
                    Directory.CreateDirectory(backupFolderPath);

                    // Delete comments
                    if (cb_Comment.Checked)
                    {
                        // Enable comments
                        if (robotType == "R-30iA" || robotType == "R-30iB" || robotType == "R-30iB Plus")
                        {
                            MkWebClient.WriteVar("http://" + tb_robotIP.Text, "*SYSTEM*", "$MNDSP_CMNT", "INT", "1");
                            MkWebClient.WriteVar("http://" + tb_robotIP.Text, "*SYSTEM*", "$UI_CONFIG.$IOSTAT_INST", "INT", "1");
                        }
                        else if (robotType == "R-50iA")
                        {
                            MkWebClient.WriteVar("http://" + tb_robotIP.Text, "$DID", "$PROGRAM_EDIT.COMMENT_DISPLAY_ON_EDIT_SCREEN", "INT", "1");
                        }
                    }
                    else
                    {
                        // Enable comments
                        if (robotType == "R-30iA" || robotType == "R-30iB" || robotType == "R-30iB Plus")
                        {
                            MkWebClient.WriteVar("http://" + tb_robotIP.Text, "*SYSTEM*", "$MNDSP_CMNT", "INT", "0");
                            MkWebClient.WriteVar("http://" + tb_robotIP.Text, "*SYSTEM*", "$UI_CONFIG.$IOSTAT_INST", "INT", "0");
                        }
                        else if (robotType == "R-50iA")
                        {
                            MkWebClient.WriteVar("http://" + tb_robotIP.Text, "$DID", "$PROGRAM_EDIT.COMMENT_DISPLAY_ON_EDIT_SCREEN", "INT", "0");
                        }
                    }

                    ftpClient.ServerPath = "MDB:";
                    List<string> tpFileNames = new List<string>(); // Used to store file names without the ".TP" extension

                    // Show the progress bar
                    this.Invoke(new Action(() =>
                    {
                        pbBackup.Visible = true;
                        pbBackup.Value = 0;
                    }));

                    // Use multithreading to download files
                    Task.Run(async () =>
                    {
                        try
                        {
                            // Get the file list in the FTP directory
                            List<FtpFile> files = ftpClient.ListDirectory();
                            int totalFiles = files.Count;
                            int processedFiles = 0;

                            foreach (FtpFile file in files)
                            {
                                // Only process files and ignore directories
                                if (file.Type == FtpFileType.File)
                                {
                                    string remoteFilePath = file.Name;
                                    file_name = remoteFilePath;
                                    string localFilePath = Path.Combine(backupFolderPath, file.Name);

                                    // Skip the A_MKWEB.PC file
                                    if (remoteFilePath.Equals("A_MKWEB.PC", StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }

                                    // Skip files starting with "-"
                                    if (remoteFilePath.StartsWith("-", StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }

                                    // If the file has a "TP" extension, store the file name without the ".TP" suffix
                                    if (remoteFilePath.EndsWith(".TP", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Remove the ".TP" suffix
                                        string fileNameWithoutTp = Path.GetFileNameWithoutExtension(remoteFilePath);
                                        tpFileNames.Add(fileNameWithoutTp);
                                    }

                                    // Download the file
                                    await Task.Run(() =>
                                    {
                                        ftpClient.Download(remoteFilePath, localFilePath);
                                    });

                                    // Update the progress bar
                                    processedFiles++;
                                    this.Invoke(new Action(() =>
                                    {
                                        pbBackup.Value = (int)((double)processedFiles / totalFiles * 100);
                                    }));
                                }
                            }

                            // Download MD: device files
                            ftpClient.ServerPath = "MD:";
                            int totalTpFiles = tpFileNames.Count;
                            int processedTpFiles = 0;

                            foreach (string tpFileName in tpFileNames)
                            {
                                // Add the ".LS" suffix
                                string remoteFilePath = tpFileName + ".ls";
                                string localFilePath = Path.Combine(backupFolderPath, remoteFilePath);

                                await Task.Run(() =>
                                {
                                    ftpClient.Download(remoteFilePath, localFilePath);
                                });

                                // Update the progress bar
                                processedTpFiles++;
                                this.Invoke(new Action(() =>
                                {
                                    pbBackup.Value = (int)((double)(processedFiles + processedTpFiles) / (totalFiles + totalTpFiles) * 100);
                                }));
                            }

                            // Return to the main thread to update the UI
                            this.Invoke(new Action(() =>
                            {
                                pbBackup.Visible = false;
                                MessageBox.Show("Backup completed!", "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }));
                        }
                        catch (Exception ex)
                        {
                            // Return to the main thread to update the UI
                            this.Invoke(new Action(() =>
                            {
                                pbBackup.Visible = false;
                                MessageBox.Show($"An error occurred: {ex.Message}, file: {file_name}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if(this.TopMost == true)
            {
                bt_topMost_Click(sender, e);
            }

            // Check teachPendant on
            if (MkWebClient.GetIoValue("http://" + tb_robotIP.Text, IOType.OperatorPanelOutput, 7).Value == 0)
            {
                MessageBox.Show("Program upload not allowed.\r\n Please turn the teach pendant mode switch to ON.", "Notify");
                return;
            }


            // Get the dragged file paths
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                // List of allowed file extensions
                string[] allowedExtensions = { ".TP", ".PC", ".VR", ".LS" };
                ftpClient.ServerPath = "MDB:"; // Set FTP server path

                // Create a list to store files to be uploaded
                List<string> filesToUpload = new List<string>();

                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file); // Get the file name part
                    string fileExtension = Path.GetExtension(file).ToUpper(); // Get the file extension and convert it to uppercase

                    // Check whether the file extension is in the allowed list
                    if (Array.IndexOf(allowedExtensions, fileExtension) >= 0)
                    {
                        filesToUpload.Add(file); // Add to the upload list
                    }
                    else
                    {
                        MessageBox.Show($"Uploading this file type is not allowed: {fileExtension} (file: {fileName})", "File Type Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                // If there are files to upload, show a confirmation dialog
                if (filesToUpload.Count > 0)
                {
                    DialogResult result = MessageBox.Show($"Do you want to upload {filesToUpload.Count} files to the FTP server?", "Confirm Upload", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    // Decide whether to upload based on the user's choice
                    if (result == DialogResult.Yes)
                    {
                        // Create a new thread to handle file uploads
                        Thread uploadThread = new Thread(() =>
                        {
                            try
                            {
                                int uploadedCount = 0;
                                foreach (string file in filesToUpload)
                                {
                                    string fileName = Path.GetFileName(file);
                                    ftpClient.Upload(file, fileName); // Upload file
                                    uploadedCount++;
                                }

                                // Return to the main thread to update the UI
                                this.Invoke(new Action(() =>
                                {
                                    MessageBox.Show($"Successfully uploaded {uploadedCount} files to the FTP server!", "Upload Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }));
                            }
                            catch (Exception ex)
                            {
                                // Return to the main thread to update the UI
                                this.Invoke(new Action(() =>
                                {
                                    MessageBox.Show($"An error occurred while uploading files: {ex.Message}", "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }));
                            }
                        });

                        // Start the thread
                        uploadThread.Start();
                    }
                    else
                    {
                        // If the user chooses to cancel the upload, show a message
                        MessageBox.Show("File upload has been canceled!", "Operation Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("There are no files allowed to be uploaded!", "Operation Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            pnKeyboard.Enabled = false;
            this.AllowDrop = false;
            bt_ConnectRobot.Text = "Connect";
            bt_ConnectRobot.BackColor = Color.White;
            lb_HostName.Text = "";

            wb_CGTP_IE.Navigate("about:blank");
            wb_CGTP_Edge.Source = new Uri("about:blank");

            pnKeyboard.Enabled = false;   
            tb_robotIP.Enabled = true; 
            lb_HostName.Visible = false;
            pn_RobotCfg.Visible = true;

        }
        #endregion

        #region Disable password
        private void bt_dispassword_Click(object sender, EventArgs e)
        {
            if (this.TopMost)
            {
                bt_topMost_Click(sender, e);
            }
            try
            {
                string passId = string.Empty;

                // Manual input
                passId = GetPassIdFromUser();
                if (string.IsNullOrEmpty(passId)) return;

                // Call DisablePass to get the ReleaseKey
                var disablePassResp = MkWebClient.DisablePass("http://" + tb_robotIP.Text, passId);

                if (disablePassResp.Status == 0)
                {
                    MessageBox.Show($"ReleaseKey:\n{disablePassResp.ReleaseKey}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"The server returned an error status: {disablePassResp.Status}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during execution: " + ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetPassIdFromUser()
        {
            // A reference to Microsoft.VisualBasic needs to be added to the project
            string passId = FRTeachPendant.UI.InputDialog.Show("Please enter the password ID", "Manually Enter Password ID");
            return passId;
        }
        #endregion

    }
}
