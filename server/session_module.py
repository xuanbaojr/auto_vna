from ultralytics import YOLO
from get_gg import get_gg
import os

current_dir = os.path.dirname(__file__)
yolo_face_url = "1ZUpSNXj0IfuIgP2hyzmnno9OiU0u50ih"
output = f"{current_dir}/yolo_face.pt"
class Session:
    def __init__(self):
        pass
        if not os.path.exists(output):
            get_gg(yolo_face_url, output, is_zip=False)
        self.model = YOLO(output)
            
    def is_create_session(self, frame):
        results = self.model.predict(frame, conf=0.5)
        if len(results[0].boxes) > 0:
            return True
        else:
            return False