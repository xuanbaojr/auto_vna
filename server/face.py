from ultralytics import YOLO
import os
import cv2
import mediapipe as mp
import numpy as np

current_dir = os.path.dirname(__file__)

degrees_map = {"0": [-178, -160, -100, 100], "1": [-180, -175, 175, 180, -10, 10], 
               "2": [-176, -163, -40, -10], "3": [-163, -160, -10, 10],
               '4': [-176, -163, 12, 30]}

class FaceService:
    def __init__(self):
        self.check_yaw_service = CheckDegreeService()
        self.save_service = SaveService()
            
    def is_create_session(self, frame):

        pitch, yaw, roll = self.check_yaw_service.get_degree(frame)
        if pitch is not None and yaw is not None and roll is not None:
            v = degrees_map["0"]
            if v[2] <= yaw <= v[3]:
                return "face:true"
        return "face:false"
    
    def get_instruction(self, frame, session_id):
        saved_ims = self.save_service.get_image(session_id)        # 0
        print("saved_ims", saved_ims)
        un_saved_ims = [k for k,v in degrees_map.items() if k not in saved_ims]
        if len(un_saved_ims) == 0:
            return "face:done"
        else:
            pitch, yaw, roll = self.check_yaw_service.get_degree(frame)
            if pitch is not None and yaw is not None and roll is not None:
                for k in un_saved_ims:
                    if k == "1":
                        v = degrees_map[k]
                        if (v[0] < pitch < v[1] or (v[2] < pitch < v[3]) and v[4] < yaw < v[5]):
                            self.save_service.save_image(frame, k)
                            return f"face:{k}"
                    else:
                        v = degrees_map[k]
                        if v[0] < pitch < v[1] and v[2] < yaw < v[3]:
                            self.save_service.save_image(frame, k)
                            return f"face:{k}"
                        
            if un_saved_ims[0] == "1":
                return "face:up"
            elif un_saved_ims[0] == "2":
                return "face:right"
            elif un_saved_ims[0] == "3":
                return "face:down"
            elif un_saved_ims[0] == "4":
                return "face:left"
            
class CheckDegreeService:
    def __init__(self):
        self.mp_face_mesh = mp.solutions.face_mesh
        self.face_mesh = self.mp_face_mesh.FaceMesh(
            static_image_mode=False,
            max_num_faces=1,
            min_detection_confidence=0.5,
            min_tracking_confidence=0.5
        )
        self.FACE_LANDMARKS = {
            'nose_tip': 1,           # Đầu mũi
            'chin': 152,             # Cằm
            'left_eye_corner': 33,   # Góc mắt trái
            'right_eye_corner': 263, # Góc mắt phải
            'left_mouth': 61,        # Góc miệng trái
            'right_mouth': 291       # Góc miệng phải
        }
        
        # Định nghĩa các điểm 3D tương ứng (theo mô hình khuôn mặt 3D)
        self.points_3D = np.array([
            (0.0, 0.0, 0.0),           # Đầu mũi
            (0.0, -330.0, -65.0),      # Cằm
            (-225.0, 170.0, -135.0),   # Góc mắt trái
            (225.0, 170.0, -135.0),    # Góc mắt phải
            (-150.0, -150.0, -125.0),  # Góc miệng trái
            (150.0, -150.0, -125.0)    # Góc miệng phải
        ], dtype=np.float64)

    def get_face_landmarks(self, frame):
        image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = self.face_mesh.process(image_rgb)
        if results.multi_face_landmarks:
            return results.multi_face_landmarks[0]
        return None
    
    def get_2d_points(self, landmarks, image_shape):
        h, w, _ = image_shape
        points_2d = []
        for landmark_name, landmark_idx in self.FACE_LANDMARKS.items():
            landmark = landmarks.landmark[landmark_idx]
            x, y = int(landmark.x * w), int(landmark.y * h)
            points_2d.append((x, y))
        return np.array(points_2d, dtype=np.float64)
    
    def get_euler_angles(self, rotation_matrix):
        """Chuyển đổi ma trận quay thành các góc Euler (Pitch, Yaw, Roll)."""
        projection_matrix = np.zeros((3, 4), dtype=np.float64)
        projection_matrix[:3, :3] = rotation_matrix
        
        # Phân rã ma trận chiếu để lấy các góc Euler
        camera_matrix = np.zeros((3, 3), dtype=np.float64)
        rot_matrix = np.zeros((3, 3), dtype=np.float64)
        trans_vect = np.zeros((4, 1), dtype=np.float64)
        euler_angles = np.zeros((3, 1), dtype=np.float64)
        
        cv2.decomposeProjectionMatrix(
            projection_matrix,
            camera_matrix,
            rot_matrix,
            trans_vect,
            None,
            None,
            None,
            euler_angles
        )
        return euler_angles.flatten()
    
    def get_degree(self, frame):
        # Placeholder for yaw calculation logic
        h, w, _ = frame.shape
        landmarks = self.get_face_landmarks(frame)
        if landmarks is None:
            return None, None, None
        points_2d = self.get_2d_points(landmarks, frame.shape)
        dist_coeffs = np.zeros((4, 1), dtype=np.float64)
        camera_matrix = np.array([
            [w, 0, w/2],
            [0, w, h/2],
            [0, 0, 1]
        ], dtype=np.float64)
        
        # Giải bài toán PnP để ước tính hướng đầu
        success, rotation_vector, translation_vector = cv2.solvePnP(
            self.points_3D, points_2d, camera_matrix, dist_coeffs
        )
        if not success:
            return None, None, None
        rotation_matrix, _ = cv2.Rodrigues(rotation_vector)
        pitch, yaw, roll = self.get_euler_angles(rotation_matrix)
        return pitch, yaw, roll

class SaveService:
    def get_image(self, session_id):
        self.im_dir = f"{current_dir}/session/{session_id}"
        if not os.path.exists(self.im_dir):
            os.makedirs(self.im_dir, exist_ok=True)
        saved_ims = []
        for im_name in os.listdir(self.im_dir):
            print("im_name", im_name)
            saved_ims.append(im_name.split(".")[0])
        return saved_ims
    
    def save_image(self, image, name):
        cv2.imwrite(f"{self.im_dir}/{name}.jpg", image)

if __name__ == "__main__":
    face_service = FaceService()
    cap = cv2.VideoCapture(0)
    while True:
        ret, frame = cap.read()
        if not ret:
            break
        instruction = face_service.get_instruction(frame)
        print(instruction)
        cv2.imshow("Frame", frame)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break