using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Grpc.Net.Client;
using MyASCS.Services.Interfaces;
using Google.Protobuf;
using Instruction;

namespace MyASCS.Services.Implementations
{
    public class RtspStreamService : IDisposable, IRtspStreamService
    {
        private readonly CancellationTokenSource _cts = new();
        private CancellationTokenSource? _framesSendingCts;
        private bool _isSendingFrames = true;

        private readonly string[] _rtspUrls =
        [
            "rtsp://admin:FFWNQY@192.168.0.118/camera/h264/ch1/main/av_stream",
            "rtsp://admin:FFWNQY@192.168.0.118/camera/h264/ch1/main/av_stream",
            "rtsp://admin:FFWNQY@192.168.0.118/camera/h264/ch1/main/av_stream",
            "rtsp://admin:FFWNQY@192.168.0.118/camera/h264/ch1/main/av_stream",
        ];

        private readonly Process?[] _ffmpegProcesses = new Process?[4];

        // Thứ tự camera dựa trên tầm nhìn
        // Camera 1: Khuôn mặt từ phần vai trở lên
        // Camera 2: Chân dung (quần, áo) 
        // Camera 3: Giày và một phần quần
        // Camera 4: Bàn tay úp, cổ tay
        private readonly string[] _positions = ["face", "portrait", "shoes", "hands"];

        // Các hướng cho FaceID - Hiển thị tiếng Việt, lưu file tiếng Anh
        private readonly string[] _faceDirectionsDisplay = ["trên", "dưới", "trái", "phải"];
        private readonly string[] _faceDirectionsFolder = ["front", "left", "right", "up"];

        // Các hướng chụp người - Hiển thị tiếng Việt, lưu file tiếng Anh
        private readonly string[] _bodyDirectionsDisplay = ["trái", "sau", "phải"];
        private readonly string[] _bodyDirectionsFolder = ["left", "back", "right"];

        private readonly string _sessionId = Guid.NewGuid().ToString();
        private readonly GrpcChannel _channel;
        private readonly GRPCService.GRPCServiceClient _grpcClient;
        private int _imageIndex = 1;
        private int _faceIdImageIndex = 1;
        private int _directionViewImageIndex = 1;

        public event Action<int, Bitmap?>? FrameCaptured;
        public event Action<string>? InstructionReceived;
        public event Action<int>? CountdownUpdated;
        public event Action<bool>? FaceIdModeChanged; // Event cho chế độ FaceID
        public event Action<bool>? HandsModeChanged; // Event cho chế độ Hands

        public RtspStreamService()
        {
            _channel = GrpcChannel.ForAddress("http://localhost:50051");
            _grpcClient = new GRPCService.GRPCServiceClient(_channel);

            for (var i = 0; i < 4; i++)
            {
                var cameraIndex = i;
                Task.Run(() => StartStreaming(cameraIndex));
            }

            StartFramesSending();
        }

