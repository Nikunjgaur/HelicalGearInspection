import cv2
import os
import shutil
import matplotlib.pyplot as plt
import numpy as np
#%matplotlib inline
#import warnings
#warnings.filterwarnings('ignore')


##img=cv2.imread("E:/yolov5/1st.jpg")
##print("size of the image : ",img.shape)

def PerspTrans(img):
  pts1 = np.float32([[330,200],[1450,70],[440,860],[1650,1020]])
  #top_left(x,y),top_right,bottom_left,bottom_right
  pts2 = np.float32([[0,0],[1920,0],[0,1080],[1920,1080]])
  M = cv2.getPerspectiveTransform(pts1,pts2)
  dst = cv2.warpPerspective(img,M,(1920,1080))
##  plt.subplot(121),plt.imshow(img),plt.title('Input')
##  plt.subplot(122),plt.imshow(dst),plt.title('Output')
##  plt.show()
  return dst

#PerspTrans(img)

def extract_frame(video_path):
  
  #video_path= 'E:/yolov5/1st.mp4'
  cap = cv2.VideoCapture(video_path)

  if not cap.isOpened():
      print("Error opening video file")
      exit()

  output_folder = 'E:/yolov5/data1/'
  os.makedirs(output_folder, exist_ok=True)

  frame_count = 0
  frame_rate = 20  
  target_frame_rate = 1  # Desired frame rate to save
  frame_interval = int(frame_rate / target_frame_rate)
  while True:     
      ret, frame = cap.read()    
      if not ret:
          break    
      if frame_count % frame_interval == 0:         
          frame_path = os.path.join(output_folder, f'Fframe1_{frame_count:04d}.jpg')
          frame=PerspTrans(frame)
          cv2.imwrite(frame_path, frame)

      frame_count += 1
  cap.release()
  cv2.destroyAllWindows()
  print(f"Saved {frame_count} frames to {output_folder}")

video_path= 'E:/yolov5/latest.mp4'
extract_frame(video_path)


def rename_file(folder_path):
    #folder_path = "C:/Users/748vi/OneDrive/Desktop/Hikvision/balanceddata"
   
    files = os.listdir(folder_path)
    for file_name in files:
        if file_name.lower().endswith((".jpg", ".jpeg", ".png", ".gif")): 
            new_file_name = "F" + file_name 
            src = os.path.join(folder_path, file_name)
            dst = os.path.join(folder_path, new_file_name)
            shutil.move(src, dst)

