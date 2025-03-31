import cv2
import os
import uuid
from session_module import Session

current_dir = os.path.dirname(__file__)

class AutoService:
    def __init__(self):
        self.session_module = Session()
        self.session_id = False
    def get_instruction(self, frame1, frame2, frame3, frame4):
        if self.session_id is False:
            self.session_id = self.session_module.is_create_session(frame1)
            if self.session_id is True:
                return "session:True"
