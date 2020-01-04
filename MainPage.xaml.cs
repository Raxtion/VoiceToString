using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
//using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Globalization;
using Windows.Media.SpeechSynthesis;
using Windows.Media.SpeechRecognition;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;

using CLARE.Core;
using CLARE.Data;
using CLARE.Motion;

// 空白頁項目範本已記錄在 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x404

namespace VoiceToString_CSharp
{
    /// <summary>
    /// 可以在本身使用或巡覽至框架內的空白頁面。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MainPage g_MainPage;
        private CLARE.Data.IniFile g_IniFile;
        private CLARE.Core.RefCore g_RefCore;
        private Thread m_MotionThread;
        private MainThread g_MainThread;
        private SpeechSynthesizer synthesizer;
        private SpeechRecognizer speechRecognizer;
        private ObservableCollection<FontFamily> fonts = new ObservableCollection<FontFamily>();
        private static uint HResultRecognizerNotFound = 0x8004503a;
        private static uint HResultPrivacyStatementDeclined = 0x80045509;
        private bool isListening;
        private int m_nHeardCounter;
        private StringBuilder sbdrTextInp;
        private int m_nMode;

        public MainPage()
        {
            this.InitializeComponent();
            g_MainPage = this;

            g_IniFile = new IniFile(ref g_MainPage);
            g_RefCore = new RefCore();
            g_MainThread = new MainThread(ref g_RefCore, ref g_MainPage, ref g_IniFile);

            //Initial Thread
            m_MotionThread = new Thread(new ParameterizedThreadStart(g_MainThread.MainThreadExecute));
            m_MotionThread.TrySetApartmentState(ApartmentState.MTA);
            m_MotionThread.Start();

            //Software Version
            g_IniFile.ProcessVersion();

            //Check MicrophonePermission
            CheckMicrophonePermission();

            //Initial SpeechSynthesizer
            synthesizer = new SpeechSynthesizer();
            synthesizer.Voice = SpeechSynthesizer.AllVoices[2];         //1 for English girl, 2 for Chinese girl

            //Initial Button Status
            isListening = false;
            m_nHeardCounter = 0;
            m_nMode = 0;
            sbdrTextInp = new StringBuilder();

            //Initial listbox
            this.listboxHistory.Items.VectorChanged += Items_VectorChanged;
        }

        private void Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
        {
            if (fonts.Count != 0)
            {
                this.listboxHistory.ScrollIntoView(this.listboxHistory.Items[this.listboxHistory.Items.Count - 1]);
            }
        }

        private async void CheckMicrophonePermission()
        {
            // Prompt the user for permission to access the microphone. This request will only happen
            // once, it will not re-prompt if the user rejects the permission.
            bool permissionGained = await AudioCapturePermissions.RequestMicrophonePermission();
            if (permissionGained)
            {
                this.btnStartListening.IsEnabled = true;
                this.btnStartRecording.IsEnabled = true;
            }
            else
            {
                this.txtInput.Text = "Permission to access capture resources was not given by the user, reset the application setting in Settings->Privacy->Microphone.";
                this.btnStartListening.IsEnabled = false;
                this.btnStartRecording.IsEnabled = false;
            }
        }

        #region Listening
        private async Task InitializeRecognizer_Listening(Language recognizerLanguage, int nMode)
        {
            //如果物件已被初始化, 先移除再生成
            if (speechRecognizer != null)
            {
                speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed_Listening;
                speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated_Listening;
                speechRecognizer.HypothesisGenerated -= SpeechRecognizer_HypothesisGenerated_Listening;
                speechRecognizer.StateChanged -= SpeechRecognizer_StateChanged_Listening;

                this.speechRecognizer.Dispose();
                this.speechRecognizer = null;
            }
            this.speechRecognizer = new SpeechRecognizer(recognizerLanguage);

            //設定事件觸發時該用什麼模式
            m_nMode = nMode;

            //設定speechRecognizer的偵測模式
            var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
            speechRecognizer.Constraints.Add(dictationConstraint);
            SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();
            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                this.tbkStatus.Text = "Grammar Compilation Failed: " + result.Status.ToString();
                this.btnStartListening.IsEnabled = false;
            }

