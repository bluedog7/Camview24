using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Ozeki.Camera;
using Ozeki.Media;
using System.Configuration;
using _02_PTZ_Camera_Motion_Control.LOG;

namespace _02_PTZ_Camera_Motion_Control
{
    public partial class MainForm : Form
    {
       private CameraURLBuilderWF _myCameraUrlBuilder;

        private Speaker _speaker;
        private int CurrentCamera = 0;
        private int maxcam = 2;
        private int Panasonic = 0;
        private IpCameraHandler[] Cameras;
        private RadioButton[] Ctlchecked;
        private VideoViewerWF[] videoViewerWFs;
        private String[] connectStr;
        AppSettingsReader ar;

        public MainForm()
        {
            ar = new AppSettingsReader();
            _speaker = Speaker.GetDefaultDevice();
            Ctlchecked = new RadioButton[24];
            connectStr = new String[24];
            Cameras = new IpCameraHandler[24];
            videoViewerWFs = new VideoViewerWF[24];
            InitializeComponent();
            Log.OnLogMessageReceived += Log_OnLogMessageReceived;
            Ozeki.Common.LicenseManager.Instance.SetLicense("OZSDK-IBS032CAM-200204-FE6FF385", "TUNDOjAsTVBMOjAsRzcyOTpmYWxzZSxNU0xDOjAsTUZDOjAsTVdQQzowLE1JUEM6MzIsVVA6MjAyMS4wMi4wNCxQOjIxOTkuMDEuMDF8czJDRHg1c1FiZjV1NmJ4UUFyK2FtTnBENjJDTEtmY2pZQnVLaUplVDJtU01QcUk4S2FCbnA5SVV6YWRlZWFzMzM3dW9aTjNjNU9FUW03ZDhCNFhHWVE9PQ==");

        }

        private void RadioButton_Click(object sender, EventArgs e)
        {
            RadioButton button = sender as RadioButton;
            String StrNum;

            StrNum = button.Name.Substring(11, button.Name.Length - 11);

            CurrentCamera = Int32.Parse(StrNum) - 1;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Panasonic = (int)ar.GetValue("Panasonic", typeof(int));
            maxcam = (int)ar.GetValue("maxcam", typeof(int));
            for (int i = 0; i < maxcam; i++)
            {
                Cameras[i] = new IpCameraHandler();
                Cameras[i].camnum = i;
                Cameras[i].CameraStateChanged += ModelCameraStateChanged;
                Cameras[i].CameraErrorOccured += ModelCameraErrorOccured;
                videoViewerWFs[i] = (VideoViewerWF)this.Controls["videoViewerWF" + (i + 1).ToString()];
                videoViewerWFs[i].SetImageProvider(Cameras[i].ImageProvider);
                videoViewerWFs[i].Click += VideoViewerWF_Click;
                Ctlchecked[i] = (RadioButton)this.Controls["radioButton" + (i + 1).ToString()];
                Ctlchecked[i].Checked = false;
                Ctlchecked[i].Click += RadioButton_Click;
                Ctlchecked[i].Text = (String)ar.GetValue("group" + (i + 1).ToString(), typeof(String));
                if (Panasonic==0)
                connectStr[i] = String.Format("http://{0}:80;Username=admin;Password={1};Transport=TCP;", (String)ar.GetValue("cam" + (i + 1).ToString(), typeof(String)), (String)ar.GetValue("password" + (i + 1).ToString(), typeof(String)));
                else
                connectStr[i] = String.Format("http://{0}:80;Username=admin;Password={1};Transport=UDP;", (String)ar.GetValue("cam" + (i + 1).ToString(), typeof(String)), (String)ar.GetValue("password" + (i + 1).ToString(), typeof(String)));

            }
            _myCameraUrlBuilder = new CameraURLBuilderWF();

            //InitializeViewer();
            

            //comboBox_Direction.DataSource = Enum.GetValues(typeof(PatrolDirection));
        }

        private void InitializeViewer()
        {
            ControlToCenter();

        //    heightText.Text = videoViewerWF1.Size.Height.ToString(CultureInfo.InvariantCulture);
        //    widthText.Text = videoViewerWF1.Size.Width.ToString(CultureInfo.InvariantCulture);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Cameras[0].Stop();
        }

        private void ModelCameraStateChanged(object sender, CameraStateEventArgs e)
        {
            IpCameraHandler cam = sender as IpCameraHandler;
            InvokeGuiThread(() =>
            {
                Log.Write(cam.camnum.ToString()+" Camera state: " +e.State);

                switch (e.State)
                {
                    // The list of streams become available at the Streaming state.
                    case CameraState.Streaming:

                        button_Connect.Enabled = false;

                        videoViewerWFs[cam.camnum].Start();
                        //ClearFields();
                        GetCameraStreams(cam.camnum);

                        button_Disconnect.Enabled = true;

                        //if (Cameras[0].Camera.UriType != CameraUriType.RTSP)
                        //    InitializeTrackBars();

                        break;

                    case CameraState.Disconnected:
                        button_Disconnect.Enabled = false;
                        videoViewerWFs[cam.camnum].Stop();
                        button_Connect.Enabled = true;
                        break;
                }
            });
        }