        private void StartFramesSending()
        {
            _framesSendingCts = new CancellationTokenSource();
            _isSendingFrames = true;

            // Tạo task mới để gửi frames đến AI
            Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested && !_framesSendingCts.Token.IsCancellationRequested)
                    {
                        await SendFramesToAi("is_check");
                        await Task.Delay(10, _framesSendingCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Task đã bị hủy, điều này là bình thường khi dừng gửi frames
                    Console.WriteLine("🛑 Frames sending task canceled");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ AI Task Crashed: {ex.Message}");
                }
            }, _framesSendingCts.Token);
        }

        private void StopFramesSending()
        {
            if (_isSendingFrames && _framesSendingCts != null)
            {
                _framesSendingCts.Cancel();
                _isSendingFrames = false;
                Console.WriteLine("🛑 Stopped sending frames to AI");
            }
        }

        private void StartStreaming(int cameraIndex)
        {
            try
            {
                var sessionPath = Path.Combine(AppContext.BaseDirectory, "sessions", _sessionId);
                Directory.CreateDirectory(sessionPath);

                var outputPath = Path.Combine(sessionPath, $"{_positions[cameraIndex]}_frame.jpg");

                _ffmpegProcesses[cameraIndex] = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments =
                            $"-rtsp_transport tcp -i {_rtspUrls[cameraIndex]} -vf scale=640:480 -r 30 -q:v 5 -update 1 -y \"{outputPath}\" -fflags nobuffer -flags low_delay -strict experimental",
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };
                _ffmpegProcesses[cameraIndex]?.Start();

                Task.Run(async () =>
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(100);
                        var bitmap = await CaptureFrame(outputPath);
                        if (bitmap != null)
                        {
                            FrameCaptured?.Invoke(cameraIndex, bitmap);
                        }
                    }
                }, _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error starting FFmpeg for Camera {cameraIndex + 1}: {ex.Message}");
            }
        }

        // Thêm method mới để gửi frame và trả về phản hồi từ AI
        private async Task<string> SendFramesToAiAndGetResponse(string typeInstruction)
        {
            try
            {
                var request = new InstructionRequest();
                for (var i = 0; i < 4; i++)
                {
                    var framePath = Path.Combine(AppContext.BaseDirectory, "sessions", _sessionId,
                        $"{_positions[i]}_frame.jpg");
                    if (File.Exists(framePath))
                    {
                        var frameData = await File.ReadAllBytesAsync(framePath);
                        switch (i)
                        {
                            case 0: request.Frame1 = ByteString.CopyFrom(frameData); break;
                            case 1: request.Frame2 = ByteString.CopyFrom(frameData); break;
                            case 2: request.Frame3 = ByteString.CopyFrom(frameData); break;
                            case 3: request.Frame4 = ByteString.CopyFrom(frameData); break;
                        }
                    }
                }

                request.TypeInstruction = typeInstruction;

                var response = await _grpcClient.GetInstructionAsync(request);
                Console.WriteLine($"🎯 AI Instruction ({typeInstruction}): {response.InstructionStr}");

                return response.InstructionStr;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ gRPC AI Detection Error: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task SendFramesToAi(string typeInstruction = "is_check")
        {
            try
            {
                var request = new InstructionRequest();
                for (var i = 0; i < 4; i++)
                {
                    var framePath = Path.Combine(AppContext.BaseDirectory, "sessions", _sessionId,
                        $"{_positions[i]}_frame.jpg");
                    if (File.Exists(framePath))
                    {
                        var frameData = await File.ReadAllBytesAsync(framePath);
                        switch (i)
                        {
                            case 0: request.Frame1 = ByteString.CopyFrom(frameData); break;
                            case 1: request.Frame2 = ByteString.CopyFrom(frameData); break;
                            case 2: request.Frame3 = ByteString.CopyFrom(frameData); break;
                            case 3: request.Frame4 = ByteString.CopyFrom(frameData); break;
                        }
                    }
                }

                request.TypeInstruction = typeInstruction;

                var response = await _grpcClient.GetInstructionAsync(request);
                Console.WriteLine($"🎯 AI Instruction: {response.InstructionStr}");

                if (response.InstructionStr == "face:true")
                {
                    await HandleFaceDetected();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ gRPC AI Detection Error: {ex.Message}");
            }
        }

        private async Task HandleFaceDetected()
        {
            try
            {
                // 1. Dừng gửi frame
                StopFramesSending();

                // 2. Thông báo đến tiếp viên
                InstructionReceived?.Invoke("Hãy đứng thẳng, giữ yên đầu, cười lên!");

                // 3. Đếm ngược 3s
                for (int i = 3; i > 0; i--)
                {
                    CountdownUpdated?.Invoke(i);
                    await Task.Delay(1000);
                }

                // Thông báo đang chụp
                CountdownUpdated?.Invoke(0);
                await Task.Delay(500);

                // 4. Chụp và lưu ảnh
                var saveSuccess = SaveCapturedFrames();

                // Thông báo kết quả
                if (saveSuccess)
                {
                    InstructionReceived?.Invoke("Chụp ảnh thành công ✅");
                    await Task.Delay(2000);
                }
                else
                {
                    InstructionReceived?.Invoke("Lưu ảnh thất bại ❌");
                    await Task.Delay(2000);
                }

                // 5. Bắt đầu quá trình FaceID ngay lập tức
                // Kích hoạt chế độ FaceID
                FaceIdModeChanged?.Invoke(true);
                await HandleFaceIdProcess();

                // Lưu ý: Không cần khởi động lại việc gửi frame ở đây
                // vì HandleFaceIdProcess sẽ tự khởi động lại khi hoàn thành
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in HandleFaceDetected: {ex.Message}");
                if (!_isSendingFrames)
                {
                    StartFramesSending();
                }
            }
        }

        private async Task HandleFaceIdProcess()
        {
            try
            {
                // 1. Dừng gửi frame
                StopFramesSending();

                // 2. Thông báo tiếp viên không cười nữa, giữ yên vị trí đầu
                InstructionReceived?.Invoke("Vui lòng không cười, giữ yên vị trí đầu để chụp ảnh nhận diện");
                await Task.Delay(2000);

                // 3. Đếm ngược trước khi chụp ảnh nhìn thẳng
                for (int i = 3; i > 0; i--)
                {
                    CountdownUpdated?.Invoke(i);
                    await Task.Delay(1000);
                }

                // 4. Chụp ảnh nhìn thẳng (hướng đầu tiên)
                CountdownUpdated?.Invoke(0);
                await Task.Delay(500);

                var saveSuccess = SaveFaceIdFrame(0); // 0 = hướng thẳng

                if (saveSuccess)
                {
                    InstructionReceived?.Invoke("Đã chụp ảnh nhìn thẳng");
                    await Task.Delay(1000);
                }

                // 5. Hướng dẫn tiếp viên quay đầu theo các hướng khác nhau
                for (var direction = 0; direction < _faceDirectionsDisplay.Length; direction++)
                {
                    switch (direction)
                    {
                        // Hướng dẫn quay đầu - Hiển thị tên tiếng Việt
                        case 0:
                            InstructionReceived?.Invoke($"Vui lòng quay đầu lên {_faceDirectionsDisplay[direction]}");
                            break;
                        case 1:
                            InstructionReceived?.Invoke($"Vui lòng quay xuống {_faceDirectionsDisplay[direction]}");
                            break;
                        default:
                            InstructionReceived?.Invoke($"Vui lòng quay đầu sang {_faceDirectionsDisplay[direction]}");
                            break;
                    }
                    await Task.Delay(2000); // Cho thời gian tiếp viên quay đầu

                    // Đếm ngược
                    CountdownUpdated?.Invoke(1);
                    await Task.Delay(1000);
                    CountdownUpdated?.Invoke(0);

                    // Chụp ảnh
                    saveSuccess = SaveFaceIdFrame(direction);

                    if (saveSuccess)
                    {
                        InstructionReceived?.Invoke($"Đã chụp ảnh nhìn {_faceDirectionsDisplay[direction]}");
                        await Task.Delay(1000);
                    }
                }

                // 6. Thông báo đã chụp xong
                InstructionReceived?.Invoke("Đã chụp xong tất cả các góc nhìn FaceID ✅");
                await Task.Delay(2000);

                // 7. Vô hiệu hóa chế độ FaceID
                FaceIdModeChanged?.Invoke(false);

                // 8. Bắt đầu quy trình chụp ảnh toàn thân 4 hướng
                await CaptureAllBodyDirections();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in HandleFaceIdProcess: {ex.Message}");
                FaceIdModeChanged?.Invoke(false); // Đảm bảo tắt chế độ FaceID trong trường hợp có lỗi

                if (!_isSendingFrames)
                {
                    StartFramesSending();
                }
            }
        }

        private async Task CaptureAllBodyDirections()
        {
            try
            {
                // Thông báo bắt đầu chụp ảnh các hướng
                InstructionReceived?.Invoke("Bắt đầu chụp ảnh toàn thân các góc độ");
                await Task.Delay(2000);

                // Chụp ảnh hướng trái (tiếp viên quay người sang phải 90 độ)
                await CaptureBodyDirection(0); // 0 = hướng trái

                // Chụp ảnh hướng sau (tiếp viên quay người thêm 90 độ, lưng về phía camera)
                await CaptureBodyDirection(1); // 1 = hướng sau

                // Chụp ảnh hướng phải (tiếp viên quay người thêm 90 độ)
                await CaptureBodyDirection(2); // 2 = hướng phải

                // Thông báo hoàn thành quá trình chụp ảnh
                InstructionReceived?.Invoke("Đã chụp xong tất cả các hướng! Cảm ơn bạn đã hợp tác ✅");
                await Task.Delay(3000);

                // Thêm mới: Thực hiện chụp ảnh tay
                await CaptureHandsImages();

                // Khởi động lại việc gửi frame với type_instruction = "clothes"
                _framesSendingCts = new CancellationTokenSource();
                _isSendingFrames = true;

                await Task.Run(async () =>
                {
                    try
                    {
                        while (!_cts.Token.IsCancellationRequested && !_framesSendingCts.Token.IsCancellationRequested)
                        {
                            await SendFramesToAi("clothes");
                            await Task.Delay(10, _framesSendingCts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("🛑 Frames sending task canceled");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ AI Task Crashed: {ex.Message}");
                    }
                }, _framesSendingCts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CaptureAllBodyDirections: {ex.Message}");

                if (!_isSendingFrames)
                {
                    StartFramesSending();
                }
            }
        }

        // Thêm method mới để thực hiện chụp ảnh tay
        private async Task CaptureHandsImages()
        {
            try
            {
                // Kích hoạt chế độ Hands để phóng to camera 4
                HandsModeChanged?.Invoke(true);
                
                // 1. Thông báo tiếp viên đặt tay lên giá
                InstructionReceived?.Invoke("Vui lòng quay phải và đặt tay lên giá đo");

                // Tạo biến theo dõi trạng thái đặt tay
                bool handPositionCorrect = false;

                // Kiểm tra vị trí tay cho đến khi đúng
                while (!handPositionCorrect)
                {
                    // Gửi frame lên service với type_instruction="hand"
                    var response = await SendFramesToAiAndGetResponse("hand");

                    // Kiểm tra phản hồi từ AI
                    if (response == "hand:true")
                    {
                        // Đã đặt tay đúng
                        InstructionReceived?.Invoke("Tay đã được đặt đúng vị trí ✅");
                        handPositionCorrect = true;
                        await Task.Delay(1000);
                    }
                    else
                    {
                        // Chưa đặt tay đúng
                        InstructionReceived?.Invoke("Bạn chưa đặt tay chính xác. Vui lòng đặt tay lên giá");
                        await Task.Delay(2000); // Chờ 2 giây trước khi kiểm tra lại
                    }
                }

                // 2. Thông báo chuẩn bị chụp
                InstructionReceived?.Invoke("Chuẩn bị chụp ảnh tay, vui lòng giữ yên tay");

                // 3. Đếm ngược 3s
                for (int i = 3; i > 0; i--)
                {
                    CountdownUpdated?.Invoke(i);
                    await Task.Delay(1000);
                }

                // 4. Thông báo đang chụp
                CountdownUpdated?.Invoke(0);
                await Task.Delay(500);

                // 5. Chụp và lưu ảnh (chủ yếu lấy ảnh từ camera 3 - hands)
                var saveSuccess = SaveHandsImages();

                // 6. Thông báo kết quả
                if (saveSuccess)
                {
                    InstructionReceived?.Invoke("Đã chụp xong ảnh tay ✅");
                    await Task.Delay(2000);

                    // 7. Thông báo hoàn thành toàn bộ quy trình
                    InstructionReceived?.Invoke("Đã hoàn thành toàn bộ quy trình, yêu cầu tiếp viên ra ngoài!");
                    await Task.Delay(3000);
                }
                else
                {
                    InstructionReceived?.Invoke("Lưu ảnh tay thất bại ❌");
                    await Task.Delay(2000);
                }
                
                // Vô hiệu hóa chế độ Hands khi hoàn thành
                HandsModeChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CaptureHandsImages: {ex.Message}");
                // Đảm bảo tắt chế độ Hands trong trường hợp có lỗi
                HandsModeChanged?.Invoke(false);
            }
        }

        private async Task CaptureBodyDirection(int directionIndex)
        {
            string turnInstruction;
            if (directionIndex == 0)
            {
                // Hướng dẫn quay sang trái
                turnInstruction = "Vui lòng quay người sang phải 90 độ so với vị trí hiện tại";
            }
            else if (directionIndex == 1)
            {
                // Hướng dẫn quay lưng về phía camera
                turnInstruction = "Vui lòng quay thêm 90 độ để lưng hướng về phía camera";
            }
            else
            {
                // Hướng dẫn quay sang phải (hoàn tất vòng quay)
                turnInstruction = "Vui lòng quay thêm 90 độ nữa để hoàn tất vòng quay";
            }

            // Thông báo hướng dẫn
            InstructionReceived?.Invoke(turnInstruction);

            // Chờ 3 giây để tiếp viên di chuyển
            await Task.Delay(3000);

            // Thông báo chuẩn bị chụp ảnh hướng hiện tại - Hiển thị tên tiếng Việt
            InstructionReceived?.Invoke(
                $"Chuẩn bị chụp ảnh hướng {_bodyDirectionsDisplay[directionIndex]}, vui lòng đứng yên");

            // Đếm ngược 1s
            CountdownUpdated?.Invoke(1);
            await Task.Delay(1000);

            // Thông báo đang chụp
            CountdownUpdated?.Invoke(0);
            await Task.Delay(500);

            // Chụp và lưu ảnh
            var saveSuccess = SaveDirectionViewFrames(directionIndex);

            // Thông báo kết quả - Hiển thị tên tiếng Việt
            if (saveSuccess)
            {
                InstructionReceived?.Invoke($"Đã chụp xong ảnh hướng {_bodyDirectionsDisplay[directionIndex]} ✅");
                await Task.Delay(2000);
            }
            else
            {
                InstructionReceived?.Invoke($"Lưu ảnh hướng {_bodyDirectionsDisplay[directionIndex]} thất bại ❌");
                await Task.Delay(2000);
            }
        }

        private static async Task<Bitmap?> CaptureFrame(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                await using var stream = File.OpenRead(filePath);
                return new Bitmap(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading frame: {ex.Message}");
                return null;
            }
        }

        private bool SaveCapturedFrames()
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                // Tạo thư mục upload nếu chưa tồn tại
                var uploadPath = Path.Combine(AppContext.BaseDirectory, "upload", _sessionId);
                Directory.CreateDirectory(uploadPath);

                for (var i = 0; i < 4; i++)
                {
                    var framePath = Path.Combine(AppContext.BaseDirectory, "sessions", _sessionId,
                        $"{_positions[i]}_frame.jpg");
                    // Tạo tên file mới theo định dạng: <position><imageIndex>_<timestamp>.png
                    var capturePath = Path.Combine(uploadPath, $"{_positions[i]}{_imageIndex}_{timestamp}.png");
                    SaveImage(framePath, capturePath);
                }

                _imageIndex++;
                Console.WriteLine($"✅ Đã lưu bộ ảnh thứ {_imageIndex - 1}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving captured frames: {ex.Message}");
                return false;
            }
        }

        private bool SaveFaceIdFrame(int directionIndex)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                // Tạo thư mục upload nếu chưa tồn tại
                var uploadPath = Path.Combine(AppContext.BaseDirectory, "upload", _sessionId);
                Directory.CreateDirectory(uploadPath);

                // Chỉ lưu ảnh từ camera khuôn mặt (camera 0)
                var framePath = Path.Combine(AppContext.BaseDirectory, "sessions", _sessionId,
                    $"{_positions[0]}_frame.jpg");

                // Tạo tên file theo định dạng mới: face<imageIndex>_<timestamp>.png
                var capturePath = Path.Combine(uploadPath, $"face{_faceIdImageIndex}_{timestamp}.png");

                SaveImage(framePath, capturePath);

                _faceIdImageIndex++;
                Console.WriteLine($"📷 Đã lưu ảnh FaceID hướng {_faceDirectionsDisplay[directionIndex]}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving FaceID frame: {ex.Message}");
                return false;
            }
        }

        private bool SaveDirectionViewFrames(int directionIndex)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // Tạo thư mục upload nếu chưa tồn tại
                var uploadPath = Path.Combine(AppContext.BaseDirectory, "upload", _sessionId);
                Directory.CreateDirectory(uploadPath);

                // Lưu 3 ảnh (đầu, chân dung, giày) từ camera 0, 1, và 2
                // Camera 0 = face (đầu), Camera 1 = portrait (chân dung), Camera 2 = shoes (giày)
                var cameraIndices = new int[] { 0, 1, 2 };

                for (var i = 0; i < cameraIndices.Length; i++)
                {
                    var cameraIndex = cameraIndices[i];
                    var framePath = Path.Combine(AppContext.BaseDirectory, "sessions", _sessionId,
                        $"{_positions[cameraIndex]}_frame.jpg");
                    // Tạo tên file theo định dạng mới: <position><imageIndex>_<timestamp>.png
                    var capturePath = Path.Combine(uploadPath,
                        $"{_positions[cameraIndex]}{_directionViewImageIndex}_{timestamp}.png");
                    SaveImage(framePath, capturePath);
                }

                _directionViewImageIndex++;
                Console.WriteLine(
                    $"📷 Đã lưu bộ ảnh hướng {_bodyDirectionsDisplay[directionIndex]} (thư mục: upload/{_sessionId})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving direction view frames: {ex.Message}");
                return false;
            }
        }

        private bool SaveHandsImages()
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // Tạo thư mục upload nếu chưa tồn tại
                var uploadPath = Path.Combine(AppContext.BaseDirectory, "upload", _sessionId);
                Directory.CreateDirectory(uploadPath);

                // Lưu ảnh từ camera 3 (hands)
                var framePath = Path.Combine(AppContext.BaseDirectory, "sessions", _sessionId,
                    $"{_positions[3]}_frame.jpg");

                // Tạo tên file theo định dạng: hands<imageIndex>_<timestamp>.png
                var capturePath = Path.Combine(uploadPath, $"hands{_imageIndex}_{timestamp}.png");
                SaveImage(framePath, capturePath);

                _imageIndex++;
                Console.WriteLine($"📷 Đã lưu ảnh tay (thư mục: upload/{_sessionId})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving hands images: {ex.Message}");
                return false;
            }
        }

        private static void SaveImage(string sourcePath, string destinationPath)
        {
            if (!File.Exists(sourcePath)) return;
            File.Copy(sourcePath, destinationPath, true);
            Console.WriteLine($"📸 Saved frame to {destinationPath}");
        }

        private void StopProcessing()
        {
            _framesSendingCts?.Cancel();
            _cts.Cancel();
        }

        public void Dispose()
        {
            StopProcessing();
            for (var i = 0; i < 4; i++)
            {
                if (_ffmpegProcesses[i] != null && !_ffmpegProcesses[i]!.HasExited)
                {
                    _ffmpegProcesses[i]!.Kill();
                    _ffmpegProcesses[i]!.WaitForExit();
                }

                _ffmpegProcesses[i]?.Dispose();
            }

            _channel.Dispose();
        }
    }
}