            //設定speechRecognizer的所有觸發事件
            speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed_Listening;
            speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated_Listening;
            speechRecognizer.HypothesisGenerated += SpeechRecognizer_HypothesisGenerated_Listening;
            speechRecognizer.StateChanged += SpeechRecognizer_StateChanged_Listening;
        }

        private async void ContinuousRecognitionSession_Completed_Listening(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            //speechRecognizer物件在辨識事件觸發結束之後會執行這段程式碼
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                if (args.Status == SpeechRecognitionResultStatus.TimeoutExceeded)
                {
                    //Timeout by Idle for a while
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>                                  //C# UWP 跨執行續操作 必須使用 Dispatcher.RunAsync()
                    {                                                                                               //該內部程式碼 會等外部執行續暫止才會執行 完成安全的非同步操作 自續則是直接跨過
                        this.tbkStatus.Text = "Automatic Time Out of Dictation";
                        this.txtInput.Text = this.tbkStatus.Text;

                        sbdrTextInp.Clear();
                        isListening = false;
                        this.btnStartRecording.IsEnabled = true;
                        this.btnStartListening.IsEnabled = true;
                        this.btnStartListening.Content = "Start Listening";
                        this.btnStartRecording.Content = "Start Recording";
                    });
                }
                else
                {
                    //User Send App To Background
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        this.tbkStatus.Text = "Continuous Recognition Completed: " + args.Status.ToString();

                        sbdrTextInp.Clear();
                        isListening = false;
                        this.btnStartRecording.IsEnabled = true;
                        this.btnStartListening.IsEnabled = true;
                        this.btnStartListening.Content = "Start Listening";
                        this.btnStartRecording.Content = "Start Recording";
                    });
                }
            }
        }

        private async void ContinuousRecognitionSession_ResultGenerated_Listening(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            //speechRecognizer物件在辨識事件完成後 回傳結果時觸發的事件 
            if (m_nMode == 0)
            {
                //0 模式為Listening
                if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                    args.Result.Confidence == SpeechRecognitionConfidence.High)
                {
                    //將結果暫存器
                    sbdrTextInp.Append(args.Result.Text + " ");

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        //同步外部畫面
                        this.txtInput.Text = sbdrTextInp.ToString();
                    });
                }
                else
                {
                    //辨識品質不高的 結果用提示顯示 結果不保留
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        this.txtInput.Text = sbdrTextInp.ToString();
                        string discardedText = args.Result.Text;
                        if (!string.IsNullOrEmpty(discardedText))
                        {
                            discardedText = discardedText.Length <= 25 ? discardedText : (discardedText.Substring(0, 25) + "...");

                            this.tbkStatus.Text = "Discarded due to low/rejected Confidence: " + discardedText;
                            this.tbkStatus.Visibility = Visibility.Visible;
                        }
                    });
                }
            }
            else
            {
                //0 模式為Recording
                if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                    args.Result.Confidence == SpeechRecognitionConfidence.High)
                {
                    //將結果暫存器
                    sbdrTextInp.Append(args.Result.Text + " ");

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        this.tbkStatus.Text = sbdrTextInp.ToString();

                        if (this.tbkStatus.Text.ToLower().Contains(this.txtInput.Text.ToLower()))
                        {
                            //觸發計數器
                            m_nHeardCounter++;

                            string strResult = string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})", sbdrTextInp.ToString(), this.txtInput.Text, "");
                            string strDateTime = System.DateTime.Now.ToString("[yyyy:MM:dd HH:mm:ss] ");
                            fonts.Add(new FontFamily(strDateTime + strResult + " (" + m_nHeardCounter.ToString() + ")"));

                            //Log to File m_listLog
                            g_RefCore.m_listLog.Add(strDateTime + strResult + " (" + m_nHeardCounter.ToString() + ")");
                        }

                    });
                }
                else
                {
                    //辨識品質不高的 直接Pass
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {

                    });
                }
            }
        }

        private async void SpeechRecognizer_HypothesisGenerated_Listening(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            //speechRecognizer物件在語音辨識中 回傳預測結果時會觸發執行這段程式碼
            string hypothesis = args.Hypothesis.Text;

            if (m_nMode == 0)
            {
                //0 Listening 模式
                string textboxContent = sbdrTextInp.ToString() + " " + hypothesis + " ...";
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //同步外部畫面
                    this.txtInput.Text = textboxContent;
                });
            }
            else
            {
                //1 Recording 模式
                string textboxContent = sbdrTextInp.ToString() + " " + hypothesis + " ...";
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //同步外部畫面
                    this.tbkStatus.Text = textboxContent;
                });
            }
            
        }
        
        private async void SpeechRecognizer_StateChanged_Listening(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            //speechRecognizer物件在狀態改變之後會觸發執行這段程式碼
            if (args.State == SpeechRecognizerState.SoundStarted)
            {
                //在語音辨識開始時
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //清空暫存區
                    this.tbkStatus.Text = "";
                    sbdrTextInp.Clear();
                });
            }
        }
        #endregion Listening

        private void media_MediaEnded(object sender, RoutedEventArgs e)
        {
            this.btnStartReading.Content = "Start Reading";
            this.btnStartListening.Content = "Start Listening";
            this.btnStartRecording.Content = "Start Recording";
        }

        private async void btnStartReading_Click(object sender, RoutedEventArgs e)
        {
            if (media.CurrentState == MediaElementState.Playing)
            {
                media.Stop();
                this.btnStartReading.Content = "Start Reading";
            }
            else
            {
                string text = txtInput.Text;
                if (!String.IsNullOrEmpty(text))
                {
                    this.btnStartReading.Content = "Stop";

                    try
                    {
                        //輸入txtInput的內容 產生SpeechSynthesisStream物件
                        SpeechSynthesisStream synthesisStream = await synthesizer.SynthesizeTextToStreamAsync(text);

                        //將SpeechSynthesisStream物件 丟給media 以撥出聲音
                        media.AutoPlay = true;
                        media.SetSource(synthesisStream, synthesisStream.ContentType);
                        media.Play();
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        this.btnStartReading.Content = "Start Reading";
                        this.btnStartReading.IsEnabled = false;
                        txtInput.IsEnabled = false;
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Media player components unavailable");
                        await messageDialog.ShowAsync();
                    }
                    catch (Exception)
                    {
                        this.btnStartReading.Content = "Start Reading";
                        media.AutoPlay = false;
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Unable to synthesize text");
                        await messageDialog.ShowAsync();
                    }
                }
            }
        }

        private async void btnStartListening_Click(object sender, RoutedEventArgs e)
        {
            this.btnStartRecording.IsEnabled = false;
            this.btnStartListening.IsEnabled = false;
            if (isListening == false)
            {
                //生成speechRecognizer物件
                await InitializeRecognizer_Listening(SpeechRecognizer.SupportedGrammarLanguages[0], 0);              //Only support 0 for En-US

                if (speechRecognizer.State == SpeechRecognizerState.Idle)
                {
                    //若speechRecognizer不在運行中
                    this.btnStartListening.Content = "Stop Listening";

                    try
                    {
                        //開始執行 speechRecognizer
                        isListening = true;
                        sbdrTextInp.Clear();
                        await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        if ((uint)ex.HResult == HResultPrivacyStatementDeclined)
                        {
                            //電腦硬體設置 錯誤 
                            //需要提供使用者授權
                        }
                        else
                        {
                            var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                            await messageDialog.ShowAsync();
                        }

                        isListening = false;
                        this.tbkStatus.Text = "Idle";

                    }
                }
            }
            else
            {
                isListening = false;

                this.btnStartListening.Content = "Start Listening";

                if (speechRecognizer.State != SpeechRecognizerState.Idle)
                {
                    //若speechRecognizer在運行中
                    try
                    {
                        //中斷 speechRecognizer物件
                        await speechRecognizer.ContinuousRecognitionSession.StopAsync();

                        //輸出最終結果
                        this.txtInput.Text = sbdrTextInp.ToString();
                        this.btnStartRecording.IsEnabled = true;
                    }
                    catch (Exception exception)
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog(exception.Message, "Exception");
                        await messageDialog.ShowAsync();
                    }
                }
            }
            this.btnStartListening.IsEnabled = true;
        }

        private async void btnStartRecording_Click(object sender, RoutedEventArgs e)
        {
            this.btnStartRecording.IsEnabled = false;
            this.btnStartListening.IsEnabled = false;
            if (isListening == false)
            {
                //生成speechRecognizer物件
                await InitializeRecognizer_Listening(SpeechRecognizer.SupportedGrammarLanguages[0], 1);              //Only support 0 for En-US

                if (speechRecognizer.State == SpeechRecognizerState.Idle)
                {
                    try
                    {
                        this.btnStartRecording.Content = "Stop Recording";
                        isListening = true;
                        m_nHeardCounter = 0;
                        sbdrTextInp.Clear();
                        //開始執行 speechRecognizer
                        await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                        await messageDialog.ShowAsync();
                    }
                }
            }
            else
            {
                isListening = false;
                this.btnStartRecording.Content = "Start Recording";

                //若speechRecognizer在運行中
                if (speechRecognizer.State != SpeechRecognizerState.Idle)
                {
                    try
                    {
                        //中斷 speechRecognizer物件
                        await speechRecognizer.ContinuousRecognitionSession.CancelAsync();

                        this.btnStartListening.IsEnabled = true;
                    }
                    catch (Exception ex)
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                        await messageDialog.ShowAsync();
                    }
                }
            }

            this.btnStartRecording.IsEnabled = true;
        }

        private async void btnOpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            string strLogData = "";
            foreach (FontFamily Item in listboxHistory.Items)
            {
                strLogData += Item.Source.ToString() + "\r\n";
            }

            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            fileSavePicker.FileTypeChoices.Add("Text", new List<string>() { ".txt" });
            fileSavePicker.SuggestedFileName = "NewLog.txt";
            StorageFile file = await fileSavePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                await FileIO.WriteTextAsync(file, strLogData);
                // Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == FileUpdateStatus.Complete)
                {
                    tbkStatus.Text = "File " + file.Name + " was saved.";
                }
                else
                {
                    tbkStatus.Text = "File " + file.Name + " couldn't be saved.";
                }
            }
            else
            {
                tbkStatus.Text = "Operation cancelled.";
            }
        }
    }


    public class AudioCapturePermissions
    {
        // If no recording device is attached, attempting to get access to audio capture devices will throw 
        // a System.Exception object, with this HResult set.
        private static int NoCaptureDevicesHResult = -1072845856;

        /// <summary>
        /// On desktop/tablet systems, users are prompted to give permission to use capture devices on a 
        /// per-app basis. Along with declaring the microphone DeviceCapability in the package manifest,
        /// this method tests the privacy setting for microphone access for this application.
        /// Note that this only checks the Settings->Privacy->Microphone setting, it does not handle
        /// the Cortana/Dictation privacy check, however (Under Settings->Privacy->Speech, Inking and Typing).
        /// 
        /// Developers should ideally perform a check like this every time their app gains focus, in order to 
        /// check if the user has changed the setting while the app was suspended or not in focus.
        /// </summary>
        /// <returns>true if the microphone can be accessed without any permissions problems.</returns>
        public async static Task<bool> RequestMicrophonePermission()
        {
            try
            {
                // Request access to the microphone only, to limit the number of capabilities we need
                // to request in the package manifest.
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
                settings.MediaCategory = MediaCategory.Speech;
                MediaCapture capture = new MediaCapture();

                await capture.InitializeAsync(settings);
            }
            catch (TypeLoadException)
            {
                // On SKUs without media player (eg, the N SKUs), we may not have access to the Windows.Media.Capture
                // namespace unless the media player pack is installed. Handle this gracefully.
                var messageDialog = new Windows.UI.Popups.MessageDialog("Media player components are unavailable.");
                await messageDialog.ShowAsync();
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                // The user has turned off access to the microphone. If this occurs, we should show an error, or disable
                // functionality within the app to ensure that further exceptions aren't generated when 
                // recognition is attempted.
                return false;
            }
            catch (Exception exception)
            {
                // This can be replicated by using remote desktop to a system, but not redirecting the microphone input.
                // Can also occur if using the virtual machine console tool to access a VM instead of using remote desktop.
                if (exception.HResult == NoCaptureDevicesHResult)
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog("No Audio Capture devices are present on this system.");
                    await messageDialog.ShowAsync();
                    return false;
                }
                else
                {
                    throw;
                }
            }
            return true;
        }
    }
}
