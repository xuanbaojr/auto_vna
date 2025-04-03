using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using Avalonia.Threading;
using MyASCS.Services.Implementations;

namespace MyASCS.ViewModels
{
    public class RtspStreamViewModel : ReactiveObject, IDisposable
    {
        private readonly RtspStreamService _rtspStreamService;
        private string _userMessage;
        private bool _isMessageVisible;
        private string _countdownText;
        private bool _isCountdownVisible;
        private bool _isFaceIdMode;
        private bool _isHandsMode; // Thêm thuộc tính mới cho chế độ Hands

        public string UserMessage
        {
            get => _userMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _userMessage, value);
                IsMessageVisible = !string.IsNullOrEmpty(value); // Hiển thị thông báo khi có giá trị
            }
        }

        public bool IsMessageVisible
        {
            get => _isMessageVisible;
            private set => this.RaiseAndSetIfChanged(ref _isMessageVisible, value);
        }

        public string CountdownText
        {
            get => _countdownText;
            set => this.RaiseAndSetIfChanged(ref _countdownText, value);
        }

        public bool IsCountdownVisible
        {
            get => _isCountdownVisible;
            set => this.RaiseAndSetIfChanged(ref _isCountdownVisible, value);
        }
        
        public bool IsFaceIdMode
        {
            get => _isFaceIdMode;
            set => this.RaiseAndSetIfChanged(ref _isFaceIdMode, value);
        }
        
        // Thêm thuộc tính IsHandsMode
        public bool IsHandsMode
        {
            get => _isHandsMode;
            set => this.RaiseAndSetIfChanged(ref _isHandsMode, value);
        }
        
        private readonly Bitmap?[] _cameraImages = new Bitmap?[4];
        public Bitmap? Camera1Image { get => _cameraImages[0]; set => this.RaiseAndSetIfChanged(ref _cameraImages[0], value); }
        public Bitmap? Camera2Image { get => _cameraImages[1]; set => this.RaiseAndSetIfChanged(ref _cameraImages[1], value); }
        public Bitmap? Camera3Image { get => _cameraImages[2]; set => this.RaiseAndSetIfChanged(ref _cameraImages[2], value); }
        public Bitmap? Camera4Image { get => _cameraImages[3]; set => this.RaiseAndSetIfChanged(ref _cameraImages[3], value); }

        public RtspStreamViewModel()
        {
            _rtspStreamService = new RtspStreamService();
            _rtspStreamService.FrameCaptured += UpdateFrame;
            _rtspStreamService.InstructionReceived += OnInstructionReceived;
            _rtspStreamService.CountdownUpdated += OnCountdownUpdated;
            
            // Đăng ký event handler cho FaceID
            _rtspStreamService.FaceIdModeChanged += OnFaceIdModeChanged;
            
            // Thêm event handler mới cho chế độ Hands
            _rtspStreamService.HandsModeChanged += OnHandsModeChanged;
        }

        private void UpdateFrame(int cameraIndex, Bitmap? bitmap)
        {
            if (bitmap == null) return;

            _cameraImages[cameraIndex] = bitmap;
            this.RaisePropertyChanged($"Camera{cameraIndex + 1}Image");
        }

        private void OnInstructionReceived(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                UserMessage = message;
            });
        }
        
        private void OnCountdownUpdated(int secondsRemaining)
        {
            Dispatcher.UIThread.Post(() =>
            {
                CountdownText = secondsRemaining.ToString();
                IsCountdownVisible = true;
                
                if (secondsRemaining <= 0)
                {
                    // Hiển thị "Đang chụp..." khi đếm ngược kết thúc
                    CountdownText = "Đang chụp...";
                    
                    // Sau 1 giây, ẩn đếm ngược
                    var timer = new System.Timers.Timer(1000);
                    timer.Elapsed += (sender, e) =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            IsCountdownVisible = false;
                            ((System.Timers.Timer)sender).Dispose();
                        });
                    };
                    timer.AutoReset = false;
                    timer.Start();
                }
            });
        }
        
        private void OnFaceIdModeChanged(bool isActive)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsFaceIdMode = isActive;
            });
        }
        
        // Thêm handler mới cho chế độ Hands
        private void OnHandsModeChanged(bool isActive)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsHandsMode = isActive;
            });
        }

        public void Dispose()
        {
            _rtspStreamService.Dispose();
        }
    }
}
