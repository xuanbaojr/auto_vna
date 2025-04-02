import cv2
import mediapipe as mp
import time

# Khởi tạo các module của MediaPipe
mp_drawing = mp.solutions.drawing_utils
mp_pose = mp.solutions.pose

def detect_from_image(image_path):
    """
    Phát hiện và hiển thị pose người từ một hình ảnh
    """
    # Khởi tạo pose detector
    with mp_pose.Pose(
        static_image_mode=True,
        model_complexity=2,
        enable_segmentation=True,
        min_detection_confidence=0.5
    ) as pose:
        # Đọc hình ảnh
        image = cv2.imread(image_path)
        image_height, image_width, _ = image.shape
        
        # Chuyển đổi hình ảnh sang RGB
        image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        
        # Xử lý hình ảnh
        results = pose.process(image_rgb)
        
        # Kiểm tra nếu phát hiện được người
        if results.pose_landmarks:
            print(f"Người được phát hiện trong {image_path}")
            
            # Vẽ các điểm pose lên hình ảnh
            annotated_image = image.copy()
            mp_drawing.draw_landmarks(
                annotated_image,
                results.pose_landmarks,
                mp_pose.POSE_CONNECTIONS,
                landmark_drawing_spec=mp_drawing.DrawingSpec(color=(0, 255, 0), thickness=2, circle_radius=2),
                connection_drawing_spec=mp_drawing.DrawingSpec(color=(0, 0, 255), thickness=2)
            )
            
            # Hiển thị kết quả
            cv2.imshow("Pose Detection", annotated_image)
            cv2.waitKey(0)
            cv2.destroyAllWindows()
            
            # Lưu kết quả
            cv2.imwrite('pose_detection_result.jpg', annotated_image)
            print("Kết quả đã được lưu vào 'pose_detection_result.jpg'")
        else:
            print(f"Không phát hiện người trong {image_path}")

def detect_from_webcam():
    """
    Phát hiện và hiển thị pose người từ webcam theo thời gian thực
    """
    # Khởi tạo webcam
    cap = cv2.VideoCapture(0)
    
    # Khởi tạo pose detector
    with mp_pose.Pose(
        static_image_mode=False,
        model_complexity=1,
        enable_segmentation=True,
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5
    ) as pose:
        prev_time = 0
        
        while cap.isOpened():
            success, image = cap.read()
            if not success:
                print("Không thể đọc từ webcam.")
                break
                
            # Chuyển đổi hình ảnh sang RGB
            image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            
            # Để cải thiện hiệu suất, đánh dấu hình ảnh là không ghi được (không cần thiết)
            image.flags.writeable = False
            
            # Xử lý hình ảnh
            results = pose.process(image_rgb)
            
            # Đặt lại thuộc tính writeable
            image.flags.writeable = True
            
            # Chuyển lại sang BGR để hiển thị
            image = cv2.cvtColor(image_rgb, cv2.COLOR_RGB2BGR)
            
            # Tính FPS
            current_time = time.time()
            fps = 1 / (current_time - prev_time)
            prev_time = current_time
            
            # Hiển thị FPS
            cv2.putText(image, f"FPS: {int(fps)}", (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
            
            # Kiểm tra nếu phát hiện được người
            if results.pose_landmarks:
                # Vẽ các điểm pose lên hình ảnh
                mp_drawing.draw_landmarks(
                    image,
                    results.pose_landmarks,
                    mp_pose.POSE_CONNECTIONS,
                    landmark_drawing_spec=mp_drawing.DrawingSpec(color=(0, 255, 0), thickness=2, circle_radius=2),
                    connection_drawing_spec=mp_drawing.DrawingSpec(color=(0, 0, 255), thickness=2)
                )
                
                # Hiển thị thông báo người được phát hiện
                cv2.putText(image, "Người được phát hiện", (10, 60), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
            
            # Hiển thị kết quả
            cv2.imshow("MediaPipe Pose Detection", image)
            
            # Nhấn 'q' để thoát
            if cv2.waitKey(5) & 0xFF == ord('q'):
                break
                
        cap.release()
        cv2.destroyAllWindows()

if __name__ == "__main__":
    # Chọn chế độ: 'image' hoặc 'webcam'
    mode = 'webcam'  # Thay đổi thành 'image' nếu muốn phát hiện từ hình ảnh
    
    if mode == 'image':
        image_path = 'path/to/your/image.jpg'  # Thay đổi đường dẫn đến hình ảnh của bạn
        detect_from_image(image_path)
    elif mode == 'webcam':
        detect_from_webcam()
    else:
        print("Chế độ không hợp lệ. Sử dụng 'image' hoặc 'webcam'.")

# Để sử dụng chương trình này, bạn cần cài đặt các thư viện cần thiết:
# pip install opencv-python
# pip install mediapipe