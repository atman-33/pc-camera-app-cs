﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using AForge.Video;             // AForge.NETライブラリから読込
using AForge.Video.DirectShow;  // AForge.NETライブラリから読込 

using Original;

namespace PcCameraApp
{
    public partial class MainForm : Form
    {

        // 画像処理モード
        readonly string[] IMAGE_MODE = { "なし", "グレー", "顔認識" };
        private string mode;    // 現在の画像処理モード

        public MainForm()
        {
            InitializeComponent();
        }

        // フィールド
        public bool DeviceExist = false;                // デバイス有無
        public FilterInfoCollection videoDevices;       // カメラデバイスの一覧
        public VideoCaptureDevice videoSource = null;   // カメラデバイスから取得した映像

        // Loadイベント（Formの立ち上げ時に実行）
        private void Form1_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("Formのロード開始");
            this.getCameraInfo();
        }

        // カメラ情報の取得
        public void getCameraInfo()
        {
            try
            {
                // 端末で認識しているカメラデバイスの一覧を取得
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                comboBoxCameraType.Items.Clear();

                if (videoDevices.Count == 0)
                    throw new ApplicationException();

                foreach (FilterInfo device in videoDevices)
                {
                    // カメラデバイスの一覧をコンボボックスに追加
                    comboBoxCameraType.Items.Add(device.Name);
                    comboBoxCameraType.SelectedIndex = 0;
                    DeviceExist = true;
                }
            }
            catch (ApplicationException)
            {
                DeviceExist = false;
                comboBoxCameraType.Items.Add("Deviceが存在していません。");
            }

            comboBoxMode.Items.Clear();

            // 画像処理モードの追加
            foreach (string mode in IMAGE_MODE)
            {
                comboBoxMode.Items.Add(mode);
            }
        }

        // 開始or停止ボタン
        private void button1_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("ボタンクリック");
            mode = comboBoxMode.Text;

            if (buttonStartStop.Text == "開始")
            {

                if (DeviceExist)
                {
                    videoSource = new VideoCaptureDevice(videoDevices[comboBoxCameraType.SelectedIndex].MonikerString);
                    videoSource.NewFrame += new NewFrameEventHandler(videoRendering);
                    this.CloseVideoSource();

                    videoSource.Start();

                    buttonStartStop.Text = "停止";
                    timer1.Enabled = true;
                }
                else
                {
                    labelFps.Text = "デバイスが存在していません。";
                }
            }
            else
            {
                if (videoSource.IsRunning)
                {
                    timer1.Enabled = false;
                    this.CloseVideoSource();
                    labelFps.Text = "停止中";
                    buttonStartStop.Text = "開始";

                }
            }
        }
        // 描画処理
        private void videoRendering(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap img = (Bitmap)eventArgs.Frame.Clone();

            Debug.WriteLine(DateTime.Now + ":" + "描画更新");
            Debug.WriteLine(mode);

            try
            {
                switch (mode)
                {
                    case "なし":
                        pictureBoxCamera.Image = img;
                        break;

                    case "グレー":
                        using (OpenCVSharpBitmap bitmap = new OpenCVSharpBitmap(img))
                        {
                            pictureBoxCamera.Image = bitmap.toGray();
                        }
                        break;

                    case "顔認識":
                        using (OpenCVSharpBitmap bitmap = new OpenCVSharpBitmap(img))
                        {
                            pictureBoxCamera.Image = bitmap.addFaceRect(@"C:\Users\gpbjk\source\repos\PcCameraApp\PcCameraApp\haarcascade_frontalface_default.xml");
                        }
                        break;

                    default:
                        pictureBoxCamera.Image = img;
                        break;
                }
            }
            catch
            {
                pictureBoxCamera.Image = img;
            }
        }
        // 停止の初期化
        private void CloseVideoSource()
        {
            if (!(videoSource == null))
                if (videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                    videoSource = null;
                }
        }
        // フレームレートの取得
        private void timer1_Tick(object sender, EventArgs e)
        {
            labelFps.Text = videoSource.FramesReceived.ToString() + "FPS";
        }
    }
}
