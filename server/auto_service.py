import cv2
import os
import uuid
from face import Cam1
current_dir = os.path.dirname(__file__)

class AutoService:
    def __init__(self):
        self.cam1 = Cam1()
    def get_instruction(self, frame1, frame2, frame3, frame4, type_instruction):
        if type_instruction == "is_check":
            self.session_id = self.cam1.is_create_session(frame1)
            if self.session_id is True:
                return "session:True"
