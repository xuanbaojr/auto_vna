from ultralytics import YOLO
import os
import cv2
import numpy as np

current_dir = os.path.dirname(__file__)
output = f"{current_dir}/yolo_hand.pt"
hand_yolo = YOLO(output)

class HandService:
    def check(self, frame):
        results = hand_yolo(frame)
        if len(results[0].boxes.cls) == 2:
            return "hand:true"
        return "hand:false"

if __name__ == "__main__":
    hand_service = HandService()
    frame = cv2.imread("im1.jpg")
    hand_service.is_capture(frame)
