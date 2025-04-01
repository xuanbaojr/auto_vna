import grpc
import instruction_pb2
import instruction_pb2_grpc
import cv2
import numpy as np
import time
class CameraClient:
    def __init__(self):
        channel = grpc.insecure_channel("localhost:50051")
        self.stub = instruction_pb2_grpc.GRPCServiceStub(channel)
        self.session_id = None
        
    def call_grpc_stream(self, frame1, frame2, frame3, frame4, type_instruction):
        """Send frames to gRPC server and get instruction stream"""
        _, f1 = cv2.imencode('.jpg', frame1)
        _, f2 = cv2.imencode('.jpg', frame2)
        _, f3 = cv2.imencode('.jpg', frame3)
        _, f4 = cv2.imencode('.jpg', frame4)

        frame1, frame2, frame3, frame4 = f1.tobytes(), f2.tobytes(), f3.tobytes(), f4.tobytes()
        request = instruction_pb2.InstructionRequest(
            frame1=frame1,
            frame2=frame2,
            frame3=frame3,
            frame4=frame4,
            type_instruction=type_instruction
        )
        return self.stub.GetInstruction(request=request)

if __name__ == "__main__":
    client = CameraClient()
    cap = cv2.VideoCapture(0)

    if not cap.isOpened():
        print("Error: Could not open webcam")
        exit()
    try:
        while True:
            # Capture frame-by-frame
            ret, frame1 = cap.read()
            frame2 = frame1.copy()
            frame3 = frame1.copy()
            frame4 = frame1.copy()
            if ret:
                if client.session_id is None:
                    type_instruction = "clothes"
                    try:
                        response = client.call_grpc_stream(
                            frame1, frame2, frame3, frame4,
                            type_instruction
                        )
                        print(response.instruction_str)
                    except Exception as e:
                        print(f"Error calling gRPC: {e}")
                
            cv2.imshow('Real-time Processing', frame1)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
            
    finally:
        cap.release()
        cv2.destroyAllWindows()