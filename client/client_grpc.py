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

import threading
import queue
import time
import cv2

if __name__ == "__main__":
    client = CameraClient()
    cap = cv2.VideoCapture(0)
    
    # Create a queue for passing frames between threads
    frame_queue = queue.Queue(maxsize=10)
    
    # Flag to signal thread termination
    stop_event = threading.Event()
    
    if not cap.isOpened():
        print("Error: Could not open webcam")
        exit()
    
    # Thread function for processing frames
    def process_frames():
        while not stop_event.is_set():
            try:
                # Get frames from queue with timeout
                frames = frame_queue.get(timeout=1)
                if frames is None:
                    continue
                
                frame1, frame2, frame3, frame4 = frames
                
                if client.session_id is None:
                    type_instruction = "6d94f513-0c33-446e-9839-d97d8c8945ca"
                    try:
                        response = client.call_grpc_stream(
                            frame1, frame2, frame3, frame4,
                            type_instruction
                        )
                        print(response.instruction_str)
                    except Exception as e:
                        print(f"Error calling gRPC: {e}")
                
                frame_queue.task_done()
            except queue.Empty:
                continue
    
    # Start processing thread
    processing_thread = threading.Thread(target=process_frames)
    processing_thread.daemon = True
    processing_thread.start()
    
    try:
        while True:
            # Capture frame-by-frame
            ret, frame1 = cap.read()
            
            if ret:
                # Create copies of the frame
                frame2 = frame1.copy()
                frame3 = frame1.copy()
                frame4 = frame1.copy()
                
                # Add frames to queue if there's room (non-blocking)
                try:
                    frames = (frame1.copy(), frame2, frame3, frame4)
                    if not frame_queue.full():
                        frame_queue.put_nowait(frames)
                except queue.Full:
                    # Skip this frame if queue is full
                    pass
                
                # Display the frame immediately
                cv2.imshow('Real-time Processing', frame1)
                
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
                
    finally:
        # Signal the processing thread to stop
        stop_event.set()
        
        # Wait for processing thread to finish
        processing_thread.join(timeout=1)
        
        # Clean up
        cap.release()
        cv2.destroyAllWindows()

# using video .mp4
    # client = CameraClient()
    # cap = cv2.VideoCapture("hand.mp4")

    # if not cap.isOpened():
    #     print("Error: Could not open video")
    #     exit()
    # try:
    #     while True:
    #         # Capture frame-by-frame
    #         ret, frame1 = cap.read()
    #         frame2 = frame1.copy()
    #         frame3 = frame1.copy()
    #         frame4 = frame1.copy()
    #         if ret:
    #             if client.session_id is None:
    #                 type_instruction = "hand"
    #                 try:
    #                     response = client.call_grpc_stream(
    #                         frame1, frame2, frame3, frame4,
    #                         type_instruction
    #                     )
    #                     print(response.instruction_str)
    #                 except Exception as e:
    #                     print(f"Error calling gRPC: {e}")
                
    #         cv2.imshow('Real-time Processing', frame1)
    #         if cv2.waitKey(1) & 0xFF == ord('q'):
    #             break
            
    # finally:    
    #     cap.release()
    #     cv2.destroyAllWindows()