import cv2
import mediapipe as mp
import numpy as np

class ClothesService:
    def __init__(self):
        mp_pose = mp.solutions.pose
        self.pose = mp_pose.Pose(static_image_mode=False,
                    model_complexity=1,
                    smooth_landmarks=True,
                    enable_segmentation=False,
                    min_detection_confidence=0.5,
                    min_tracking_confidence=0.5)
        
        self.indices = [i for i in range(11, 29)]
    def get_keypoints(self, frame):
        # Convert BGR to RGB (MediaPipe requires RGB)
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = self.pose.process(rgb_frame)
        
        h, w, _ = frame.shape
        keypoints = []
        valid_keypoints = []
        
        if results.pose_landmarks:
            for lm in results.pose_landmarks.landmark:
                x, y = int(lm.x * w), int(lm.y * h)
                keypoints.append([lm.x, lm.y, lm.z, x, y])
            
        for idx in self.indices:
            if idx < len(keypoints):
                x, y = keypoints[idx][3], keypoints[idx][4]
                valid_keypoints.append((x,y))
        
        return valid_keypoints

if __name__ == "__main__":
    # Load the image
    frame = cv2.imread("im_.png")
    if frame is None:
        print("Error: Could not load image. Please check the file path.")
    else:
        # Process the image
        service = ClothesService()
        keypoints = service.get_keypoints(frame)
        