        private void GetCameraStreams(int camnum)
        {
            if (Cameras[0].Camera.AvailableStreams.Any())
            {
                var selected = 0;
                InvokeGuiThread(() =>
                {
                    for (var index = 0; index < Cameras[camnum].Camera.AvailableStreams.Count(); index++)
                    {
                        if (Cameras[camnum].Camera.CurrentStream.Name == Cameras[camnum].Camera.AvailableStreams[index].Name)
                        {
                            selected = index;
                        }

                        var name = Cameras[camnum].Camera.AvailableStreams[index].Name + " " + (Cameras[camnum].Camera.AvailableStreams[index].VideoEncoding != null ? Cameras[camnum].Camera.AvailableStreams[index].VideoEncoding.Resolution.ToString() : "_" + index);
                     //   StreamCombo.Items.Add(name);
                    }
                //    StreamCombo.SelectedIndex = selected;

                });
            }
        }

        private void ModelCameraErrorOccured(object sender, CameraErrorEventArgs e)
        {
            InvokeGuiThread(() => Log.Write("Camera error: " + (e.Details ?? e.Error.ToString())));
        }

        private void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var link = new LinkLabel.Link { LinkData = "http://www.camera-sdk.com/" };

            if (link.LinkData != null) Process.Start(link.LinkData as string);
        }

        #region Connect - Disconnect

        private void button_Connect_Click(object sender, EventArgs e)
        {
            ClearFields();
            // ONVIF
            
            ConnectIpCam();
            _speaker.Start();
        }

        private void button_Disconnect_Click(object sender, EventArgs e)
        {
            if (Cameras[0].Camera != null)
                Cameras[0].Disconnect();

           // ClearFields();
        }

        private void ConnectIpCam()
        {
            for (int i = 0; i < maxcam; i++)
            {
                Cameras[i].ConnectOnvifCamera(connectStr[i]);
            }
            for (int i = 0; i < maxcam; i++)
            {
                videoViewerWFs[i].Start();
            }
        }

        #endregion

        #region LOG

        void Log_OnLogMessageReceived(object sender, LogEventArgs e)
        {
            InvokeGuiThread(() =>
            {
                System.Diagnostics.Debug.WriteLine(e.LogMessage);
                LogScroll();
            });
        }

        void LogScroll()
        {
  
        }

        #endregion



        private void ClearFields()
        {

        }

        #region Stream select

        private void StreamCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var combo = sender as ComboBox;
      /*      if (combo == null || combo.SelectedIndex == -1) return;
            AudioInfoText.Clear();
            VideoInfoText.Clear();

            var CurrentStream = Cameras[0].Camera.AvailableStreams[StreamCombo.SelectedIndex];
            if (CurrentStream == null) throw new ArgumentNullException("Stream");
            Log.Write("Camera changed stream to " + CurrentStream.Name);

            Cameras[0].Camera.Start(CurrentStream);

            InvokeGuiThread(() =>
            {
                DetailsText.Text = Cameras[0].GetDeviceInfo();
                AudioInfoText.Text = Cameras[0].StreamInfoAudio();
                VideoInfoText.Text = Cameras[0].StreamInfoVideo();
            }); */
        }
        #endregion

        #region Image Size
        private void applyButton_Click(object sender, EventArgs e)
        {

        }

        void ControlToCenter()
        {

        }

        #endregion


        #region Image Settings

        private void TrackbarPropertiesScroll(object sender, EventArgs e)
        {
        }

        private void RefreshTrackBars()
        {
        }

        private void RefreshFrameRate()
        {
        }

        private void InitializeTrackBars()
        {

        }

        #endregion
        private void VideoViewerWF_Click(object sender, EventArgs e)
        {
            VideoViewerWF vv = sender as VideoViewerWF;
            String StrNum;

            StrNum = vv.Name.Substring(13, vv.Name.Length - 13);

            CurrentCamera = Int32.Parse(StrNum) - 1;
            Ctlchecked[CurrentCamera].Checked = true;
        }

        private void InvokeGuiThread(Action action)
        {
            BeginInvoke(action);
        }

        private void Flip(object sender, EventArgs e)
        {

         }

        private void MouseDownMove(object sender, MouseEventArgs e)
        {
            var button = sender as Button;
            if (button != null) Cameras[CurrentCamera].Move(button.Tag.ToString());
        }

        private void MouseUpMove(object sender, MouseEventArgs e)
        {
            if (Cameras[CurrentCamera].Camera == null) return;
            Cameras[CurrentCamera].Camera.CameraMovement.StopMovement();
        }

        private void button_Home_Click(object sender, EventArgs e)
        {
            if (Cameras[CurrentCamera].Camera == null) return;
            Cameras[0].Camera.CameraMovement.GoToHome();
        }

        private void button_SetHome_Click(object sender, EventArgs e)
        {
            if (Cameras[CurrentCamera].Camera == null) return;
            Cameras[0].Camera.CameraMovement.SetHome();
        }

        private void button_AddPreset_Click(object sender, EventArgs e)
        {

        }

        private void button_PresetMove_Click(object sender, EventArgs e)
        {
      
        }

        private void button_PresetDelete_Click(object sender, EventArgs e)
        {
         
        }

        private void button_ScanStart_Click(object sender, EventArgs e)
        {

        }

        private void button_ScanStop_Click(object sender, EventArgs e)
        {
            if (Cameras[CurrentCamera].Camera == null) return;
            Cameras[CurrentCamera].Camera.CameraMovement.StopMovement();
        }

        private void btn_compose_Click(object sender, EventArgs e)
        {

        }

        private void Button1_Click(object sender, EventArgs e)
        {

            this.Controls["micpic" + (CurrentCamera + 1).ToString()].Visible = true;
            Cameras[CurrentCamera].AudioOn();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            this.Controls["micpic" + (CurrentCamera + 1).ToString()].Visible = false;
            Cameras[CurrentCamera].AudioOff();
        }

        private void Button_ZoomIn_Click(object sender, EventArgs e)
        {

        }
    }
}
